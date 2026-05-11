using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    internal static class AbilityRuntimeDefinitionResolver
    {
        public static IReadOnlyList<AbilityEffectEntryDefinition> CreateCommittedEntries(BattleSetup battleSetup,
            UnitDescriptor source, UnitActionType committedActionType, int committedActionValue)
        {
            var definition = battleSetup.TryGetUnit(source, out var unitSetup)
                ? unitSetup.ActiveAbility
                : default;
            var entries = definition.EffectEntries;
            if (entries.Count == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new AbilityEffectEntryDefinition[entries.Count];
            for (var i = 0; i < entries.Count; i++)
                result[i] = CreateCommittedEntry(entries[i], committedActionType, committedActionValue);

            return result;
        }

        private static AbilityEffectEntryDefinition CreateCommittedEntry(AbilityEffectEntryDefinition entry,
            UnitActionType committedActionType, int committedActionValue)
        {
            return new AbilityEffectEntryDefinition(entry.Targeting,
                CreateCommittedDirectActions(entry.DirectActions, committedActionType, committedActionValue),
                entry.BuffApplications, entry.IgnoresAvatarGroupDefense);
        }

        private static DirectActionDefinition[] CreateCommittedDirectActions(IReadOnlyList<DirectActionDefinition> directActions, 
            UnitActionType committedActionType, int committedActionValue)
        {
            if (null == directActions || directActions.Count == 0)
                return Array.Empty<DirectActionDefinition>();

            var committedKind = UnitActionTypeMapping.ToDirectActionKind(committedActionType);
            var result = new DirectActionDefinition[directActions.Count];
            for (var i = 0; i < directActions.Count; i++)
            {
                var action = directActions[i];
                result[i] = action.Kind == committedKind
                    ? new DirectActionDefinition(action.Kind, committedActionValue)
                    : action;
            }

            return result;
        }
    }
}