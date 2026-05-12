using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    internal static class AbilityRuntimeDefinitionResolver
    {
        public static DirectActionDefinition CreateCommittedDirectAction(BattleSetup battleSetup,
            UnitDescriptor source, UnitActionType committedActionType, int committedActionValue)
        {
            var definition = battleSetup.TryGetUnit(source, out var unitSetup)
                ? unitSetup.ActiveAbility
                : default;
            var action = definition.DirectAction;
            var committedKind = UnitActionTypeMapping.ToDirectActionKind(committedActionType);

            return action.Kind == committedKind
                ? new DirectActionDefinition(action.Kind, committedActionValue, action.Targeting,
                    action.IgnoresAvatarGroupDefense)
                : action;
        }
    }
}