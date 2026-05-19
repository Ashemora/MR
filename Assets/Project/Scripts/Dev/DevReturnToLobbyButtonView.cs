#if DEV
using Project.Scripts.Services.Board;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Dev
{
    public class DevReturnToLobbyButtonView : MonoBehaviour
    {
        private const float BoardAnchorOffsetX = 0f;
        private const float BoardAnchorOffsetY = 30f;


        private DevAbortBattleService _abortService;
        private DevMatchPhaseSkipService _skipService;
        private IBoardBoundsProvider _boardBoundsProvider;
        private Button _button;
        private RectTransform _rectTransform;
        private RectTransform _parentRectTransform;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private bool _revealed;


        private void LateUpdate()
        {
            if (null == _abortService || null == _skipService)
                return;

            if (_skipService.ShouldShow())
                _revealed = true;

            var shouldShow = _revealed && _skipService.IsVisibleDuringGameplay();
            var canAbort = shouldShow && _abortService.CanAbort();

            if (_canvasGroup)
            {
                _canvasGroup.alpha = shouldShow ? 1f : 0f;
                _canvasGroup.blocksRaycasts = shouldShow;
                _canvasGroup.interactable = canAbort;
            }

            if (_button)
                _button.interactable = canAbort;

            if (shouldShow)
                ApplyPosition();
        }

        private void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(OnClick);
        }


        public void Init(DevAbortBattleService abortService, DevMatchPhaseSkipService skipService,
            IBoardBoundsProvider boardBoundsProvider)
        {
            _abortService = abortService;
            _skipService = skipService;
            _boardBoundsProvider = boardBoundsProvider;
            _button = GetComponent<Button>();
            _rectTransform = transform as RectTransform;
            _parentRectTransform = transform.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (!_canvasGroup)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_rectTransform)
            {
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.zero;
                _rectTransform.pivot = new Vector2(0f, 1f);
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            if (_button)
                _button.onClick.AddListener(OnClick);
        }


        private void OnClick()
        {
            _abortService?.TryAbort();
        }

        private void ApplyPosition()
        {
            if (null == _boardBoundsProvider || !_rectTransform || !_parentRectTransform || !_canvas)
                return;

            var worldCamera = _canvas.worldCamera ? _canvas.worldCamera : Camera.main;
            if (!worldCamera)
                return;

            var worldPosition = new Vector3(_boardBoundsProvider.BoardCenterX - _boardBoundsProvider.BoardHalfWidth,
                _boardBoundsProvider.BoardTopWorldY, 0f);

            var cameraForCanvas = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : worldCamera;

            if (false == RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform,
                    worldCamera.WorldToScreenPoint(worldPosition), cameraForCanvas, out var localPosition))
                return;

            var parentRect = _parentRectTransform.rect;
            _rectTransform.anchoredPosition = new Vector2(
                localPosition.x - parentRect.xMin + BoardAnchorOffsetX,
                localPosition.y - parentRect.yMin + BoardAnchorOffsetY);
        }
    }
}
#endif