using System;
using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Clock;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public class HeroService : IHeroService, IDisposable
    {
        private const int SlotCount = 4;


        private readonly EventBus _eventBus;
        private readonly IResurrectOnDeathBuffService _resurrectOnDeathBuffService;
        private readonly IShieldService _shieldService;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleClock _battleClock;
        private readonly IBattleFlowService _battleFlowService;
        private readonly HeroSlotState[] _playerSlots = new HeroSlotState[SlotCount];
        private readonly HeroSlotState[] _enemySlots = new HeroSlotState[SlotCount];


        public HeroService(EventBus eventBus, BattleSetup battleSetup,
            IResurrectOnDeathBuffService resurrectOnDeathBuffService,
            IShieldService shieldService, IGameStateService gameStateService, IBattleClock battleClock,
            IBattleFlowService battleFlowService)
        {
            _eventBus = eventBus;
            _resurrectOnDeathBuffService = resurrectOnDeathBuffService;
            _shieldService = shieldService;
            _gameStateService = gameStateService;
            _battleClock = battleClock;
            _battleFlowService = battleFlowService;

            InitSlots(_playerSlots, battleSetup, BattleSide.Player);
            InitSlots(_enemySlots, battleSetup, BattleSide.Enemy);

            PublishInitialHPEvents(_playerSlots, BattleSide.Player);
            PublishInitialHPEvents(_enemySlots, BattleSide.Enemy);
        }


        public IReadOnlyList<HeroSlotState> GetSlots(BattleSide side)
        {
            return side == BattleSide.Player ? _playerSlots : _enemySlots;
        }

        public void ApplyDamageToHero(BattleSide side, int slotIndex, int amount, bool silent = false)
        {
            if (slotIndex is < 0 or >= SlotCount || amount <= 0)
                return;

            var target = UnitDescriptor.Hero(side, slotIndex);
            var absorption = _shieldService.AbsorbDamage(target, amount, _battleFlowService.Snapshot.Phase);
            amount = absorption.RemainingDamage;
            if (amount <= 0)
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

        public bool TryResurrectHero(BattleSide side, int slotIndex, int restoredHP, out int actualRestoredHP,
            long occurredAtTick = 0)
        {
            actualRestoredHP = 0;
            if (slotIndex is < 0 or >= SlotCount || restoredHP <= 0)
                return false;

            ref var slot = ref GetSlotRef(side, slotIndex);
            if (false == slot.IsAssigned || slot.MaxHP <= 0 || slot.CurrentHP > 0)
                return false;

            actualRestoredHP = restoredHP > slot.MaxHP ? slot.MaxHP : restoredHP;
            slot.CurrentHP = actualRestoredHP;

            var resolvedTick = occurredAtTick > 0 ? occurredAtTick : _battleClock.CurrentTick;
            _eventBus.Publish(new HeroResurrectedEvent(side, slotIndex, actualRestoredHP, resolvedTick));
            _eventBus.Publish(new HeroHPChangedEvent(side, slotIndex, slot.CurrentHP, slot.MaxHP));

            return true;
        }

        public void Dispose()
        {
        }


        private void ApplyHPChange(ref HeroSlotState slot, BattleSide side, int slotIndex, int delta, bool silent = false)
        {
            if (false == slot.IsAssigned || slot.MaxHP <= 0)
                return;

            var owner = UnitDescriptor.Hero(side, slotIndex);
            var resurrectChargeHP = _resurrectOnDeathBuffService.GetResurrectOnDeath(owner, slot.MaxHP);
            var isBurndownActive = _gameStateService.State.CurrentValue == GameState.Burndown;
            var resolution = HeroDeathResolutionRules.Resolve(slot.CurrentHP, slot.MaxHP, delta, resurrectChargeHP,
                isBurndownActive);

            if (false == resolution.HealthChange.WasChanged)
                return;

            slot.CurrentHP = resolution.HealthChange.CurrentHP;

            if (resolution.WasResurrected)
            {
                _eventBus.Publish(new HeroResurrectedEvent(side, slotIndex, resolution.ResurrectedHP,
                    _battleClock.CurrentTick));
                _eventBus.Publish(new HeroHPChangedEvent(side, slotIndex, slot.CurrentHP, slot.MaxHP, silent));
                
                return;
            }

            _eventBus.Publish(new HeroHPChangedEvent(side, slotIndex, slot.CurrentHP, slot.MaxHP, silent));

            if (false == resolution.HealthChange.BecameDefeated)
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

        private static void InitSlots(HeroSlotState[] slots, BattleSetup battleSetup, BattleSide side)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                var setup = battleSetup.GetHero(side, i);
                if (false == setup.IsAssigned)
                {
                    slots[i] = new HeroSlotState { SlotKind = setup.SlotKind };
                    continue;
                }

                slots[i] = new HeroSlotState
                {
                    SlotKind = setup.SlotKind,
                    IsAssigned = true,
                    ActivationEnergyCost = setup.BaseActivationEnergyCost,
                    ActionType = setup.ActionType,
                    ActionValue = setup.BaseAbilityPower,
                    CurrentHP = setup.MaxHP,
                    MaxHP = setup.MaxHP,
                };
            }
        }
    }
}