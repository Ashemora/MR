using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Passives
{
    public class BuffEngine
    {
        public IReadOnlyList<BuffRuntimeState> Buffs => _buffs;

        
        private readonly List<BuffRuntimeState> _buffs = new();


        public bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound)
        {
            if (false == definition.IsConfigured)
                return false;

            for (var i = 0; i < _buffs.Count; i++)
            {
                if (false == IsSameStack(_buffs[i], source, target, sourceSlotKind, definition))
                    continue;

                if (definition.StackingMode == BuffStackingMode.IgnoreNew)
                    return false;

                _buffs[i] = _buffs[i].WithStackAdded(1, currentRound);
                return true;
            }

            _buffs.Add(new BuffRuntimeState(source, target, sourceSlotKind, definition, 1, currentRound));
            
            return true;
        }

        public bool RemoveByUnit(UnitDescriptor unit)
        {
            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                if (BattleUnitKey.FromDescriptor(_buffs[i].Source) != BattleUnitKey.FromDescriptor(unit)
                    && BattleUnitKey.FromDescriptor(_buffs[i].Target) != BattleUnitKey.FromDescriptor(unit))
                    continue;

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        public bool ExpireRoundLimitedBuffs(int currentRound)
        {
            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (buff.Definition.LifetimeKind != BuffLifetimeKind.Rounds || buff.ExpiresAtRound <= 0)
                    continue;

                if (currentRound < buff.ExpiresAtRound)
                    continue;

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        public float GetModifiedAbilityPower(float basePower, BattleSide side, int slotIndex)
        {
            return GetModifiedAbilityPower(basePower, UnitDescriptor.Hero(side, slotIndex, HeroActionType.DealDamage));
        }

        public float GetModifiedAbilityPower(float basePower, UnitDescriptor target)
        {
            return GetModifiedUnitValue(basePower, target, BuffKind.ModifyAbilityPower);
        }

        public float GetModifiedActivationEnergyCost(float baseCost, BattleSide side, int slotIndex)
        {
            return GetModifiedHeroValue(baseCost, side, slotIndex, BuffKind.ModifyActivationEnergyCost);
        }

        public float GetModifiedActivationCooldown(float baseCooldown, BattleSide side, int slotIndex)
        {
            return GetModifiedHeroValue(baseCooldown, side, slotIndex, BuffKind.ModifyActivationCooldown);
        }

        public float GetModifiedMatchEnergy(float baseEnergy, BattleSide side, TileKind tileKind)
        {
            var result = baseEnergy;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ModifyMatchEnergyBySlotKind)
                    continue;

                if (buff.Target.Side != side || buff.SourceSlotKind != tileKind)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            return result < 0f ? 0f : result;
        }

        public float GetModifiedSpecialActivationEnergy(float baseEnergy, BattleSide side)
        {
            var result = baseEnergy;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ModifySpecialTileActivationEnergy)
                    continue;

                if (buff.Target.Side != side)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            return result < 0f ? 0f : result;
        }

        public int GetBombRadiusBonus(BattleSide side)
        {
            var result = 0f;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ModifyBombRadius)
                    continue;

                if (buff.Target.Side != side)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            return BuffRules.ToDisplayInt(result);
        }

        public int GetNextAttackDamage(UnitDescriptor source)
        {
            var total = 0f;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (false == IsNextAttackDamageBuff(buff, source))
                    continue;

                total += buff.Definition.Value * buff.StackCount;
            }

            return BuffRules.ToDisplayInt(total);
        }

        public int ConsumeNextAttackDamage(UnitDescriptor source)
        {
            var total = 0f;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (false == IsNextAttackDamageBuff(buff, source))
                    continue;

                total += buff.Definition.Value * buff.StackCount;
                _buffs.RemoveAt(i);
            }

            return BuffRules.ToDisplayInt(total);
        }

        public bool HasMatchEnergyBuff(BattleSide side, TileKind tileKind)
        {
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind == BuffKind.ModifyMatchEnergyBySlotKind
                    && buff.Target.Side == side
                    && buff.SourceSlotKind == tileKind)
                    return true;
            }

            return false;
        }

        public bool HasBuffFromSource(UnitDescriptor source)
        {
            var key = BattleUnitKey.FromDescriptor(source);
            for (var i = 0; i < _buffs.Count; i++)
                if (BattleUnitKey.FromDescriptor(_buffs[i].Source) == key)
                    return true;

            return false;
        }

        private float GetModifiedHeroValue(float baseValue, BattleSide side, int slotIndex, BuffKind kind)
        {
            return GetModifiedUnitValue(baseValue, UnitDescriptor.Hero(side, slotIndex, HeroActionType.DealDamage), kind);
        }

        private float GetModifiedUnitValue(float baseValue, UnitDescriptor target, BuffKind kind)
        {
            var result = baseValue;
            var targetKey = BattleUnitKey.FromDescriptor(target);
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != kind)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != targetKey)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            return result;
        }

        private static bool IsNextAttackDamageBuff(BuffRuntimeState buff, UnitDescriptor source)
        {
            return buff.Definition.Kind == BuffKind.NextAttackDamage
                   && BattleUnitKey.FromDescriptor(buff.Target) == BattleUnitKey.FromDescriptor(source);
        }

        private static bool IsSameStack(BuffRuntimeState buff, UnitDescriptor source, UnitDescriptor target,
            TileKind sourceSlotKind, BuffDefinition definition)
        {
            return BattleUnitKey.FromDescriptor(buff.Source) == BattleUnitKey.FromDescriptor(source)
                   && BattleUnitKey.FromDescriptor(buff.Target) == BattleUnitKey.FromDescriptor(target)
                   && buff.SourceSlotKind == sourceSlotKind
                   && buff.Definition.Kind == definition.Kind
                   && buff.Definition.Operation == definition.Operation
                   && buff.Definition.Value.Equals(definition.Value)
                   && buff.Definition.LifetimeKind == definition.LifetimeKind
                   && buff.Definition.DurationRounds == definition.DurationRounds
                   && buff.Definition.StackingMode == definition.StackingMode;
        }
    }
}