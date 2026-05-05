using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Passives
{
    public static class ActivationConditionRules
    {
        public static bool Matches(ActivationConditionDefinition condition, ActivationConditionEvent e,
            BattleSide ownerSide, int ownerSlotIndex)
        {
            if (false == condition.IsConfigured || condition.Kind != e.Kind)
                return false;

            return condition.Subject switch
            {
                ActivationConditionSubject.Owner => IsOwner(e.Source, ownerSide, ownerSlotIndex),
                ActivationConditionSubject.OwnerSide => e.Side == ownerSide,
                _ => false
            };
        }

        private static bool IsOwner(UnitDescriptor source, BattleSide ownerSide, int ownerSlotIndex)
        {
            return source.Kind == UnitKind.Hero
                   && source.Side == ownerSide
                   && source.SlotIndex == ownerSlotIndex;
        }
    }
}