using System;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Game;
using R3;
using UnityEngine;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Units
{
    public class AvatarChargeBarViewModel : IDisposable
    {
        public ReactiveProperty<float> FillFraction { get; } = new (0f);
        public ReactiveProperty<bool> IsReady { get; } = new (false);
        public ReactiveProperty<UnitActivationBlockReason> ActivationBlockReason { get; } = new (UnitActivationBlockReason.None);
        public ReactiveProperty<bool> IsAvailabilityDimmed { get; } = new (false);
        public ReactiveProperty<(float Remaining, float Duration)> CooldownProgress { get; } = new ((0f, 0f));
        public ReactiveProperty<(float Remaining, float Duration)> StunProgress { get; } = new ((0f, 0f));


        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly BattleSide _side;
        private int _activationEnergyCost;
        private int _currentEnergy;
        private bool _hasSufficientEnergy;
        private bool _isOnCooldown;
        private bool _isStunned;


        public AvatarChargeBarViewModel(EventBus eventBus, BattleSide side, int activationEnergyCost,
            IUnitActivationCooldownService cooldownService, IBattleActionRuntimeService battleActionRuntimeService)
        {
            _battleActionRuntimeService = battleActionRuntimeService;
            _side = side;
            _activationEnergyCost = activationEnergyCost;
            _hasSufficientEnergy = activationEnergyCost <= 0;
            _isOnCooldown = cooldownService.IsAvatarOnCooldown(side);

            _subscriptions.Add(eventBus.Subscribe<BattleSideEnergyChangedEvent>(OnBattleSideEnergyChanged));
            _subscriptions.Add(eventBus.Subscribe<AvatarCooldownChangedEvent>(OnAvatarCooldownChanged));
            _subscriptions.Add(eventBus.Subscribe<UnitStunChangedEvent>(OnUnitStunChanged));
            _subscriptions.Add(_battleActionRuntimeService.State.Subscribe(_ => RefreshReadyState()));
        }

        public void UpdateActivationEnergyCost(int activationEnergyCost)
        {
            _activationEnergyCost = activationEnergyCost < 0 ? 0 : activationEnergyCost;
            RefreshEnergyState(_currentEnergy);
        }

        public void Dispose()
        {
            FillFraction.Dispose();
            IsReady.Dispose();
            ActivationBlockReason.Dispose();
            IsAvailabilityDimmed.Dispose();
            CooldownProgress.Dispose();
            StunProgress.Dispose();
            _subscriptions.Dispose();
        }

        private void OnBattleSideEnergyChanged(BattleSideEnergyChangedEvent e)
        {
            if (e.Side != _side)
                return;

            RefreshEnergyState(e.Current);
        }

        private void RefreshEnergyState(int currentEnergy)
        {
            _currentEnergy = currentEnergy < 0 ? 0 : currentEnergy;
            FillFraction.Value = _activationEnergyCost > 0 ? Mathf.Clamp01((float)_currentEnergy / _activationEnergyCost) : 0f;
            _hasSufficientEnergy = _activationEnergyCost <= 0 || _currentEnergy >= _activationEnergyCost;
            RefreshReadyState();
        }

        private void RefreshReadyState()
        {
            var gateResult = _battleActionRuntimeService.Evaluate(BattleActionKind.AvatarActivation);
            if (false == gateResult.IsAllowed)
            {
                IsReady.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.BlockedByPhase;
                RefreshAvailabilityVisualState();
                
                return;
            }

            if (false == _hasSufficientEnergy)
            {
                IsReady.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.InsufficientEnergy;
                RefreshAvailabilityVisualState();
                
                return;
            }

            if (_isStunned)
            {
                IsReady.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.Stunned;
                RefreshAvailabilityVisualState();

                return;
            }

            if (_isOnCooldown)
            {
                IsReady.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.Cooldown;
                RefreshAvailabilityVisualState();
                
                return;
            }

            IsReady.Value = true;
            ActivationBlockReason.Value = UnitActivationBlockReason.None;
            RefreshAvailabilityVisualState();
        }

        private void OnAvatarCooldownChanged(AvatarCooldownChangedEvent e)
        {
            if (e.Side != _side)
                return;

            _isOnCooldown = e.RemainingSeconds > 0f;
            CooldownProgress.Value = (e.RemainingSeconds, e.DurationSeconds);
            RefreshReadyState();
        }

        private void OnUnitStunChanged(UnitStunChangedEvent e)
        {
            if (e.Unit.Kind != UnitKind.Avatar || e.Unit.Side != _side)
                return;

            _isStunned = e.RemainingSeconds > 0f;
            StunProgress.Value = (e.RemainingSeconds, e.DurationSeconds);
            RefreshReadyState();
        }

        private void RefreshAvailabilityVisualState()
        {
            IsAvailabilityDimmed.Value = _battleActionRuntimeService.CanAcceptNormalActions
                && ActivationBlockReason.Value != UnitActivationBlockReason.None
                && ActivationBlockReason.Value != UnitActivationBlockReason.Cooldown;
        }
    }
}