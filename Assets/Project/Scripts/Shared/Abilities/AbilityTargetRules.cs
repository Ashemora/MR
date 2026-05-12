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
            if (false == isSourceAlive || null == candidates)
                return false;

            var anyEffectTouches = false;

            if (directAction.IsConfigured && DirectActionTouchesTarget(directAction, source, target, candidates))
            {
                anyEffectTouches = true;
                if (false == IsDirectActionRecipientValid(directAction, target, isTargetAlive, isTargetHpFull,
                        isTargetExposed))
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
            if (actionKind == DirectActionKind.Damage)
                return isTargetAlive && isTargetExposed;

            if (actionKind == DirectActionKind.Heal)
                return isTargetAlive && false == isTargetHpFull;

            if (actionKind == DirectActionKind.Resurrect)
                return false == isTargetAlive;

            return false;
        }

        private static bool EffectTouchesTarget(UnitTargetingDefinition targeting, UnitDescriptor source,
            UnitDescriptor target, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            var targets = UnitTargetingRules.SelectTargets(targeting, source, target, candidates);
            return UnitTargetingRules.ContainsTarget(targets, target);
        }

        private static bool DirectActionTouchesTarget(DirectActionDefinition action, UnitDescriptor source,
            UnitDescriptor target, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (action.Kind != DirectActionKind.Resurrect)
                return EffectTouchesTarget(action.Targeting, source, target, candidates);

            var targets = UnitTargetingRules.SelectTargetsIncludingUnavailable(action.Targeting, source, target,
                candidates);
            return UnitTargetingRules.ContainsTarget(targets, target);
        }

        private static bool IsDirectActionRecipientValid(DirectActionDefinition action, UnitDescriptor target,
            bool isTargetAlive, bool isTargetHpFull, bool isTargetExposed)
        {
            if (action.Kind == DirectActionKind.Damage)
                return isTargetAlive && (isTargetExposed || action.IgnoresAvatarGroupDefense);

            if (action.Kind == DirectActionKind.Heal)
                return isTargetAlive && false == isTargetHpFull;

            if (action.Kind == DirectActionKind.Resurrect)
                return target.Kind == UnitKind.Hero && false == isTargetAlive;

            return false;
        }
    }
}