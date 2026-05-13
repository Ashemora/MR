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

            if (definition.Kind == BuffKind.Stun && durationSeconds <= 0f)
                return false;

            if (definition.Kind == BuffKind.Shield && IsUnsupportedShieldLifetime(definition.LifetimeKind))
                return false;

            if (definition.StackingMode == BuffStackingMode.IgnoreNew)
            {
                for (var i = 0; i < _buffs.Count; i++)
                    if (IsSameStack(_buffs[i], source, target, sourceSlotKind, definition))
                        return false;
            }

            var state = new BuffRuntimeState(source, target, sourceSlotKind, definition, 1, currentRound,
                currentPhase, durationSeconds);
            if (definition.Kind == BuffKind.Shield && state.ShieldCapacity <= 0)
                return false;

            _buffs.Add(state);
            
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

        public bool ExpireUntilEndOfRoundBuffs(int completedRound)
        {
            if (completedRound <= 0)
                return false;

            var removed = false;
            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (buff.Definition.LifetimeKind != BuffLifetimeKind.UntilEndOfRound)
                    continue;

                if (buff.ExpiresAfterRound != completedRound)
                    continue;

                _buffs.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        public ShieldSnapshot GetShield(UnitDescriptor target)
        {
            var targetKey = BattleUnitKey.FromDescriptor(target);
            var current = 0;
            var capacity = 0;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (false == buff.IsActiveShield)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != targetKey)
                    continue;

                current += buff.ShieldRemaining;
                capacity += buff.ShieldCapacity;
            }

            return new ShieldSnapshot(target, current, capacity);
        }

        public ShieldAbsorptionResult AbsorbShieldDamage(UnitDescriptor target, int damage,
            BattlePhaseKind currentPhase)
        {
            if (damage <= 0)
                return new ShieldAbsorptionResult(target, 0, 0, 0, GetShield(target));

            var remainingDamage = damage;
            var absorbedDamage = 0;
            while (remainingDamage > 0)
            {
                var shieldIndex = FindNextShieldLayerIndex(target, currentPhase);
                if (shieldIndex < 0)
                    break;

                var shield = _buffs[shieldIndex];
                var absorbedByLayer = remainingDamage < shield.ShieldRemaining
                    ? remainingDamage
                    : shield.ShieldRemaining;
                remainingDamage -= absorbedByLayer;
                absorbedDamage += absorbedByLayer;

                var nextShieldRemaining = shield.ShieldRemaining - absorbedByLayer;
                if (nextShieldRemaining <= 0)
                {
                    _buffs.RemoveAt(shieldIndex);
                    continue;
                }

                _buffs[shieldIndex] = shield.WithShieldRemaining(nextShieldRemaining);
            }

            return new ShieldAbsorptionResult(target, damage, absorbedDamage, remainingDamage, GetShield(target));
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
            return GetModifiedActivationEnergyCost(baseCost, UnitDescriptor.Hero(side, slotIndex));
        }

        public float GetModifiedActivationEnergyCost(float baseCost, UnitDescriptor target)
        {
            return GetModifiedUnitValue(baseCost, target, BuffKind.ModifyActivationEnergyCost);
        }

        public float GetModifiedActivationCooldown(float baseCooldown, BattleSide side, int slotIndex)
        {
            return GetModifiedActivationCooldown(baseCooldown, UnitDescriptor.Hero(side, slotIndex));
        }

        public float GetModifiedActivationCooldown(float baseCooldown, UnitDescriptor target)
        {
            return GetModifiedUnitValue(baseCooldown, target, BuffKind.ModifyActivationCooldown);
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

                total += BuffRules.ResolveAdditiveValue(buff.Definition.Operation, buff.Definition.Value, maxHP)
                         * buff.StackCount;
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

        public StunStatusSnapshot GetStunStatus(UnitDescriptor target)
        {
            var targetKey = BattleUnitKey.FromDescriptor(target);
            var best = default(StunStatusSnapshot);
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (buff.Definition.Kind != BuffKind.Stun || false == buff.UsesDuration)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != targetKey)
                    continue;

                if (buff.RemainingDurationSeconds > best.RemainingSeconds)
                    best = new StunStatusSnapshot(buff.RemainingDurationSeconds, buff.DurationSeconds);
            }

            return best;
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

        private int FindNextShieldLayerIndex(UnitDescriptor target, BattlePhaseKind currentPhase)
        {
            var targetKey = BattleUnitKey.FromDescriptor(target);
            var result = -1;
            for (var i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i];
                if (false == buff.IsActiveShield)
                    continue;

                if (BattleUnitKey.FromDescriptor(buff.Target) != targetKey)
                    continue;

                if (result < 0 || CompareShieldConsumptionPriority(buff, _buffs[result], currentPhase) < 0)
                    result = i;
            }

            return result;
        }

        private static int CompareShieldConsumptionPriority(BuffRuntimeState left, BuffRuntimeState right,
            BattlePhaseKind currentPhase)
        {
            var leftRank = GetShieldExpirationRank(left);
            var rightRank = GetShieldExpirationRank(right);
            if (leftRank != rightRank)
                return leftRank < rightRank ? -1 : 1;

            if (left.UsesDuration && right.UsesDuration
                                  && false == left.RemainingDurationSeconds.Equals(right.RemainingDurationSeconds))
                return left.RemainingDurationSeconds < right.RemainingDurationSeconds ? -1 : 1;

            if (left.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfNextMainPhase
                && right.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfNextMainPhase)
            {
                var leftDistance = GetMainPhaseExpirationDistance(currentPhase, left.ExpiresAfterMainPhase);
                var rightDistance = GetMainPhaseExpirationDistance(currentPhase, right.ExpiresAfterMainPhase);
                if (leftDistance != rightDistance)
                    return leftDistance < rightDistance ? -1 : 1;
            }

            if (left.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfRound
                && right.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfRound
                && left.ExpiresAfterRound != right.ExpiresAfterRound)
                return left.ExpiresAfterRound < right.ExpiresAfterRound ? -1 : 1;

            return 0;
        }

        private static int GetShieldExpirationRank(BuffRuntimeState buff)
        {
            if (buff.UsesDuration)
                return 0;

            if (buff.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfNextMainPhase)
                return 1;

            if (buff.Definition.LifetimeKind == BuffLifetimeKind.UntilEndOfRound)
                return 2;

            return 3;
        }

        private static int GetMainPhaseExpirationDistance(BattlePhaseKind currentPhase,
            BattlePhaseKind expiresAfterMainPhase)
        {
            if (currentPhase == expiresAfterMainPhase)
                return 0;

            if (currentPhase == BattlePhaseKind.Match && expiresAfterMainPhase == BattlePhaseKind.Hero)
                return 1;

            if (currentPhase == BattlePhaseKind.Hero && expiresAfterMainPhase == BattlePhaseKind.Match)
                return 1;

            return 2;
        }

        private static bool IsUnsupportedShieldLifetime(BuffLifetimeKind lifetimeKind)
        {
            return lifetimeKind is BuffLifetimeKind.NextAttack or BuffLifetimeKind.NextActivation;
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