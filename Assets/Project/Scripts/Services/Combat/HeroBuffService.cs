using System.Collections.Generic;
using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Combat
{
    public class HeroBuffService : IBuffService, IEnergyGainModifierService, IHeroAbilityModifierService,
        IAbilityPowerModifierService, INextAttackBuffService, IBombRadiusModifierService,
        IHeroCooldownModifierService, INextActivationBuffService, IAbilityRepeatModifierService
    {
        public IReadOnlyList<BuffRuntimeState> Buffs => _engine.Buffs;

        
        private readonly BuffEngine _engine = new();


        public bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound)
        {
            return _engine.AddBuff(source, target, sourceSlotKind, definition, currentRound);
        }

        public bool RemoveByUnit(UnitDescriptor unit)
        {
            return _engine.RemoveByUnit(unit);
        }

        public bool ExpireRoundLimitedBuffs(int currentRound)
        {
            return _engine.ExpireRoundLimitedBuffs(currentRound);
        }

        public bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind)
        {
            return _engine.HasMatchEnergyBuff(side, tileKind);
        }

        public bool HasBuffFromSource(UnitDescriptor source)
        {
            return _engine.HasBuffFromSource(source);
        }

        public float CalculateEnergy(BattleSide side, EnergyGainBreakdown breakdown)
        {
            return CalculateMatchEnergy(side, breakdown.MatchEnergyByKind)
                   + _engine.GetModifiedSpecialActivationEnergy(
                       EnergyGainRules.SumAll(breakdown.SpecialActivationEnergyByKind), side);
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
            if (baseCost <= 0)
                return 0;

            return BuffRules.ToDisplayInt(_engine.GetModifiedActivationEnergyCost(baseCost, side, slotIndex));
        }

        public int GetAbilityPower(BattleSide side, int slotIndex, int basePower)
        {
            return GetAbilityPower(UnitDescriptor.Hero(side, slotIndex, HeroActionType.DealDamage), basePower);
        }

        public int GetAbilityPower(UnitDescriptor target, int basePower)
        {
            return BuffRules.ToDisplayInt(_engine.GetModifiedAbilityPower(basePower, target));
        }

        public float GetActivationCooldown(BattleSide side, int slotIndex, float baseCooldown)
        {
            if (baseCooldown <= 0f)
                return 0f;

            var cooldown = _engine.GetModifiedActivationCooldown(baseCooldown, side, slotIndex);
            
            return cooldown < 0f ? 0f : cooldown;
        }

        public int GetBombRadiusBonus(BattleSide side)
        {
            return _engine.GetBombRadiusBonus(side);
        }

        public int GetRepeatCount(UnitDescriptor source)
        {
            return _engine.GetAbilityRepeatCount(source);
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

        public void Grant(IReadOnlyList<UnitDescriptor> targets, int amount)
        {
            var definition = new BuffDefinition(BuffKind.NextAttackDamage, BuffModifierOperation.AddFlat,
                amount, BuffLifetimeKind.NextAttack, 0, BuffStackingMode.Stack);

            for (var i = 0; i < targets.Count; i++)
                _engine.AddBuff(targets[i], targets[i], TileKind.None, definition, 0);
        }
    }
}