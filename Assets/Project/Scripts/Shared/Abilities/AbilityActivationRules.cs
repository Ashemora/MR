using System.Collections.Generic;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public static class AbilityActivationRules
    {
        public static bool WouldProduceAnyEffect(UnitDescriptor source, DirectActionDefinition directAction,
            IReadOnlyList<BuffEntryDefinition> buffEntries, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (null == candidates)
                return false;

            if (directAction.IsConfigured && HasValidDirectTarget(source, directAction, candidates))
                return true;

            if (null == buffEntries)
                return false;

            for (var i = 0; i < buffEntries.Count; i++)
            {
                var entry = buffEntries[i];
                if (false == entry.IsConfigured)
                    continue;

                if (HasValidBuffTarget(source, entry, candidates))
                    return true;
            }

            return false;
        }

        private static bool HasValidDirectTarget(UnitDescriptor source, DirectActionDefinition action,
            IReadOnlyList<UnitTargetCandidate> candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (false == UnitTargetingRules.MatchesTargeting(action.Targeting, source, candidate))
                {
                    if (action.Kind != DirectActionKind.Resurrect ||
                        false == UnitTargetingRules.MatchesTargetingIncludingUnavailable(action.Targeting, source,
                            candidate))
                        continue;
                }

                if (action.Kind == DirectActionKind.Damage && false == IsAlive(candidate))
                    continue;

                if (action.Kind == DirectActionKind.Heal && IsHpFull(candidate))
                    continue;

                if (action.Kind == DirectActionKind.Heal && false == IsAlive(candidate))
                    continue;

                if (action.Kind == DirectActionKind.Resurrect && false == IsValidResurrectionTarget(candidate))
                    continue;

                return true;
            }

            return false;
        }

        private static bool HasValidBuffTarget(UnitDescriptor source, BuffEntryDefinition entry,
            IReadOnlyList<UnitTargetCandidate> candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (UnitTargetingRules.MatchesTargeting(entry.Targeting, source, candidate))
                    return true;
            }

            return false;
        }

        private static bool IsHpFull(UnitTargetCandidate candidate)
        {
            return candidate.MaxHP <= 0 || candidate.CurrentHP >= candidate.MaxHP;
        }

        private static bool IsAlive(UnitTargetCandidate candidate)
        {
            return candidate.MaxHP <= 0 || candidate.CurrentHP > 0;
        }

        private static bool IsValidResurrectionTarget(UnitTargetCandidate candidate)
        {
            return candidate.IsAssigned && candidate.Descriptor.Kind == UnitKind.Hero && candidate.MaxHP > 0
                   && candidate.CurrentHP <= 0;
        }
    }
}