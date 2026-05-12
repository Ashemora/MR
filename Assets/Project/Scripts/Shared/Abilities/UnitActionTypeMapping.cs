using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public static class UnitActionTypeMapping
    {
        public static UnitActionType FromActiveAbility(ActiveAbilityDefinition ability)
        {
            var direct = ability.DirectAction;
            if (direct.IsConfigured)
            {
                if (direct.Kind == DirectActionKind.Damage)
                    return UnitActionType.DealDamage;

                if (direct.Kind == DirectActionKind.Heal)
                    return UnitActionType.HealAlly;
            }

            var buffEntries = ability.BuffEntries;
            for (var i = 0; i < buffEntries.Count; i++)
                if (buffEntries[i].IsConfigured)
                    return UnitActionType.SupportAlly;

            return UnitActionType.DealDamage;
        }

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