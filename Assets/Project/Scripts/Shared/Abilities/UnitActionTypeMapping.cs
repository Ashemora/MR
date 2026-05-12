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

                if (direct.Kind == DirectActionKind.Resurrect)
                    return UnitActionType.ResurrectAlly;
            }

            var buffEntries = ability.BuffEntries;
            for (var i = 0; i < buffEntries.Count; i++)
                if (buffEntries[i].IsConfigured)
                    return UnitActionType.SupportAlly;

            return UnitActionType.DealDamage;
        }

        public static UnitActionType FromDirectActionKind(DirectActionKind kind)
        {
            if (kind == DirectActionKind.Heal)
                return UnitActionType.HealAlly;

            if (kind == DirectActionKind.Resurrect)
                return UnitActionType.ResurrectAlly;

            return UnitActionType.DealDamage;
        }

        public static DirectActionKind ToDirectActionKind(UnitActionType actionType)
        {
            if (actionType == UnitActionType.HealAlly)
                return DirectActionKind.Heal;

            if (actionType == UnitActionType.ResurrectAlly)
                return DirectActionKind.Resurrect;

            return DirectActionKind.Damage;
        }
    }
}