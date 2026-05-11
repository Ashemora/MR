using System;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Game;
using R3;
using UnityEngine;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Units
{
    public class HeroSlotViewModel : IDisposable
    {
        public bool IsAssigned { get; }
        public Color SlotColor { get; }
        public Sprite Portrait { get; }
        public int SlotIndex { get; }
        public BattleSide Side { get; }
        public HeroActionType ActionType { get; }
        public int ActivationEnergyCost => ActivationEnergyCostChanged.Value;
        public int AbilityPower => AbilityPowerChanged.Value;
        public bool IsPlayerSlot => Side == BattleSide.Player;
        public ReactiveProperty<bool>  IsActivatable { get; } = new(false);
        public ReactiveProperty<UnitActivationBlockReason> ActivationBlockReason { get; } = new(UnitActivationBlockReason.None);
        public ReactiveProperty<int> ActivationEnergyCostChanged { get; }
        public ReactiveProperty<int> AbilityPowerChanged { get; }
        public ReactiveProperty<bool> IsAvailabilityDimmed { get; } = new(false);
        public ReactiveProperty<bool> IsSlotKindPassiveActive { get; } = new(false);
        public ReactiveProperty<(float Remaining, float Duration)> CooldownProgress { get; } = new((0f, 0f));
        public ReactiveProperty<float> HPFill { get; }
        public ReactiveProperty<bool>  IsDefeated { get; } = new(false);
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public Observable<HealthBarUpdate> HealthBarUpdated => _healthBarUpdated;
        public Observable<int> Hit => _hit;
        public Observable<int> Heal => _heal;


        private readonly Subject<HealthBarUpdate> _healthBarUpdated = new();
        private readonly Subject<int> _hit = new();
        private readonly Subject<int> _heal = new();
        private readonly CompositeDisposable _subscriptions = new();
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private int _prevHP;
        private int _currentEnergy;
        private bool _hasSufficientEnergy;
        private bool _isOnCooldown;


        public HeroSlotViewModel(int slotIndex, BattleSide side, HeroSlotState state, Color color, 
            Sprite portrait, EventBus eventBus, IUnitActivationCooldownService cooldownService, 
            IBattleActionRuntimeService battleActionRuntimeService)
        {
            SlotIndex = slotIndex;
            Side = side;
            IsAssigned = state.IsAssigned;
            ActionType = state.ActionType;
            ActivationEnergyCostChanged = new ReactiveProperty<int>(state.ActivationEnergyCost);
            AbilityPowerChanged = new ReactiveProperty<int>(state.ActionValue);
            SlotColor = color;
            Portrait = portrait;
            _battleActionRuntimeService = battleActionRuntimeService;

            HPFill = new ReactiveProperty<float>(state.IsAssigned && state.MaxHP > 0 ? (float)state.CurrentHP / state.MaxHP : 1f);
            _prevHP = state.IsAssigned ? state.CurrentHP : 0;
            CurrentHP = _prevHP;
            MaxHP = state.IsAssigned ? state.MaxHP : 0;
            IsDefeated.Value = state.IsAssigned && state.CurrentHP <= 0;
            _hasSufficientEnergy = ActivationEnergyCost <= 0;
            _isOnCooldown = cooldownService.IsHeroOnCooldown(side, slotIndex);
            RefreshActivatable();

            _subscriptions.Add(_battleActionRuntimeService.State.Subscribe(_ => RefreshActivatable()));
            _subscriptions.Add(eventBus.Subscribe<BattleSideEnergyChangedEvent>(OnBattleSideEnergyChanged));
            _subscriptions.Add(eventBus.Subscribe<HeroCooldownChangedEvent>(OnHeroCooldownChanged));
        }

        public void UpdateHP(int current, int max, bool silent = false)
        {
            var fill = max > 0 ? (float)current / max : 0f;
            var previousHP = _prevHP;

            _prevHP = current;
            CurrentHP = current;
            MaxHP = max;
            HPFill.Value = fill;

            if (silent)
            {
                _healthBarUpdated.OnNext(new HealthBarUpdate(fill, HealthBarUpdateMode.Snap, current, max));

                if (current <= 0)
                    IsDefeated.Value = true;

                return;
            }

            if (current < previousHP)
            {
                _hit.OnNext(previousHP - current);
                _healthBarUpdated.OnNext(new HealthBarUpdate(fill, HealthBarUpdateMode.Damage, current, max));
            }
            else if (current > previousHP)
            {
                _heal.OnNext(current - previousHP);
                _healthBarUpdated.OnNext(new HealthBarUpdate(fill, HealthBarUpdateMode.Heal, current, max));
            }
            else
                _healthBarUpdated.OnNext(new HealthBarUpdate(fill, HealthBarUpdateMode.Snap, current, max));

            if (current <= 0)
                IsDefeated.Value = true;
        }

        public void SetSlotKindPassiveActive(bool active)
        {
            IsSlotKindPassiveActive.Value = active;
        }

        public void UpdateAbilityStats(int activationEnergyCost, int abilityPower)
        {
            ActivationEnergyCostChanged.Value = activationEnergyCost;
            AbilityPowerChanged.Value = abilityPower;
            _hasSufficientEnergy = activationEnergyCost <= 0 || _currentEnergy >= activationEnergyCost;
            RefreshActivatable();
        }

        public void Dispose()
        {
            IsActivatable.Dispose();
            ActivationBlockReason.Dispose();
            ActivationEnergyCostChanged.Dispose();
            AbilityPowerChanged.Dispose();
            IsAvailabilityDimmed.Dispose();
            IsSlotKindPassiveActive.Dispose();
            CooldownProgress.Dispose();
            HPFill.Dispose();
            IsDefeated.Dispose();
            _healthBarUpdated.Dispose();
            _hit.Dispose();
            _heal.Dispose();
            _subscriptions.Dispose();
        }


        private void RefreshActivatable()
        {
            if (false == IsAssigned)
            {
                IsActivatable.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.None;
                RefreshAvailabilityVisualState();
                
                return;
            }

            var gateResult = _battleActionRuntimeService.Evaluate(BattleActionKind.HeroActivation);
            if (false == gateResult.IsAllowed)
            {
                IsActivatable.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.BlockedByPhase;
                RefreshAvailabilityVisualState();
                
                return;
            }

            if (false == _hasSufficientEnergy)
            {
                IsActivatable.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.InsufficientEnergy;
                RefreshAvailabilityVisualState();
                
                return;
            }

            if (_isOnCooldown)
            {
                IsActivatable.Value = false;
                ActivationBlockReason.Value = UnitActivationBlockReason.Cooldown;
                RefreshAvailabilityVisualState();
                
                return;
            }

            IsActivatable.Value = true;
            ActivationBlockReason.Value = UnitActivationBlockReason.None;
            RefreshAvailabilityVisualState();
        }

        private void OnBattleSideEnergyChanged(BattleSideEnergyChangedEvent e)
        {
            if (e.Side != Side)
                return;

            _currentEnergy = e.Current;
            _hasSufficientEnergy = ActivationEnergyCost <= 0 || e.Current >= ActivationEnergyCost;
            RefreshActivatable();
        }

        private void OnHeroCooldownChanged(HeroCooldownChangedEvent e)
        {
            if (e.Side != Side || e.SlotIndex != SlotIndex)
                return;

            _isOnCooldown = e.RemainingSeconds > 0f;
            CooldownProgress.Value = (e.RemainingSeconds, e.DurationSeconds);
            RefreshActivatable();
        }

        private void RefreshAvailabilityVisualState()
        {
            IsAvailabilityDimmed.Value = IsAssigned
                && _battleActionRuntimeService.CanAcceptNormalActions
                && ActivationBlockReason.Value != UnitActivationBlockReason.None
                && ActivationBlockReason.Value != UnitActivationBlockReason.Cooldown;
        }
    }
}