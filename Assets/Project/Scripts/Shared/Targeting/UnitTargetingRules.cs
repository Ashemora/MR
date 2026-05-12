using System.Collections.Generic;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Targeting
{
    public static class UnitTargetingRules
    {
        public static List<UnitDescriptor> SelectTargets(UnitTargetingDefinition targeting, UnitDescriptor owner,
            IReadOnlyList<UnitTargetCandidate> candidates)
        {
            return SelectTargets(targeting, owner, default, false, candidates);
        }

        public static List<UnitDescriptor> SelectTargets(UnitTargetingDefinition targeting, UnitDescriptor owner,
            UnitDescriptor selectedTarget, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            return SelectTargets(targeting, owner, selectedTarget, true, candidates);
        }
        

        private static List<UnitDescriptor> SelectTargets(UnitTargetingDefinition targeting, UnitDescriptor owner,
            UnitDescriptor selectedTarget, bool hasSelectedTarget, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (targeting.Scope == UnitTargetScope.Self)
                return new List<UnitDescriptor> { owner };

            if (targeting.Scope == UnitTargetScope.SelectedTarget)
                return hasSelectedTarget && IsCandidateMatch(targeting, owner, selectedTarget, candidates)
                    ? new List<UnitDescriptor> { selectedTarget }
                    : new List<UnitDescriptor>();

            var result = new List<UnitDescriptor>();
            if (targeting.SelectionMode != UnitTargetSelectionMode.All || null == candidates)
                return result;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (IsCandidateMatch(targeting, owner, candidate))
                    result.Add(candidate.Descriptor);
            }

            return result;
        }

        private static bool IsCandidateMatch(UnitTargetingDefinition targeting, UnitDescriptor owner,
            UnitDescriptor selectedTarget, IReadOnlyList<UnitTargetCandidate> candidates)
        {
            if (null == candidates)
                return false;

            var selectedKey = BattleUnitKey.FromDescriptor(selectedTarget);
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (BattleUnitKey.FromDescriptor(candidate.Descriptor) == selectedKey)
                    return IsCandidateMatch(targeting, owner, candidate);
            }

            return false;
        }

        private static bool IsCandidateMatch(UnitTargetingDefinition targeting, UnitDescriptor owner,
            UnitTargetCandidate candidate)
        {
            if (false == candidate.IsAvailable)
                return false;

            if (false == targeting.IncludeOwner &&
                BattleUnitKey.FromDescriptor(candidate.Descriptor) == BattleUnitKey.FromDescriptor(owner))
                return false;

            if (false == IsRelationMatch(targeting.Relation, owner.Side, candidate.Descriptor.Side))
                return false;

            if (false == IsKindMatch(targeting.UnitKind, candidate.Descriptor.Kind))
                return false;

            return PassesFilters(targeting.Filters, candidate);
        }

        public static bool ContainsTarget(IReadOnlyList<UnitDescriptor> targets, UnitDescriptor target)
        {
            if (null == targets)
                return false;

            var targetKey = BattleUnitKey.FromDescriptor(target);
            for (var i = 0; i < targets.Count; i++)
                if (BattleUnitKey.FromDescriptor(targets[i]) == targetKey)
                    return true;

            return false;
        }

        public static bool MatchesTargeting(UnitTargetingDefinition targeting, UnitDescriptor owner,
            UnitTargetCandidate candidate)
        {
            if (false == candidate.IsAvailable)
                return false;

            if (targeting.Scope == UnitTargetScope.Self)
                return BattleUnitKey.FromDescriptor(candidate.Descriptor) == BattleUnitKey.FromDescriptor(owner);

            return IsCandidateMatch(targeting, owner, candidate);
        }

        private static bool IsRelationMatch(UnitTargetRelation relation, BattleSide ownerSide, BattleSide candidateSide)
        {
            if (relation == UnitTargetRelation.Everyone)
                return true;

            if (relation == UnitTargetRelation.Allies)
                return candidateSide == ownerSide;

            if (relation == UnitTargetRelation.Enemies)
                return candidateSide != ownerSide;

            return false;
        }

        private static bool IsKindMatch(UnitTargetKind targetKind, UnitKind candidateKind)
        {
            if (targetKind == UnitTargetKind.Units)
                return true;

            if (targetKind == UnitTargetKind.Heroes)
                return candidateKind == UnitKind.Hero;

            if (targetKind == UnitTargetKind.Avatar)
                return candidateKind == UnitKind.Avatar;

            return false;
        }

        private static bool PassesFilters(IReadOnlyList<UnitTargetFilter> filters, UnitTargetCandidate candidate)
        {
            if (null == filters || filters.Count == 0)
                return true;

            for (var i = 0; i < filters.Count; i++)
                if (filters[i] == UnitTargetFilter.CanDealDamage && candidate.Descriptor.ActionType != UnitActionType.DealDamage)
                    return false;

            return true;
        }
    }
}