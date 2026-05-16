#if DEV
using Project.Scripts.Services.Board;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Dev
{
    public class DevMatchPhaseSkipButtonView : MonoBehaviour
    {
        private const float BoardAnchorOffsetX = 0f;
        private const float BoardAnchorOffsetY = 0f;

        
        private DevMatchPhaseSkipService _skipService;
        private IBoardBoundsProvider _boardBoundsProvider;
        private Button _button;
        private RectTransform _rectTransform;
        private RectTransform _parentRectTransform;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;


        private void Update()
        {
            if (null == _skipService)
                return;

            var shouldShow = _skipService.ShouldShow();
            var canSkip = _skipService.CanSkip();

            if (_canvasGroup)
            {
                _canvasGroup.alpha = shouldShow ? 1f : 0f;
                _canvasGroup.blocksRaycasts = shouldShow;
                _canvasGroup.interactable = canSkip;
            }

            if (_button)
                _button.interactable = canSkip;

            ApplyPosition();
        }

        private void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(OnClick);
        }
        

        public void Init(DevMatchPhaseSkipService skipService, IBoardBoundsProvider boardBoundsProvider)
        {
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
                _rectTransform.pivot = Vector2.one;
                _rectTransform.sizeDelta = new Vector2(100f, 100f);
            }

            if (_button)
                _button.onClick.AddListener(OnClick);
        }
        
        
        private void OnClick()
        {
            _skipService?.TrySkip();
        }

        private void ApplyPosition()
        {
            if (null == _boardBoundsProvider || !_rectTransform || !_parentRectTransform || !_canvas)
                return;

            var camera = Camera.main;
            if (!camera)
                return;

            var worldPosition = new Vector3(_boardBoundsProvider.BoardCenterX + _boardBoundsProvider.BoardHalfWidth,
                _boardBoundsProvider.BoardTopWorldY, 0f);

            var cameraForCanvas = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            if (false == RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform,
                    camera.WorldToScreenPoint(worldPosition), cameraForCanvas, out var localPosition))
            {
                return;
            }

            var parentRect = _parentRectTransform.rect;
            _rectTransform.anchoredPosition = new Vector2(
                localPosition.x - parentRect.xMin + BoardAnchorOffsetX,
                localPosition.y - parentRect.yMin + BoardAnchorOffsetY);
        }
    }
}
#endif