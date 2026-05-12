using System.Collections.Generic;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Buffs
{
    public class BuffEngine
    {
        private const int MaxRepeatAbilityApplications = 10;
        private const int MaxAdditionalAbilityTargets = 10;


        public IReadOnlyList<BuffRuntimeState> Buffs => _buffs;

        
        private readonly List<BuffRuntimeState> _buffs = new();


        public bool AddBuff(UnitDescriptor source, UnitDescriptor target, TileKind sourceSlotKind, BuffDefinition definition,
            int currentRound, BattlePhaseKind currentPhase, float durationSeconds = 0f)
        {
            if (false == definition.IsConfigured)
                return false;

            for (var i = 0; i < _buffs.Count; i++)
            {
                if (false == IsSameStack(_buffs[i], source, target, sourceSlotKind, definition))
                    continue;

                if (definition.StackingMode == BuffStackingMode.IgnoreNew)
                    return false;

                _buffs[i] = _buffs[i].WithStackAdded(1, currentRound, currentPhase, durationSeconds);
                
                return true;
            }

            _buffs.Add(new BuffRuntimeState(source, target, sourceSlotKind, definition, 1, currentRound,
                currentPhase, durationSeconds));
            
            return true;
        }

        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return false;

            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (false == buff.UsesDuration)
                    continue;

                var nextBuff = buff.WithDurationTicked(deltaTime);
                if (nextBuff.UsesDuration)
                {
                    _buffs[i] = nextBuff;
                    continue;
                }

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
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

        public bool ExpireUntilEndOfNextMainPhaseBuffs(BattlePhaseKind previousPhase, BattlePhaseKind nextPhase)
        {
            if (false == IsMainPhase(previousPhase) || previousPhase == nextPhase)
                return false;

            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (buff.Definition.LifetimeKind != BuffLifetimeKind.UntilEndOfNextMainPhase)
                    continue;

                if (buff.ExpiresAfterMainPhase != previousPhase)
                    continue;

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        public float GetModifiedAbilityPower(float basePower, BattleSide side, int slotIndex)
        {
            return GetModifiedAbilityPower(basePower, UnitDescriptor.Hero(side, slotIndex));
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

        public int GetLineRuneThicknessBonus(BattleSide side)
        {
            var result = 0f;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ModifyLineRuneThickness)
                    continue;

                if (buff.Target.Side != side)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            return BuffRules.ToDisplayInt(result);
        }

        public int GetAbilityRepeatCount(UnitDescriptor source)
        {
            var result = 0f;
            var sourceKey = BattleUnitKey.FromDescriptor(source);
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.RepeatAbilityApplication)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != sourceKey)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            if (result <= 0f)
                return 0;

            var repeatCount = (int)result;
            
            return repeatCount > MaxRepeatAbilityApplications ? MaxRepeatAbilityApplications : repeatCount;
        }

        public int GetAdditionalAbilityTargetCount(UnitDescriptor source)
        {
            var result = 0f;
            var sourceKey = BattleUnitKey.FromDescriptor(source);
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ApplyAbilityToAdditionalTargets)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != sourceKey)
                    continue;

                result = BuffRules.Apply(result, buff.Definition, buff.StackCount);
            }

            if (result <= 0f)
                return 0;

            var targetCount = (int)result;
            
            return targetCount > MaxAdditionalAbilityTargets ? MaxAdditionalAbilityTargets : targetCount;
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

        public int GetResurrectOnDeath(UnitDescriptor target, int maxHP)
        {
            if (maxHP <= 0)
                return 0;

            var total = 0f;
            var targetKey = BattleUnitKey.FromDescriptor(target);
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.ResurrectOnDeath)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != targetKey)
                    continue;

                if (buff.Definition.Operation == BuffModifierOperation.AddPercent)
                    total += maxHP * buff.Definition.Value * buff.StackCount / 100f;
                else
                    total += buff.Definition.Value * buff.StackCount;
            }

            return BuffRules.ToDisplayInt(total);
        }

        public bool ConsumeNextActivationBuffs(UnitDescriptor source)
        {
            var sourceKey = BattleUnitKey.FromDescriptor(source);
            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (buff.Definition.LifetimeKind != BuffLifetimeKind.NextActivation)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != sourceKey)
                    continue;

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
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
            return GetModifiedUnitValue(baseValue, UnitDescriptor.Hero(side, slotIndex), kind);
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
                   && buff.Definition.StackingMode == definition.StackingMode;
        }

        private static bool IsMainPhase(BattlePhaseKind phase)
        {
            return phase is BattlePhaseKind.Match or BattlePhaseKind.Hero;
        }
    }
}