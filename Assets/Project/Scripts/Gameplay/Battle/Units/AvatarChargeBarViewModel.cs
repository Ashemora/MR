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


        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly BattleSide _side;
        private readonly int _activationEnergyCost;
        private bool _hasSufficientEnergy;
        private bool _isOnCooldown;


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
            _subscriptions.Add(_battleActionRuntimeService.State.Subscribe(_ => RefreshReadyState()));
        }

        public void Dispose()
        {
            FillFraction.Dispose();
            IsReady.Dispose();
            ActivationBlockReason.Dispose();
            IsAvailabilityDimmed.Dispose();
            CooldownProgress.Dispose();
            _subscriptions.Dispose();
        }

        private void OnBattleSideEnergyChanged(BattleSideEnergyChangedEvent e)
        {
            if (e.Side != _side)
                return;

            FillFraction.Value = _activationEnergyCost > 0 ? Mathf.Clamp01((float)e.Current / _activationEnergyCost) : 0f;
            _hasSufficientEnergy = _activationEnergyCost <= 0 || e.Current >= _activationEnergyCost;
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

        private void RefreshAvailabilityVisualState()
        {
            IsAvailabilityDimmed.Value = _battleActionRuntimeService.CanAcceptNormalActions
                && ActivationBlockReason.Value != UnitActivationBlockReason.None
                && ActivationBlockReason.Value != UnitActivationBlockReason.Cooldown;
        }
    }
}