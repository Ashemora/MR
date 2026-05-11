#if UNITY_EDITOR
using DG.Tweening;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Services.Grid;
using UnityEngine;
using UnityEngine.InputSystem;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Services.BoardEdit
{
    public class BoardEditClickHandler : MonoBehaviour
    {
        private IGridState _state;
        private IGridView _view;
        private GridConfig _gridConfig;
        private float _cellSize;
        private GameObject _overlayGo;
        private LineRenderer _overlay;
        private Tween _overlayTween;


        public void Init(IGridState state, IGridView view, GridConfig gridConfig, float cellSize)
        {
            _state = state;
            _view = view;
            _gridConfig = gridConfig;
            _cellSize = cellSize;
            BoardEditMode.OnToggled += OnEditModeToggled;
        }


        private void OnDestroy()
        {
            BoardEditMode.OnToggled -= OnEditModeToggled;
            DestroyOverlay();
        }


        private void Update()
        {
            if (false == BoardEditMode.IsActive)
                return;

            if (false == TryGetPressPosition(out var screenPos))
                return;

            if (null == _state)
                return;

            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(Camera.main.transform.position.z)));
            var gridPos = _view.WorldToGrid(worldPos);

            if (false == _state.IsValidPosition(gridPos))
                return;

            if (false == _view.GetTile(gridPos))
                return;

            _view.ReplaceForEdit(gridPos, BoardEditMode.SelectedKind);
        }

        private static bool TryGetPressPosition(out Vector2 screenPos)
        {
            if (null != Mouse.current && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                
                return true;
            }

            if (null != Touchscreen.current && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                
                return true;
            }

            screenPos = default;
            
            return false;
        }


        private void OnEditModeToggled(bool active)
        {
            if (active)
                CreateOverlay();
            else
                DestroyOverlay();
        }

        private void CreateOverlay()
        {
            var half = _cellSize * 0.5f;
            var bl = _view.GridToWorld(new GridPoint(0, 0)) + new Vector3(-half, -half, 0f);
            var br = _view.GridToWorld(new GridPoint(_gridConfig.Width - 1, 0)) + new Vector3( half, -half, 0f);
            var tr = _view.GridToWorld(new GridPoint(_gridConfig.Width - 1, _gridConfig.Height - 1)) + new Vector3( half,  half, 0f);
            var tl = _view.GridToWorld(new GridPoint(0, _gridConfig.Height - 1)) + new Vector3(-half,  half, 0f);

            _overlayGo = new GameObject("BoardEditOverlay");
            _overlay = _overlayGo.AddComponent<LineRenderer>();
            _overlay.positionCount = 4;
            _overlay.loop = true;
            _overlay.useWorldSpace = true;
            _overlay.SetPositions(new[] { bl, br, tr, tl });
            _overlay.startWidth = _overlay.endWidth = _cellSize * 0.06f;
            _overlay.material = new Material(Shader.Find("Sprites/Default"));
            _overlay.sortingOrder = 100;

            var baseColor = new Color(0.2f, 1f, 0.4f, 0.7f);
            var fadeColor = new Color(0.2f, 1f, 0.4f, 0.25f);
            _overlay.startColor = _overlay.endColor = baseColor;

            _overlayTween = DOTween.To(
                () => _overlay.startColor,
                c => { _overlay.startColor = c; _overlay.endColor = c; },
                fadeColor,
                0.7f
            ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void DestroyOverlay()
        {
            if (false == _overlayGo)
                return;

            _overlayTween?.Kill();
            _overlayTween = null;
            Destroy(_overlayGo);
            _overlayGo = null;
            _overlay = null;
        }
    }
}
#endif