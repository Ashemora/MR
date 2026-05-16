using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Services.UISystem;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Gameplay.UI.MoveBar
{
    public class MoveBarView : BaseView<MoveBarViewModel>
    {
        [Tooltip("Всегда видимое непрозрачное изображение базового слоя")]
        [SerializeField] private Image _background;

        [Tooltip("Полупрозрачное изображение заполнения (Type=Filled, Fill Method=Horizontal, Fill Origin=Left)")]
        [SerializeField] private Image _fill;

        [Tooltip("Контейнер RectTransform, в котором во время выполнения создаются изображения дискретных сегментов")]
        [SerializeField] private RectTransform _segmentsContainer;

        [Tooltip("Префаб с компонентом Image для каждого дискретного сегмента заряда хода")]
        [SerializeField] private GameObject _segmentPrefab;


        private readonly List<Image> _segments = new();
        private readonly List<Tween> _blinkTweens = new();
        private int _previousMoves;
        private Canvas _canvas;
        private bool _isOverlay;
        private RectTransform _referenceRect;
        private Tween _shakeTween;
#if UNITY_EDITOR
        private bool _rebuildPending;
#endif


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;


        protected override async UniTask OnBindViewModel()
        {
            _canvas = GetComponentInParent<Canvas>();
            _isOverlay = _canvas && _canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            _referenceRect = transform.parent as RectTransform;

            await UniTask.NextFrame();

            BuildSegments(ViewModel.MaxMoves);
            ApplyBoardWidth();

            var initialMoves = ViewModel.CurrentMoves.CurrentValue;
            SetSegmentsImmediate(initialMoves);
            _previousMoves = initialMoves;

            _fill.fillAmount = ViewModel.FillProgress.CurrentValue;

            ViewModel.FillProgress
                .Skip(1)
                .Subscribe(v => _fill.fillAmount = v)
                .AddTo(Disposables);

            ViewModel.CurrentMoves
                .Skip(1)
                .Subscribe(UpdateSegments)
                .AddTo(Disposables);

            ViewModel.IsAtMax
                .Skip(1)
                .Subscribe(UpdateBlink)
                .AddTo(Disposables);

            UpdateBlink(ViewModel.IsAtMax.CurrentValue);

            ViewModel.OnSwapRejected
                .Subscribe(_ => ShakeBar())
                .AddTo(Disposables);
        }

        protected override void OnClose()
        {
            _shakeTween?.Kill();
            _shakeTween = null;
            KillBlinkTweens();
        }
        

#if UNITY_EDITOR
        private void OnRectTransformDimensionsChange()
        {
            if (null == ViewModel || _rebuildPending)
                return;

            _rebuildPending = true;
            RebuildOnResizeAsync().Forget();
        }

        private async UniTaskVoid RebuildOnResizeAsync()
        {
            await UniTask.NextFrame();
            _rebuildPending = false;

            BuildSegments(ViewModel.MaxMoves);
            ApplyBoardWidth();
            SetSegmentsImmediate(_previousMoves);
            UpdateBlink(ViewModel.IsAtMax.CurrentValue);
        }
#endif

        private void ApplyBoardWidth()
        {
            var cam = Camera.main;
            if (!cam || !_canvas || !_referenceRect)
                return;

            var camForCanvas = _isOverlay ? null : _canvas.worldCamera;
            var centerX = ViewModel.BoardCenterX;
            var halfWidth = ViewModel.BoardHalfWidth;

            var leftWorld = new Vector3(centerX - halfWidth, 0f, 0f);
            var rightWorld = new Vector3(centerX + halfWidth, 0f, 0f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _referenceRect, cam.WorldToScreenPoint(leftWorld), camForCanvas, out var leftLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _referenceRect, cam.WorldToScreenPoint(rightWorld), camForCanvas, out var rightLocal);

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rightLocal.x - leftLocal.x, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2((leftLocal.x + rightLocal.x) / 2f, rt.anchoredPosition.y);
        }

        private void BuildSegments(int count)
        {
            for (var i = 0; i < _segments.Count; i++)
                if (_segments[i])
                    Destroy(_segments[i].gameObject);

            _segments.Clear();

            if (count <= 0)
                return;

            var totalWidth = _segmentsContainer.rect.width;
            var gapWidth = totalWidth * ViewModel.Config.GapFraction;
            var totalGapWidth = gapWidth * (count - 1);
            var segmentWidth = (totalWidth - totalGapWidth) / count;

            for (var i = 0; i < count; i++)
            {
                var go = Instantiate(_segmentPrefab, _segmentsContainer);
                var rt = go.GetComponent<RectTransform>();

                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(segmentWidth, 0f);
                rt.anchoredPosition = new Vector2(i * (segmentWidth + gapWidth), 0f);

                var image = go.GetComponent<Image>();
                image.enabled = false;
                _segments.Add(image);
            }
        }

        private void SetSegmentsImmediate(int currentMoves)
        {
            for (var i = 0; i < _segments.Count; i++)
                _segments[i].enabled = i < currentMoves;
        }

        private void UpdateSegments(int newMoves)
        {
            if (newMoves > _previousMoves)
            {
                for (var i = _previousMoves; i < newMoves && i < _segments.Count; i++)
                {
                    DOTween.Kill(_segments[i]);
                    DOTween.Kill(_segments[i].transform);
                    var c = _segments[i].color;
                    c.a = 1f;
                    _segments[i].color = c;
                    _segments[i].enabled = true;

                    _segments[i].transform.DOPunchScale(
                        Vector3.one * ViewModel.Config.PunchStrength,
                        ViewModel.Config.PunchDuration,
                        vibrato: 1,
                        elasticity: 0f);
                }
            }
            else if (newMoves < _previousMoves)
            {
                for (var i = newMoves; i < _previousMoves && i < _segments.Count; i++)
                    _segments[i].enabled = false;
            }

            _previousMoves = newMoves;
        }

        private void UpdateBlink(bool isAtMax)
        {
            KillBlinkTweens();

            for (var i = 0; i < _segments.Count; i++)
            {
                var c = _segments[i].color;
                c.a = 1f;
                _segments[i].color = c;
            }

            if (false == isAtMax || _segments.Count == 0)
                return;

            for (var i = 0; i < _segments.Count; i++)
            {
                var tween = _segments[i]
                    .DOFade(ViewModel.Config.FullBlinkMinAlpha, ViewModel.Config.FullBlinkHalfDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
                _blinkTweens.Add(tween);
            }
        }

        private void ShakeBar()
        {
            _shakeTween?.Kill(complete: true);
            _shakeTween = transform.DOShakePosition(
                ViewModel.Config.EmptyShakeDuration,
                new Vector3(ViewModel.Config.EmptyShakeStrength, 0f, 0f),
                vibrato: 10,
                randomness: 0f);
        }

        private void KillBlinkTweens()
        {
            for (var i = 0; i < _blinkTweens.Count; i++)
                _blinkTweens[i]?.Kill();
            _blinkTweens.Clear();
        }
    }
}