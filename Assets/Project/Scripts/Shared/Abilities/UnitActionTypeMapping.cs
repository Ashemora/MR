using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public static class UnitActionTypeMapping
    {
        public static UnitActionType FromDirectActionKind(DirectActionKind kind)
        {
            return kind == DirectActionKind.Heal ? UnitActionType.HealAlly : UnitActionType.DealDamage;
        }

        public static DirectActionKind ToDirectActionKind(UnitActionType actionType)
        {
            return actionType == UnitActionType.HealAlly ? DirectActionKind.Heal : DirectActionKind.Damage;
        }
    }
}