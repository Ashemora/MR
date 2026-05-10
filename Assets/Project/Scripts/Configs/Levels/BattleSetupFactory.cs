using System.Collections.Generic;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Configs.Levels
{
    public static class BattleSetupFactory
    {
        public static BattleSetup Create(LevelConfig levelConfig, SlotLayoutConfig slotLayoutConfig)
        {
            return new BattleSetup(
                CreateAvatar(BattleSide.Player, levelConfig.PlayerAvatarConfig),
                CreateAvatar(BattleSide.Enemy, levelConfig.EnemyAvatarConfig),
                CreateHeroes(BattleSide.Player, levelConfig.PlayerHeroes, slotLayoutConfig),
                CreateHeroes(BattleSide.Enemy, levelConfig.EnemyHeroes, slotLayoutConfig));
        }
        

        private static BattleUnitSetup CreateAvatar(BattleSide side, AvatarConfig config)
        {
            if (!config)
                return new BattleUnitSetup(UnitDescriptor.Avatar(side, HeroActionType.DealDamage), false,
                    0, 0, 0, 0f, default, TileKind.None);

            return new BattleUnitSetup(UnitDescriptor.Avatar(side, config.AbilityType), true,
                config.MaxHP, config.AbilityPower, config.ActivationEnergyCost,
                config.ActivationCooldownSeconds, config.ToActiveAbilityDefinition(), TileKind.None);
        }

        private static BattleUnitSetup[] CreateHeroes(BattleSide side, HeroConfig[] heroes,
            SlotLayoutConfig slotLayoutConfig)
        {
            var result = new BattleUnitSetup[BattleSetup.HeroSlotCount];
            for (var slotIndex = 0; slotIndex < result.Length; slotIndex++)
                result[slotIndex] = CreateHero(side, slotIndex, GetHeroConfig(heroes, slotIndex),
                    GetSlotKind(slotLayoutConfig, slotIndex));

            return result;
        }

        private static BattleUnitSetup CreateHero(BattleSide side, int slotIndex, HeroConfig config, TileKind slotKind)
        {
            if (!config)
                return new BattleUnitSetup(UnitDescriptor.Hero(side, slotIndex, HeroActionType.DealDamage), false,
                    0, 0, 0, 0f, default, slotKind);

            return new BattleUnitSetup(UnitDescriptor.Hero(side, slotIndex, config.AbilityType), true,
                config.MaxHP, config.AbilityPower, config.ActivationEnergyCost,
                config.ActivationCooldownSeconds, config.ToActiveAbilityDefinition(), slotKind,
                CreatePassiveDefinitions(config.PassiveAbilities));
        }

        private static IReadOnlyList<PassiveAbilityDefinition> CreatePassiveDefinitions(
            IReadOnlyList<HeroPassiveConfig> passiveConfigs)
        {
            if (null == passiveConfigs || passiveConfigs.Count == 0)
                return null;

            var result = new List<PassiveAbilityDefinition>(passiveConfigs.Count);
            for (var i = 0; i < passiveConfigs.Count; i++)
            {
                var passiveConfig = passiveConfigs[i];
                if (!passiveConfig)
                    continue;

                var definition = passiveConfig.ToDefinition();
                if (definition.IsConfigured)
                    result.Add(definition);
            }

            return result;
        }

        private static HeroConfig GetHeroConfig(HeroConfig[] heroes, int slotIndex)
        {
            return null != heroes && slotIndex >= 0 && slotIndex < heroes.Length ? heroes[slotIndex] : null;
        }

        private static TileKind GetSlotKind(SlotLayoutConfig slotLayoutConfig, int slotIndex)
        {
            var slotKinds = slotLayoutConfig ? slotLayoutConfig.HeroSlotKinds : null;
            return null != slotKinds && slotIndex >= 0 && slotIndex < slotKinds.Length
                ? slotKinds[slotIndex]
                : TileKind.None;
        }
    }
}