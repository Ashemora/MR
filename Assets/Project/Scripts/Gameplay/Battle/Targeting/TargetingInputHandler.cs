using System;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Input;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Rules;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Targeting
{
    public class TargetingInputHandler : MonoBehaviour
    {
        private const float OffsetPx = 20f;


        public ReadOnlyReactiveProperty<bool> IsHoveringBlockedAvatar => _isHoveringBlockedAvatar;


        private IInputService _input;
        private TargetingRegistry _registry;
        private IAbilityExecutionService _abilityExecution;
        private IGameStateService _gameStateService;
        private IBattleActionRuntimeService _battleActionRuntimeService;
        private Camera _cam;
        private ITargetable _source;
        private ITargetable _target;
        private Vector2 _currentScreenPos;
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


        public void Init(
            IInputService input,
            TargetingRegistry registry,
            IAbilityExecutionService abilityExecution,
            IGameStateService gameStateService,
            IBattleActionRuntimeService battleActionRuntimeService,
            Camera cam)
        {
            Unsubscribe();
            _input = input;
            _registry = registry;
            _abilityExecution = abilityExecution;
            _gameStateService = gameStateService;
            _battleActionRuntimeService = battleActionRuntimeService;
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

            if (unit == null || false == unit.IsReadySource || unit.Descriptor.Side != BattleSide.Player)
                return;

            _actionSessionVersion = _battleActionRuntimeService.CaptureVersion();
            _source = unit;
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

            var candidate = _registry.FindAtPosition(_currentScreenPos, _cam, OffsetPx);
            var valid = candidate != null
                        && candidate != _source
                        && candidate.IsValidTarget(_source.Descriptor);

            if (_target != null && _target != candidate)
            {
                _target.SetTargetHighlight(false, default);
                _target = null;
            }

            if (valid)
            {
                _target = candidate;
                _target.SetTargetHighlight(true, _source.Descriptor.ActionType);
            }

            _isHoveringBlockedAvatar.Value = candidate != null
                && false == valid
                && candidate.Descriptor.Kind == UnitKind.Avatar
                && candidate.Descriptor.Side == BattleSide.Enemy
                && _source.Descriptor.ActionType == HeroActionType.DealDamage;
        }

        private void HandleDragCanceled()
        {
            if (_source != null && _target != null && CanCommitSelection())
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
            if (_source == null && _target == null)
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
            _isHoveringBlockedAvatar.Value = false;
        }
    }
}