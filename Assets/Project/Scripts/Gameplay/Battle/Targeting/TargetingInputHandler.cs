using System;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Input;
using R3;
using UnityEngine;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Targeting
{
    public class TargetingInputHandler : MonoBehaviour
    {
        private const float OffsetPx = 20f;
        private const float SelfTargetThresholdPx = 25f;


        public ReadOnlyReactiveProperty<bool> IsHoveringBlockedAvatar => _isHoveringBlockedAvatar;


        private IInputService _input;
        private TargetingRegistry _registry;
        private IAbilityExecutionService _abilityExecution;
        private IGameStateService _gameStateService;
        private IBattleActionRuntimeService _battleActionRuntimeService;
        private IAvatarGroupDefenseService _groupDefense;
        private Camera _cam;
        private ITargetable _source;
        private ITargetable _target;
        private Vector2 _currentScreenPos;
        private Vector2 _dragStartScreenPos;
        private bool _selfTargetArmed;
        private int _actionSessionVersion = -1;
        private IDisposable _runtimeStateSubscription;
        private IDisposable _gameStateSubscription;
        private readonly ReactiveProperty<bool> _isHoveringBlockedAvatar = new(false);


        private void OnDestroy()
        {
            ClearSelection();
            Unsubscribe();
            _isHoveringBlockedAvatar.Dispose();
        }


        public void Init(IInputService input, TargetingRegistry registry, IAbilityExecutionService abilityExecution,
            IGameStateService gameStateService, IBattleActionRuntimeService battleActionRuntimeService,
            IAvatarGroupDefenseService groupDefense, Camera cam)
        {
            Unsubscribe();
            _input = input;
            _registry = registry;
            _abilityExecution = abilityExecution;
            _gameStateService = gameStateService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _groupDefense = groupDefense;
            _cam = cam;

            _input.OnDragStarted += HandleDragStarted;
            _input.OnDragDelta += HandleDragDelta;
            _input.OnDragCanceled += HandleDragCanceled;
            _runtimeStateSubscription = _battleActionRuntimeService.State.Subscribe(_ => OnRuntimeStateChanged());
            _gameStateSubscription = _gameStateService.State.Subscribe(OnGameStateChanged);
        }


        private void HandleDragStarted(Vector2 screenPos)
        {
            if (false == CanStartSelection())
                return;

            _currentScreenPos = screenPos;

            var unit = _registry.FindAtPosition(screenPos, _cam, OffsetPx);

            if (null == unit || false == unit.IsReadySource || unit.Descriptor.Side != BattleSide.Player)
                return;

            _actionSessionVersion = _battleActionRuntimeService.CaptureVersion();
            _source = unit;
            _dragStartScreenPos = screenPos;
            _selfTargetArmed = false;
            _source.SetSourceHighlight(true);
        }

        private void HandleDragDelta(Vector2 screenDelta)
        {
            if (null == _source)
                return;

            if (false == CanContinueSelection())
            {
                ClearSelection();
                return;
            }

            _currentScreenPos += screenDelta;

            if (false == _selfTargetArmed
                && (_currentScreenPos - _dragStartScreenPos).sqrMagnitude
                    >= SelfTargetThresholdPx * SelfTargetThresholdPx)
                _selfTargetArmed = true;

            var candidate = _registry.FindAtPosition(_currentScreenPos, _cam, OffsetPx);
            var isSelfCandidate = null != candidate && candidate == _source;
            var passesSelfGate = false == isSelfCandidate || _selfTargetArmed;
            var valid = null != candidate
                        && passesSelfGate
                        && _abilityExecution.CanTarget(_source.Descriptor, candidate.Descriptor);

            ApplyHighlights(valid ? candidate : null);

            _isHoveringBlockedAvatar.Value = null != candidate
                && candidate.Descriptor.Kind == UnitKind.Avatar
                && candidate.Descriptor.Side == BattleSide.Enemy
                && _source.ActionType == UnitActionType.DealDamage
                && null != _groupDefense
                && false == _groupDefense.IsExposed(candidate.Descriptor.Side);
        }

        private void ApplyHighlights(ITargetable newTarget)
        {
            if (null != _target && _target != newTarget)
            {
                _target.SetTargetHighlight(false, default);
                _target = null;
            }

            _target = newTarget;

            if (null != _target)
            {
                _target.SetTargetHighlight(true, _source.ActionType);

                if (_target == _source)
                    return;
            }

            _source?.SetSourceHighlight(true);
        }

        private void HandleDragCanceled()
        {
            if (null != _source && null != _target && CanCommitSelection())
                _abilityExecution.Execute(_source.Descriptor, _target.Descriptor);

            ClearSelection();
        }

        private void Unsubscribe()
        {
            _runtimeStateSubscription?.Dispose();
            _runtimeStateSubscription = null;
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = null;

            if (null == _input)
                return;

            _input.OnDragStarted -= HandleDragStarted;
            _input.OnDragDelta -= HandleDragDelta;
            _input.OnDragCanceled -= HandleDragCanceled;
        }

        private bool CanStartSelection()
        {
            if (null == _battleActionRuntimeService || null == _gameStateService)
                return false;

            if (false == _gameStateService.IsPlaying)
                return false;

            return _battleActionRuntimeService.Evaluate(BattleActionKind.AbilitySourceSelect).IsAllowed;
        }

        private bool CanContinueSelection()
        {
            if (null == _battleActionRuntimeService || null == _gameStateService)
                return false;

            if (false == _gameStateService.IsPlaying)
                return false;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilitySourceSelect).IsAllowed)
                return false;

            return _battleActionRuntimeService.IsCurrent(_actionSessionVersion);
        }

        private bool CanCommitSelection()
        {
            if (null == _battleActionRuntimeService || null == _gameStateService)
                return false;

            if (false == _gameStateService.IsPlaying)
                return false;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return false;

            return _battleActionRuntimeService.IsCurrent(_actionSessionVersion);
        }

        private void OnRuntimeStateChanged()
        {
            if (null == _source && null == _target)
                return;

            if (false == _battleActionRuntimeService.CanAcceptNormalActions)
                ClearSelection();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Playing)
                ClearSelection();
        }

        private void ClearSelection()
        {
            _source?.SetSourceHighlight(false);
            _target?.SetTargetHighlight(false, default);
            _source = null;
            _target = null;
            _actionSessionVersion = -1;
            _dragStartScreenPos = default;
            _selfTargetArmed = false;
            _isHoveringBlockedAvatar.Value = false;
        }
    }
}