using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;

namespace Project.Scripts.Shared.Rules
{
    public static class AbilityTargetRules
    {
        public static bool IsTargetAllowedByDirectEntries(UnitDescriptor source, UnitDescriptor target,
            IReadOnlyList<AbilityEffectEntryDefinition> entries, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (entries == null || candidates == null)
                return false;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (false == HasConfiguredDirectActions(entry.DirectActions))
                    continue;

                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, target, candidates);
                if (UnitTargetingRules.ContainsTarget(targets, target))
                    return true;
            }

            return false;
        }

        public static bool IsTargetValid(HeroActionType actionType, bool isSourceAlive, bool isTargetAlive,
            bool isTargetHpFull, bool isTargetExposed)
        {
            if (false == isSourceAlive || false == isTargetAlive)
                return false;

            if (actionType == HeroActionType.DealDamage)
                return CanDealDamage(isTargetExposed);

            if (actionType == HeroActionType.HealAlly)
                return CanHeal(isTargetHpFull);

            return false;
        }

        private static bool HasConfiguredDirectActions(IReadOnlyList<DirectActionDefinition> directActions)
        {
            if (directActions == null)
                return false;

            for (var i = 0; i < directActions.Count; i++)
                if (directActions[i].IsConfigured)
                    return true;

            return false;
        }

        private static bool CanDealDamage(bool isTargetExposed)
        {
            return isTargetExposed;
        }

        private static bool CanHeal(bool isTargetHpFull)
        {
            return false == isTargetHpFull;
        }
    }
}