using System.Collections.Generic;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public static class AbilityTargetRules
    {
        public static bool IsTargetValidForEffect(UnitDescriptor source, UnitDescriptor target,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries,
            IReadOnlyList<UnitTargetCandidate> candidates,
            bool isSourceAlive, bool isTargetAlive, bool isTargetHpFull, bool isTargetExposed)
        {
            if (false == isSourceAlive || false == isTargetAlive || null == candidates)
                return false;

            var anyEffectTouches = false;

            if (directAction.IsConfigured && EffectTouchesTarget(directAction.Targeting, source, target, candidates))
            {
                anyEffectTouches = true;
                if (false == IsDirectActionRecipientValid(directAction, isTargetHpFull, isTargetExposed))
                    return false;
            }

            if (null != buffEntries)
            {
                for (var i = 0; i < buffEntries.Count; i++)
                {
                    var entry = buffEntries[i];
                    if (false == entry.IsConfigured)
                        continue;

                    if (false == EffectTouchesTarget(entry.Targeting, source, target, candidates))
                        continue;

                    anyEffectTouches = true;
                }
            }

            return anyEffectTouches;
        }

        public static bool IsActionRecipientValid(DirectActionKind actionKind, bool isTargetAlive,
            bool isTargetHpFull, bool isTargetExposed)
        {
            if (false == isTargetAlive)
                return false;

            if (actionKind == DirectActionKind.Damage)
                return isTargetExposed;

            if (actionKind == DirectActionKind.Heal)
                return false == isTargetHpFull;

            return false;
        }

        private static bool EffectTouchesTarget(UnitTargetingDefinition targeting, UnitDescriptor source,
            UnitDescriptor target, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            var targets = UnitTargetingRules.SelectTargets(targeting, source, target, candidates);
            return UnitTargetingRules.ContainsTarget(targets, target);
        }

        private static bool IsDirectActionRecipientValid(DirectActionDefinition action, bool isTargetHpFull,
            bool isTargetExposed)
        {
            if (action.Kind == DirectActionKind.Damage)
                return isTargetExposed || action.IgnoresAvatarGroupDefense;

            if (action.Kind == DirectActionKind.Heal)
                return false == isTargetHpFull;

            return false;
        }
    }
}