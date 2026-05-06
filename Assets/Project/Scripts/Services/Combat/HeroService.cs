using System;
using System.Collections.Generic;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Combat
{
    public class HeroService : IHeroService, IDisposable
    {
        private const int SlotCount = 4;


        private readonly EventBus _eventBus;
        private readonly IPlayerStateService _playerState;
        private readonly IEnemyStateService _enemyState;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IUnitActivationCooldownService _unitActivationCooldownService;
        private readonly IHeroAbilityModifierService _heroAbilityModifierService;
        private readonly INextAttackBuffService _nextAttackBuffService;
        private readonly IBattleClock _battleClock;
        private readonly HeroSlotState[] _playerSlots = new HeroSlotState[SlotCount];
        private readonly HeroSlotState[] _enemySlots = new HeroSlotState[SlotCount];


        public HeroService(EventBus eventBus, LevelConfig levelConfig, SlotLayoutConfig slotLayoutConfig,
            IPlayerStateService playerState, IEnemyStateService enemyState, IBattleSideEnergyService battleSideEnergyService,
            IBattleActionRuntimeService battleActionRuntimeService, IUnitActivationCooldownService unitActivationCooldownService,
            IHeroAbilityModifierService heroAbilityModifierService, INextAttackBuffService nextAttackBuffService,
            IBattleClock battleClock)
        {
            _eventBus = eventBus;
            _playerState = playerState;
            _enemyState = enemyState;
            _battleSideEnergyService = battleSideEnergyService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _unitActivationCooldownService = unitActivationCooldownService;
            _heroAbilityModifierService = heroAbilityModifierService;
            _nextAttackBuffService = nextAttackBuffService;
            _battleClock = battleClock;

            InitSlots(_playerSlots, levelConfig.PlayerHeroes, slotLayoutConfig.HeroSlotKinds);
            InitSlots(_enemySlots, levelConfig.EnemyHeroes, slotLayoutConfig.HeroSlotKinds);

            PublishInitialHPEvents(_playerSlots, BattleSide.Player);
            PublishInitialHPEvents(_enemySlots, BattleSide.Enemy);
        }


        public IReadOnlyList<HeroSlotState> GetSlots(BattleSide side)
        {
            return side == BattleSide.Player ? _playerSlots : _enemySlots;
        }

        public bool CanActivate(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return false;

            ref var slot = ref GetSlotRef(side, slotIndex);
            if (false == slot.IsAssigned || false == slot.IsAlive)
                return false;

            if (slot.ActionType == HeroActionType.HealAlly && false == HasHealTarget(side, slotIndex))
                return false;

            if (_unitActivationCooldownService.IsHeroOnCooldown(side, slotIndex))
                return false;

            return _battleSideEnergyService.CanSpend(side, GetActivationEnergyCost(side, slotIndex, slot));
        }

        public void TryActivate(int slotIndex)
        {
            TryActivate(BattleSide.Player, slotIndex);
        }

        public void TryActivate(BattleSide side, int slotIndex)
        {
            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.HeroActivation).IsAllowed)
                return;

            if (slotIndex is < 0 or >= SlotCount)
                return;

            if (side == BattleSide.Player)
                TryActivateSlot(ref _playerSlots[slotIndex], BattleSide.Player, slotIndex);
            else
                TryActivateSlot(ref _enemySlots[slotIndex], BattleSide.Enemy, slotIndex);
        }

        public bool TryDischargeHero(BattleSide side, int slotIndex, out HeroActionType actionType, out int actionValue)
        {
            actionType = default;
            actionValue = 0;

            if (false == _battleActionRuntimeService.Evaluate(BattleActionKind.AbilityCommit).IsAllowed)
                return false;

            if (slotIndex is < 0 or >= SlotCount)
                return false;

            ref var slot = ref GetSlotRef(side, slotIndex);

            if (false == CanActivate(side, slotIndex))
                return false;

            var activationEnergyCost = GetActivationEnergyCost(side, slotIndex, slot);
            actionType = slot.ActionType;
            var baseActionValue = GetAbilityPower(side, slotIndex, slot);

            if (false == _battleSideEnergyService.TrySpend(side, activationEnergyCost))
                return false;

            _unitActivationCooldownService.StartHeroCooldown(side, slotIndex);
            actionValue = GetActionValueWithNextAttackBuff(side, slotIndex, actionType, baseActionValue);
            
            return true;
        }

        public void ApplyDamageToHero(BattleSide side, int slotIndex, int amount, bool silent = false)
        {
            if (slotIndex is < 0 or >= SlotCount || amount <= 0)
                return;

            ref var slot = ref GetSlotRef(side, slotIndex);
            ApplyHPChange(ref slot, side, slotIndex, -amount, silent);
        }

        public void ApplyHealToHero(BattleSide side, int slotIndex, int amount)
        {
            if (slotIndex is < 0 or >= SlotCount || amount <= 0)
                return;

            ref var slot = ref GetSlotRef(side, slotIndex);
            ApplyHPChange(ref slot, side, slotIndex, +amount);
        }

        public void Dispose()
        {
        }


        private bool HasHealTarget(BattleSide side, int sourceSlotIndex)
        {
            if (side == BattleSide.Player && _playerState.CurrentHP < _playerState.MaxHP)
                return true;

            if (side == BattleSide.Enemy && _enemyState.CurrentHP < _enemyState.MaxHP)
                return true;

            var slots = side == BattleSide.Player ? _playerSlots : _enemySlots;
            for (var i = 0; i < slots.Length; i++)
            {
                if (i == sourceSlotIndex)
                    continue;

                var slot = slots[i];
                if (false == slot.IsAssigned || false == slot.IsAlive || slot.MaxHP <= 0)
                    continue;

                if (slot.CurrentHP < slot.MaxHP)
                    return true;
            }

            return false;
        }

        private void TryActivateSlot(ref HeroSlotState slot, BattleSide side, int slotIndex)
        {
            if (false == CanActivate(side, slotIndex))
                return;

            var activationEnergyCost = GetActivationEnergyCost(side, slotIndex, slot);
            if (false == _battleSideEnergyService.TrySpend(side, activationEnergyCost))
                return;

            _unitActivationCooldownService.StartHeroCooldown(side, slotIndex);
            var actionValue = GetActionValueWithNextAttackBuff(side, slotIndex, slot.ActionType, GetAbilityPower(side, slotIndex, slot));
            _eventBus.Publish(new HeroActivatedEvent(side, slotIndex, slot.ActionType, actionValue,
                _battleClock.CurrentTick));
        }

        private int GetActivationEnergyCost(BattleSide side, int slotIndex, HeroSlotState slot)
        {
            return _heroAbilityModifierService.GetActivationEnergyCost(side, slotIndex, slot.ActivationEnergyCost);
        }

        private int GetAbilityPower(BattleSide side, int slotIndex, HeroSlotState slot)
        {
            return _heroAbilityModifierService.GetAbilityPower(side, slotIndex, slot.ActionValue);
        }

        private int GetActionValueWithNextAttackBuff(BattleSide side, int slotIndex, HeroActionType actionType,
            int baseActionValue)
        {
            if (actionType != HeroActionType.DealDamage)
                return baseActionValue;

            var source = UnitDescriptor.Hero(side, slotIndex, actionType);
            return baseActionValue + _nextAttackBuffService.Consume(source);
        }

        private void ApplyHPChange(ref HeroSlotState slot, BattleSide side, int slotIndex, int delta, bool silent = false)
        {
            if (false == slot.IsAssigned || slot.MaxHP <= 0)
                return;

            var result = HealthChangeRules.Apply(slot.CurrentHP, slot.MaxHP, delta);
            if (false == result.WasChanged)
                return;

            slot.CurrentHP = result.CurrentHP;
            _eventBus.Publish(new HeroHPChangedEvent(side, slotIndex, slot.CurrentHP, slot.MaxHP, silent));

            if (false == result.BecameDefeated)
                return;

            _eventBus.Publish(new HeroDefeatedEvent(side, slotIndex, slot.SlotKind, _battleClock.CurrentTick));
        }

        private ref HeroSlotState GetSlotRef(BattleSide side, int slotIndex)
        {
            var slots = side == BattleSide.Player ? _playerSlots : _enemySlots;
            
            return ref slots[slotIndex];
        }

        private void PublishInitialHPEvents(HeroSlotState[] slots, BattleSide side)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (false == slots[i].IsAssigned)
                    continue;

                _eventBus.Publish(new HeroHPChangedEvent(side, i, slots[i].CurrentHP, slots[i].MaxHP));
            }
        }

        private static void InitSlots(HeroSlotState[] slots, HeroConfig[] configs, TileKind[] slotKinds)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                var slotKind = (slotKinds != null && i < slotKinds.Length) ? slotKinds[i] : default;
                var config = (configs != null && i < configs.Length) ? configs[i] : null;
                if (!config)
                {
                    slots[i] = new HeroSlotState { SlotKind = slotKind };
                    continue;
                }

                slots[i] = new HeroSlotState
                {
                    SlotKind = slotKind,
                    IsAssigned = true,
                    ActivationEnergyCost = config.ActivationEnergyCost,
                    ActionType = config.AbilityType,
                    ActionValue = config.AbilityPower,
                    CurrentHP = config.MaxHP,
                    MaxHP = config.MaxHP,
                };
            }
        }
    }
}