using System;
using System.Collections.Generic;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    internal static class AbilityRuntimeDefinitionResolver
    {
        private const int SlotCount = 4;


        public static IReadOnlyList<AbilityEffectEntryDefinition> CreateCommittedEntries(LevelConfig levelConfig,
            UnitDescriptor source, HeroActionType committedActionType, int committedActionValue)
        {
            var definition = GetActiveAbilityDefinition(levelConfig, source);
            var entries = definition.EffectEntries;
            if (entries.Count == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new AbilityEffectEntryDefinition[entries.Count];
            for (var i = 0; i < entries.Count; i++)
                result[i] = CreateCommittedEntry(entries[i], committedActionType, committedActionValue);

            return result;
        }

        private static ActiveAbilityDefinition GetActiveAbilityDefinition(LevelConfig levelConfig, UnitDescriptor source)
        {
            if (source.Kind == UnitKind.Avatar)
            {
                var avatar = source.Side == BattleSide.Player
                    ? levelConfig.PlayerAvatarConfig
                    : levelConfig.EnemyAvatarConfig;

                return avatar ? avatar.ToActiveAbilityDefinition() : default;
            }

            var hero = GetHeroConfig(levelConfig, source.Side, source.SlotIndex);
            
            return hero ? hero.ToActiveAbilityDefinition() : default;
        }

        private static HeroConfig GetHeroConfig(LevelConfig levelConfig, BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return null;

            var heroes = side == BattleSide.Player
                ? levelConfig.PlayerHeroes
                : levelConfig.EnemyHeroes;

            return heroes != null && slotIndex < heroes.Length ? heroes[slotIndex] : null;
        }

        private static AbilityEffectEntryDefinition CreateCommittedEntry(AbilityEffectEntryDefinition entry,
            HeroActionType committedActionType, int committedActionValue)
        {
            return new AbilityEffectEntryDefinition(entry.Targeting,
                CreateCommittedDirectActions(entry.DirectActions, committedActionType, committedActionValue),
                entry.BuffApplications);
        }

        private static DirectActionDefinition[] CreateCommittedDirectActions(IReadOnlyList<DirectActionDefinition> directActions, 
            HeroActionType committedActionType, int committedActionValue)
        {
            if (directActions == null || directActions.Count == 0)
                return Array.Empty<DirectActionDefinition>();

            var committedKind = ToDirectActionKind(committedActionType);
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

        private static DirectActionKind ToDirectActionKind(HeroActionType actionType)
        {
            return actionType == HeroActionType.HealAlly ? DirectActionKind.Heal : DirectActionKind.Damage;
        }
    }
}