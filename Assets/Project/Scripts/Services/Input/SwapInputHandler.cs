using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.Grid;
using Project.Scripts.Shared.Grid;
using UnityEngine;

namespace Project.Scripts.Services.Input
{
    public class SwapInputHandler : ISwapInputHandler, IDisposable
    {
        public event Action<SwapRequest> OnSwapRequested;


        private readonly IInputService _input;
        private readonly IGridState _state;
        private readonly IGridView _view;
        private readonly float _worldThreshold;
        private readonly bool _reanchorOnUnlock;
        private Camera _camera;
        private GridPoint _startGridPos;
        private bool _hasPendingSwap;
        private Vector2 _accumulatedScreenDelta;


        public SwapInputHandler(IInputService input, IGridState state, IGridView view, float worldThreshold, bool reanchorOnUnlock)
        {
            _input = input;
            _state = state;
            _view = view;
            _worldThreshold = worldThreshold;
            _reanchorOnUnlock = reanchorOnUnlock;
        }

        public UniTask InitAsync()
        {
            _camera = Camera.main;
            _input.OnDragStarted += HandleDragStarted;
            _input.OnDragDelta += HandleDragDelta;
            _input.OnDragCanceled += HandleDragCanceled;

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _input.OnDragStarted -= HandleDragStarted;
            _input.OnDragDelta -= HandleDragDelta;
            _input.OnDragCanceled -= HandleDragCanceled;
        }


        private void HandleDragStarted(Vector2 screenPos)
        {
#if UNITY_EDITOR
            if (BoardEdit.BoardEditMode.IsActive)
                return;
#endif
            _accumulatedScreenDelta = Vector2.zero;
            var worldPos = ScreenToWorld(screenPos);
            _startGridPos = _view.WorldToGrid(worldPos);
            _hasPendingSwap = _state.IsValidPosition(_startGridPos) && _view.GetTile(_startGridPos) != null;
        }

        private void HandleDragDelta(Vector2 screenDelta)
        {
            if (false == _hasPendingSwap)
                return;

            _accumulatedScreenDelta += screenDelta;

            var worldDelta = ScreenDeltaToWorld(_accumulatedScreenDelta);
            if (worldDelta.magnitude < _worldThreshold)
                return;

            var dir = GetDirection(worldDelta);
            var target = _startGridPos + dir;
            if (false == _state.IsValidPosition(target))
                return;

            if (false == _view.GetTile(target))
                return;

            _hasPendingSwap = false;
            OnSwapRequested?.Invoke(new SwapRequest(_startGridPos, target));
        }

        public void NotifyBoardReady()
        {
            if (_reanchorOnUnlock)
                _input.ReanchorDrag();
        }

        private void HandleDragCanceled()
        {
            _hasPendingSwap = false;
            _accumulatedScreenDelta = Vector2.zero;
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            var z = Mathf.Abs(_camera.transform.position.z);

            return _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        }

        private Vector2 ScreenDeltaToWorld(Vector2 screenDelta)
        {
            var z = Mathf.Abs(_camera.transform.position.z);
            var origin = _camera.ScreenToWorldPoint(new Vector3(0, 0, z));
            var target = _camera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, z));

            return new Vector2(target.x - origin.x, target.y - origin.y);
        }

        private static GridPoint GetDirection(Vector2 worldDelta)
        {
            if (Mathf.Abs(worldDelta.x) >= Mathf.Abs(worldDelta.y))
                return worldDelta.x > 0 ? GridPoint.Right : GridPoint.Left;

            return worldDelta.y > 0 ? GridPoint.Up : GridPoint.Down;
        }
    }
}