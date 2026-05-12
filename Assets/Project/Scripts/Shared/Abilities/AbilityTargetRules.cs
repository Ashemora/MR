using System.Collections.Generic;
using Project.Scripts.Shared.Targeting;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public static class AbilityTargetRules
    {
        public static bool IsTargetAllowedByEffect(UnitDescriptor source, UnitDescriptor target,
            DirectActionDefinition directAction, IReadOnlyList<BuffEntryDefinition> buffEntries,
            IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (null == candidates)
                return false;

            if (directAction.IsConfigured)
            {
                var directTargets = UnitTargetingRules.SelectTargets(directAction.Targeting, source, target, candidates);
                if (UnitTargetingRules.ContainsTarget(directTargets, target))
                    return true;
            }

            if (null == buffEntries)
                return false;

            for (var i = 0; i < buffEntries.Count; i++)
            {
                var entry = buffEntries[i];
                if (false == entry.IsConfigured)
                    continue;

                var targets = UnitTargetingRules.SelectTargets(entry.Targeting, source, target, candidates);
                if (UnitTargetingRules.ContainsTarget(targets, target))
                    return true;
            }

            return false;
        }

        public static bool IsTargetValid(UnitActionType actionType, bool isSourceAlive, bool isTargetAlive,
            bool isTargetHpFull, bool isTargetExposed)
        {
            if (false == isSourceAlive || false == isTargetAlive)
                return false;

            if (actionType == UnitActionType.DealDamage)
                return CanDealDamage(isTargetExposed);

            if (actionType == UnitActionType.HealAlly)
                return CanHeal(isTargetHpFull);

            if (actionType == UnitActionType.SupportAlly)
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