using Cysharp.Threading.Tasks;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Services.UISystem;
using R3;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI
{
    public class TopBarView : BaseView<BattleFieldViewModel>
    {
        [Tooltip("Отображает имя противника")]
        [SerializeField] private TMP_Text _enemyNameTMP;

        [Tooltip("Таймер боя")]
        [SerializeField] private TMP_Text _timerTMP;

        [Tooltip("Текущий раунд в формате 'R1', 'R2' и т.д.")]
        [SerializeField] private TMP_Text _roundTMP;


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;


        public bool ApplyLayout(Rect gameplayScreenRect, float bottomScreenY, float sidePadding,
            float bottomPadding, float height)
        {
            if (transform is not RectTransform rectTransform)
                return false;

            if (rectTransform.parent is not RectTransform parentRectTransform)
                return false;

            if (Screen.width <= 0 || Screen.height <= 0 || gameplayScreenRect.width <= 0f || height <= 0f)
                return false;

            var canvas = GetComponentInParent<Canvas>();
            var cameraForCanvas = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                new Vector2(gameplayScreenRect.xMin, bottomScreenY),
                cameraForCanvas,
                out var leftBottomLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                new Vector2(gameplayScreenRect.xMax, bottomScreenY),
                cameraForCanvas,
                out var rightBottomLocal);

            var parentRect = parentRectTransform.rect;
            if (parentRect.width <= 0f || parentRect.height <= 0f)
                return false;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(
                leftBottomLocal.x - parentRect.xMin + sidePadding,
                leftBottomLocal.y - parentRect.yMin + bottomPadding);
            rectTransform.sizeDelta = new Vector2(
                rightBottomLocal.x - leftBottomLocal.x - sidePadding * 2f,
                height);
            
            return true;
        }


        protected override UniTask OnBindViewModel()
        {
            if (_enemyNameTMP)
                _enemyNameTMP.text = ViewModel.EnemyName;

            Disposables.Add(ViewModel.TimerSeconds.Subscribe(OnTimerSecondsChanged));
            Disposables.Add(ViewModel.CurrentRound.Subscribe(OnCurrentRoundChanged));

            return UniTask.CompletedTask;
        }


        private void OnTimerSecondsChanged(int totalSeconds)
        {
            if (false == _timerTMP)
                return;

            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            _timerTMP.text = $"{minutes:00}:{seconds:00}";
        }

        private void OnCurrentRoundChanged(int round)
        {
            if (false == _roundTMP)
                return;

            _roundTMP.text = $"R{round}";
        }
    }
}