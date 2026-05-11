using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Passives;

namespace Project.Scripts.Shared.Rules
{
    public static class AbilityAdditionalTargetRules
    {
        public static List<UnitDescriptor> SelectTargets(UnitDescriptor source, UnitDescriptor primaryTarget,
            HeroActionType actionType, int maxTargets, IReadOnlyList<AbilityTargetCandidate> candidates,
            IReadOnlyList<AbilityEffectEntryDefinition> entries)
        {
            var result = new List<UnitDescriptor>();
            if (maxTargets <= 0 || null == candidates)
                return result;

            var rankedCandidates = new List<AbilityTargetCandidate>();
            var unitCandidates = ToUnitTargetCandidates(candidates);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (IsSameUnit(candidate.Descriptor, primaryTarget))
                    continue;

                if (false == AbilityTargetRules.IsTargetValid(actionType, true, candidate.IsAlive,
                        candidate.IsHpFull, candidate.IsExposed))
                    continue;

                if (false == AbilityTargetRules.IsTargetAllowedByDirectEntries(source, candidate.Descriptor, entries,
                        unitCandidates))
                    continue;

                rankedCandidates.Add(candidate);
            }

            rankedCandidates.Sort((left, right) => Compare(actionType, left, right));

            for (var i = 0; i < rankedCandidates.Count && result.Count < maxTargets; i++)
                result.Add(rankedCandidates[i].Descriptor);

            return result;
        }


        private static int Compare(HeroActionType actionType, AbilityTargetCandidate left, AbilityTargetCandidate right)
        {
            if (actionType == HeroActionType.HealAlly)
                return CompareHealTarget(left, right);

            return CompareDamageTarget(left, right);
        }

        private static int CompareDamageTarget(AbilityTargetCandidate left, AbilityTargetCandidate right)
        {
            var ratioComparison = GetHealthRatio(left).CompareTo(GetHealthRatio(right));
            if (ratioComparison != 0)
                return ratioComparison;

            var hpComparison = left.CurrentHP.CompareTo(right.CurrentHP);

            return hpComparison != 0 ? hpComparison : CompareStableUnitOrder(left.Descriptor, right.Descriptor);
        }

        private static int CompareHealTarget(AbilityTargetCandidate left, AbilityTargetCandidate right)
        {
            var missingComparison = GetMissingHP(right).CompareTo(GetMissingHP(left));
            if (missingComparison != 0)
                return missingComparison;

            var missingRatioComparison = GetMissingHealthRatio(right).CompareTo(GetMissingHealthRatio(left));
            if (missingRatioComparison != 0)
                return missingRatioComparison;

            var hpComparison = left.CurrentHP.CompareTo(right.CurrentHP);

            return hpComparison != 0 ? hpComparison : CompareStableUnitOrder(left.Descriptor, right.Descriptor);
        }

        private static float GetHealthRatio(AbilityTargetCandidate candidate)
        {
            return candidate.MaxHP <= 0 ? 1f : (float)candidate.CurrentHP / candidate.MaxHP;
        }

        private static int GetMissingHP(AbilityTargetCandidate candidate)
        {
            return candidate.MaxHP <= candidate.CurrentHP ? 0 : candidate.MaxHP - candidate.CurrentHP;
        }

        private static float GetMissingHealthRatio(AbilityTargetCandidate candidate)
        {
            return candidate.MaxHP <= 0 ? 0f : (float)GetMissingHP(candidate) / candidate.MaxHP;
        }

        private static int CompareStableUnitOrder(UnitDescriptor left, UnitDescriptor right)
        {
            var sideComparison = left.Side.CompareTo(right.Side);
            if (sideComparison != 0)
                return sideComparison;

            var kindComparison = GetKindOrder(left.Kind).CompareTo(GetKindOrder(right.Kind));
            if (kindComparison != 0)
                return kindComparison;

            return left.SlotIndex.CompareTo(right.SlotIndex);
        }

        private static int GetKindOrder(UnitKind kind)
        {
            return kind == UnitKind.Hero ? 0 : 1;
        }

        private static bool IsSameUnit(UnitDescriptor left, UnitDescriptor right)
        {
            return left.Side == right.Side
                   && left.Kind == right.Kind
                   && left.SlotIndex == right.SlotIndex;
        }

        private static List<UnitTargetCandidate> ToUnitTargetCandidates(IReadOnlyList<AbilityTargetCandidate> candidates)
        {
            var result = new List<UnitTargetCandidate>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                result.Add(new UnitTargetCandidate(candidate.Descriptor, candidate.CurrentHP, candidate.MaxHP,
                    candidate.IsAlive));
            }

            return result;
        }
    }
}
