using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.UISystem;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class BoardAnnouncementView : BaseView<BoardAnnouncementViewModel>
    {
        [Tooltip("Canvas group of the root object - used for fade-out")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("RectTransform of the text container - used for fly-up animation")]
        [SerializeField] private RectTransform _textRect;

        [Tooltip("Text component displaying the announcement message")]
        [SerializeField] private TMP_Text _text;


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;
        
        
        private Sequence _sequence;
        private RectTransform _canvasRect;
        private Camera _cam;


        private void Awake()
        {
            _canvasRect = transform.parent as RectTransform;
            _cam = Camera.main;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }


        protected override async UniTask OnShow()
        {
            if (_text)
            {
                _text.text = ViewModel.Text;
                _text.color = ViewModel.TextColor;
            }

            if (_canvasGroup)
                _canvasGroup.alpha = 1f;

            if (_textRect)
                _textRect.localScale = Vector3.one * ViewModel.BaseScale;

            var startPos = WorldYToAnchored(ViewModel.CurrentWorldY);

            if (_textRect)
                _textRect.anchoredPosition = startPos;

            await FollowAnchorDuringDisplay();
            startPos = _textRect ? _textRect.anchoredPosition : WorldYToAnchored(ViewModel.CurrentWorldY);

            if (ViewModel.FadeOutDuration > 0f)
            {
                _sequence?.Kill();
                _sequence = DOTween.Sequence();

                switch (ViewModel.Style)
                {
                    case AnnouncementStyle.FlyUp:
                        if (_textRect)
                            _sequence.Join(_textRect
                                .DOAnchorPosY(startPos.y + ViewModel.FlyDistance, ViewModel.FadeOutDuration)
                                .SetEase(ViewModel.FadeOutEase));
                        break;

                    case AnnouncementStyle.ScaleFade:
                        if (_textRect)
                            _sequence.Join(_textRect
                                .DOScale(ViewModel.BaseScale * ViewModel.ScaleMultiplier, ViewModel.FadeOutDuration)
                                .SetEase(ViewModel.FadeOutEase));
                        break;
                }

                if (_canvasGroup)
                    _sequence.Join(_canvasGroup
                        .DOFade(0f, ViewModel.FadeOutDuration)
                        .SetEase(ViewModel.FadeOutEase));

                await _sequence.ToUniTask();
            }

            ViewModel.NotifyAnimationDone();
        }


        private Vector2 WorldYToAnchored(float worldY)
        {
            if (!_cam || !_canvasRect)
                return Vector2.zero;

            var screenPoint = _cam.WorldToScreenPoint(new Vector3(0f, worldY, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, null, out var localPoint);

            return localPoint;
        }

        private async UniTask FollowAnchorDuringDisplay()
        {
            if (ViewModel.DisplayDuration <= 0f)
                return;

            var elapsed = 0f;
            while (elapsed < ViewModel.DisplayDuration)
            {
                if (_textRect)
                    _textRect.anchoredPosition = WorldYToAnchored(ViewModel.CurrentWorldY);

                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
        }
    }
}