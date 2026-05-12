using System.Collections.Generic;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Shared.Buffs;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Buffs
{
    public class BuffService : IBuffService, IEnergyGainModifierService, IHeroAbilityModifierService,
        IAbilityPowerModifierService, INextAttackBuffService, IBombRadiusModifierService,
        IHeroCooldownModifierService, INextActivationBuffService, IAbilityRepeatModifierService,
        IAbilityAdditionalTargetModifierService, ILineRuneModifierService, IResurrectOnDeathBuffService,
        IStunStatusService
    {
        public IReadOnlyList<BuffRuntimeState> Buffs => _engine.Buffs;

        
        private const int SlotCount = 4;


        private readonly EventBus _eventBus;
        private readonly BattleSetup _battleSetup;
        private readonly BuffEngine _engine = new();


        public BuffService(EventBus eventBus, BattleSetup battleSetup)
        {
            _eventBus = eventBus;
            _battleSetup = battleSetup;
        }


        public bool Tick(float deltaTime)
        {
            var stunTargets = CaptureStunTargets();
            var buffsRemoved = _engine.Tick(deltaTime);
            PublishStunStatuses(stunTargets);
            PublishActiveStunStatuses(stunTargets);

            if (false == buffsRemoved)
                return false;

            _eventBus.Publish(new BuffsChangedEvent());
            PublishAllAbilityStatsChanged();
            
            return true;
        }

        public bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound, BattlePhaseKind currentPhase, float durationSeconds = 0f)
        {
            var added = _engine.AddBuff(source, target, sourceSlotKind, definition, currentRound, currentPhase,
                durationSeconds);
            if (added && definition.Kind == BuffKind.Stun)
                PublishStunStatus(target);

            return added;
        }

        public bool RemoveByUnit(UnitDescriptor unit)
        {
            var stunTargets = CaptureStunTargets();
            var removed = _engine.RemoveByUnit(unit);
            if (removed)
                PublishStunStatuses(stunTargets);

            return removed;
        }

        public bool ExpireUntilEndOfNextMainPhaseBuffs(BattlePhaseKind previousPhase, BattlePhaseKind nextPhase)
        {
            return _engine.ExpireUntilEndOfNextMainPhaseBuffs(previousPhase, nextPhase);
        }

        public bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind)
        {
            return _engine.HasMatchEnergyBuff(side, tileKind);
        }

        public bool HasBuffFromSource(UnitDescriptor source)
        {
            return _engine.HasBuffFromSource(source);
        }

        public StunStatusSnapshot GetStunStatus(UnitDescriptor target)
        {
            return _engine.GetStunStatus(target);
        }

        public bool IsStunned(UnitDescriptor target)
        {
            return GetStunStatus(target).IsActive;
        }

        public float CalculateEnergy(BattleSide side, EnergyGainBreakdown breakdown)
        {
            return CalculateMatchEnergy(side, breakdown.MatchEnergyByKind)
                   + _engine.GetModifiedSpecialActivationEnergy(EnergyGainRules.SumAll(breakdown.SpecialActivationEnergyByKind), side);
        }

        private float CalculateMatchEnergy(BattleSide side, IReadOnlyDictionary<TileKind, float> energyByKind)
        {
            if (null == energyByKind)
                return 0f;

            var total = 0f;
            foreach (var pair in energyByKind)
            {
                if (pair.Value <= 0f)
                    continue;

                total += _engine.GetModifiedMatchEnergy(pair.Value, side, pair.Key);
            }

            return total;
        }

        public int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost)
        {
            return GetActivationEnergyCost(UnitDescriptor.Hero(side, slotIndex), baseCost);
        }

        public int GetActivationEnergyCost(UnitDescriptor unit, int baseCost)
        {
            if (baseCost <= 0)
                return 0;

            return BuffRules.ToDisplayInt(_engine.GetModifiedActivationEnergyCost(baseCost, unit));
        }

        public int GetAbilityPower(BattleSide side, int slotIndex, int basePower)
        {
            return GetAbilityPower(UnitDescriptor.Hero(side, slotIndex), basePower);
        }

        public int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return BuffRules.ToDisplayInt(_engine.GetModifiedAbilityPower(basePower, target));
        }

        public float GetActivationCooldown(UnitDescriptor unit, float baseCooldown)
        {
            if (baseCooldown <= 0f)
                return 0f;

            var cooldown = _engine.GetModifiedActivationCooldown(baseCooldown, unit);
            
            return cooldown < 0f ? 0f : cooldown;
        }

        public int GetBombRadiusBonus(BattleSide side)
        {
            return _engine.GetBombRadiusBonus(side);
        }

        public int GetLineRuneThicknessBonus(BattleSide side)
        {
            return _engine.GetLineRuneThicknessBonus(side);
        }

        public int GetRepeatCount(UnitDescriptor source)
        {
            return _engine.GetAbilityRepeatCount(source);
        }

        public int GetAdditionalTargetCount(UnitDescriptor source)
        {
            return _engine.GetAdditionalAbilityTargetCount(source);
        }

        public int Get(UnitDescriptor source)
        {
            return _engine.GetNextAttackDamage(source);
        }

        public int Consume(UnitDescriptor source)
        {
            return _engine.ConsumeNextAttackDamage(source);
        }

        bool INextActivationBuffService.Consume(UnitDescriptor source)
        {
            return _engine.ConsumeNextActivationBuffs(source);
        }

        public int GetResurrectOnDeath(UnitDescriptor target, int maxHP)
        {
            return _engine.GetResurrectOnDeath(target, maxHP);
        }

        public void Grant(IReadOnlyList<UnitDescriptor> targets, int amount)
        {
            var definition = new BuffDefinition(BuffKind.NextAttackDamage, ValueModifierOperation.AddFlat,
                amount, BuffLifetimeKind.NextAttack, BuffStackingMode.Stack);

            for (var i = 0; i < targets.Count; i++)
                _engine.AddBuff(targets[i], targets[i], TileKind.None, definition, 0, BattlePhaseKind.Hero);
        }

        private List<UnitDescriptor> CaptureStunTargets()
        {
            var result = new List<UnitDescriptor>();
            var buffs = _engine.Buffs;
            for (var i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                if (buff.Definition.Kind == BuffKind.Stun)
                    AddUnique(result, buff.Target);
            }

            return result;
        }

        private void PublishActiveStunStatuses(List<UnitDescriptor> knownTargets)
        {
            var buffs = _engine.Buffs;
            for (var i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                if (buff.Definition.Kind != BuffKind.Stun)
                    continue;

                if (ContainsUnit(knownTargets, buff.Target))
                    continue;

                PublishStunStatus(buff.Target);
                AddUnique(knownTargets, buff.Target);
            }
        }

        private void PublishStunStatuses(List<UnitDescriptor> targets)
        {
            for (var i = 0; i < targets.Count; i++)
                PublishStunStatus(targets[i]);
        }

        private void PublishStunStatus(UnitDescriptor target)
        {
            var status = _engine.GetStunStatus(target);
            _eventBus.Publish(new UnitStunChangedEvent(target, status.RemainingSeconds, status.DurationSeconds));
        }

        private static void AddUnique(List<UnitDescriptor> units, UnitDescriptor unit)
        {
            if (ContainsUnit(units, unit))
                return;

            units.Add(unit);
        }

        private static bool ContainsUnit(IReadOnlyList<UnitDescriptor> units, UnitDescriptor unit)
        {
            for (var i = 0; i < units.Count; i++)
                if (IsSameUnit(units[i], unit))
                    return true;

            return false;
        }

        private static bool IsSameUnit(UnitDescriptor left, UnitDescriptor right)
        {
            return left.Side == right.Side && left.Kind == right.Kind && left.SlotIndex == right.SlotIndex;
        }

        private void PublishAllAbilityStatsChanged()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                PublishHeroAbilityStatsChanged(BattleSide.Player, i);
                PublishHeroAbilityStatsChanged(BattleSide.Enemy, i);
            }

            PublishAvatarAbilityStatsChanged(BattleSide.Player);
            PublishAvatarAbilityStatsChanged(BattleSide.Enemy);
        }

        private void PublishHeroAbilityStatsChanged(BattleSide side, int slotIndex)
        {
            var unit = _battleSetup.GetHero(side, slotIndex);
            if (false == unit.IsAssigned)
                return;

            _eventBus.Publish(new HeroAbilityStatsChangedEvent(side, slotIndex,
                GetActivationEnergyCost(side, slotIndex, unit.BaseActivationEnergyCost),
                GetAbilityPower(unit.Unit, unit.BaseAbilityPower)));
        }

        private void PublishAvatarAbilityStatsChanged(BattleSide side)
        {
            var unit = side == BattleSide.Player ? _battleSetup.PlayerAvatar : _battleSetup.EnemyAvatar;
            if (false == unit.IsAssigned)
                return;

            _eventBus.Publish(new AvatarAbilityPowerChangedEvent(side,
                GetActivationEnergyCost(unit.Unit, unit.BaseActivationEnergyCost),
                GetAbilityPower(unit.Unit, unit.BaseAbilityPower)));
        }
    }
}