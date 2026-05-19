using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Gameplay.Battle;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.Results;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Gameplay.UI.MoveBar;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Bot;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Layout;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Timer;
using Project.Scripts.Services.Clock;
using Project.Scripts.Shared.BattleSetup;
using VContainer;
using VContainer.Unity;
#if DEV
using Project.Scripts.Dev;
#endif

namespace Project.Scripts.DI
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            const bool EnableEscalationModule = false;
            const bool EnableBurndownModule = true;


            var slotLayoutConfig = Parent.Container.Resolve<SlotLayoutConfig>();
            var playerBattleConfig = Parent.Container.Resolve<PlayerBattleConfig>();
            var session = Parent.Container.Resolve<IBattleSessionProvider>().Current;

            BattleSetup battleSetup;
            BotStrengthConfig effectiveBotStrengthConfig;
            BotStrategyConfig effectiveBotStrategyConfig;
            MatchVisualsProvider matchVisuals;
#if DEV
            var devOverride = Parent.Container.Resolve<IDevMatchOverrideService>();
            var devCatalog = Parent.Container.Resolve<DevUnitCatalogConfig>();
            var debugConfig = Parent.Container.Resolve<Project.Scripts.Configs.DebugConfig>();

            var playerSeed = session?.PlayerSeed ?? 0;
            var opponentSeed = session?.OpponentSeed ?? 0;

            var playerAvatar = ResolveSideAvatar(devOverride, devOverride.PlayerMode,
                devOverride.PlayerDeckIndex, playerSeed, playerBattleConfig.DefaultUnitDeck, out var playerHeroes);
            var opponentAvatar = ResolveSideAvatar(devOverride, devOverride.OpponentMode,
                devOverride.OpponentDeckIndex, opponentSeed, null, out var opponentHeroes);

            battleSetup = BattleSetupFactory.Create(playerAvatar, playerHeroes,
                opponentAvatar, opponentHeroes, slotLayoutConfig);
            matchVisuals = new MatchVisualsProvider(playerAvatar, playerHeroes, opponentAvatar, opponentHeroes);

            effectiveBotStrengthConfig = ResolveBotStrength(devOverride, opponentSeed);
            effectiveBotStrategyConfig = ResolveBotStrategy(devOverride, opponentSeed);

            if (debugConfig.LogDevOpponentOptions)
                UnityEngine.Debug.Log($"[DevMatch] Battle built player({devOverride.PlayerMode}) " +
                                      $"opponent({devOverride.OpponentMode}) seed=({playerSeed},{opponentSeed}) " +
                                      $"strength={devOverride.StrengthMode}/{GetConfigName(effectiveBotStrengthConfig)} " +
                                      $"strategy={devOverride.StrategyMode}/{GetConfigName(effectiveBotStrategyConfig)}");
#else
            throw new System.NotSupportedException(
                "Non-DEV match assembly is not implemented yet - the server-driven path will fill this in.");
#endif

            builder.RegisterInstance(battleSetup);
            builder.RegisterInstance(matchVisuals);
            builder.RegisterInstance(new EffectiveBotConfigProvider(effectiveBotStrengthConfig,
                effectiveBotStrategyConfig));

            builder.RegisterComponentInHierarchy<GameplayEntryPoint>();
            builder.Register<BoardSystemsFactory>(Lifetime.Singleton);

            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BattleClockService>().As<IBattleClock>();
            builder.Register<IBattleActionRuntimeService, BattleActionRuntimeService>(Lifetime.Singleton);
            builder.Register<IMoveCounterService, MoveCounterService>(Lifetime.Singleton);
            builder.Register<IAvatarGroupDefenseService, AvatarGroupDefenseService>(Lifetime.Singleton);
            builder.Register<IAvatarService, AvatarService>(Lifetime.Singleton);
            builder.Register<IUnitStateService, UnitStateService>(Lifetime.Singleton);
            builder.Register<IMoveBarService, MoveBarService>(Lifetime.Singleton);
            builder.Register<IHeroService, HeroService>(Lifetime.Singleton);
            builder.Register<BuffService>(Lifetime.Singleton)
                .As<IBuffService>()
                .As<IEnergyGainModifierService>()
                .As<IHeroAbilityModifierService>()
                .As<IAbilityPowerModifierService>()
                .As<INextAttackBuffService>()
                .As<IBombRadiusModifierService>()
                .As<ILineRuneModifierService>()
                .As<IHeroCooldownModifierService>()
                .As<INextActivationBuffService>()
                .As<IAbilityRepeatModifierService>()
                .As<IAbilityAdditionalTargetModifierService>()
                .As<IResurrectOnDeathBuffService>()
                .As<IShieldService>()
                .As<IStunStatusService>();
            builder.RegisterEntryPoint<HeroPassiveService>().As<IHeroPassiveService>();
            builder.RegisterEntryPoint<BattleSideEnergyService>().As<IBattleSideEnergyService>();
            builder.Register<IUnitActivationCooldownService, UnitActivationCooldownService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<AutoEnergyTickService>();

            if (EnableEscalationModule)
            {
                builder.Register<EscalationModifierService>(Lifetime.Singleton)
                    .As<IEscalationModifierService>()
                    .As<IBattleEconomyModifierService>();
            }
            else
            {
                builder.Register<DefaultBattleEconomyModifierService>(Lifetime.Singleton)
                    .As<IEscalationModifierService>()
                    .As<IBattleEconomyModifierService>();
            }
            builder.Register<IAbilityEffectApplicationService, AbilityEffectApplicationService>(Lifetime.Singleton);
            builder.Register<IUnitAbilityActivationService, UnitAbilityActivationService>(Lifetime.Singleton);
            builder.Register<IAbilityExecutionService, AbilityExecutionService>(Lifetime.Singleton);
            builder.Register<IBotActionCandidateBuilder, BotActionCandidateBuilder>(Lifetime.Singleton);

            builder.Register<MoveBarViewModel>(Lifetime.Singleton);
            builder.Register<BattleFieldViewModel>(Lifetime.Singleton);
            builder.Register<GameResultPresenter>(Lifetime.Singleton);
            builder.Register<GameResultSequenceController>(Lifetime.Singleton);
            builder.Register<IReadyPulseCoordinator, ReadyPulseCoordinator>(Lifetime.Singleton);
#if DEV
            builder.Register<DevMatchPhaseSkipService>(Lifetime.Singleton);
            builder.Register<DevMatchPhaseSkipButtonSpawner>(Lifetime.Singleton);
            builder.Register<DevAbortBattleService>(Lifetime.Singleton);
            builder.Register<DevReturnToLobbyButtonSpawner>(Lifetime.Singleton);
#endif

            builder.Register<IBoardRuntimeService, BoardRuntimeService>(Lifetime.Singleton);
            builder.Register<IBoardBoundsProvider, BoardBoundsProvider>(Lifetime.Singleton);
            builder.Register<IGameplayScreenLayoutService, GameplayScreenLayoutService>(Lifetime.Singleton);
            builder.Register<IBattleFlowService, BattleFlowService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<Project.Scripts.Services.BattleFlow.BattleFlowPhaseCoordinator>();

            if (EnableBurndownModule)
            {
                builder.Register<IBurndownService, BurndownService>(Lifetime.Singleton);
                builder.Register<IBurndownTransitionCoordinator, BurndownTransitionCoordinator>(Lifetime.Singleton);
            }
            else
            {
                builder.Register<IBurndownService, DefaultBurndownService>(Lifetime.Singleton);
                builder.Register<IBurndownTransitionCoordinator, DefaultBurndownTransitionCoordinator>(Lifetime.Singleton);
            }

            builder.RegisterEntryPoint<BoardAnnouncementService>().As<IBoardAnnouncementService>();
            builder.RegisterEntryPoint<BattlePrePhaseAnnouncementService>();
            builder.RegisterEntryPoint<BattleTimerAnnouncementService>();

            if (EnableBurndownModule)
            {
                builder.RegisterEntryPoint<BurndownAnnouncementService>();
            }

            if (EnableEscalationModule)
            {
                builder.RegisterEntryPoint<BattleEscalationController>();
                builder.RegisterEntryPoint<BattleEscalationAnnouncementService>();
            }

            if (effectiveBotStrengthConfig)
            {
                builder.RegisterInstance(effectiveBotStrengthConfig);
                builder.RegisterEntryPoint<BotOpponentService>().As<IBotOpponentService>();
            }
        }

#if DEV
        private static AvatarConfig ResolveSideAvatar(IDevMatchOverrideService devOverride, DevSideMode mode,
            int deckIndex, int seed, UnitDeckConfig fallbackDeck, out HeroConfig[] heroes)
        {
            if (mode == DevSideMode.PickDeck && devOverride.TryGetPickedDeck(deckIndex, out var pickedDeck) && pickedDeck)
            {
                heroes = pickedDeck.Heroes;
                return pickedDeck.AvatarConfig;
            }

            if (mode == DevSideMode.Random && devOverride.TryBuildRandomDeck(seed, out var randomSelection))
            {
                heroes = randomSelection.Heroes;
                return randomSelection.Avatar;
            }

            heroes = fallbackDeck ? fallbackDeck.Heroes : null;
            return fallbackDeck ? fallbackDeck.AvatarConfig : null;
        }

        private static BotStrengthConfig ResolveBotStrength(IDevMatchOverrideService devOverride, int opponentSeed)
        {
            if (devOverride.StrengthMode == DevBotSelectionMode.Random)
                return devOverride.TryPickRandomStrength(DeriveSeed(opponentSeed, 0x51A7), out var randomStrength)
                    ? randomStrength
                    : null;

            return devOverride.TryGetPickedStrength(devOverride.StrengthIndex, out var pickedStrength)
                ? pickedStrength
                : null;
        }

        private static BotStrategyConfig ResolveBotStrategy(IDevMatchOverrideService devOverride, int opponentSeed)
        {
            if (devOverride.StrategyMode == DevBotSelectionMode.Random)
                return devOverride.TryPickRandomStrategy(DeriveSeed(opponentSeed, 0x7A39), out var randomStrategy)
                    ? randomStrategy
                    : null;

            return devOverride.TryGetPickedStrategy(devOverride.StrategyIndex, out var pickedStrategy)
                ? pickedStrategy
                : null;
        }

        private static int DeriveSeed(int seed, int salt)
        {
            return unchecked(seed ^ salt);
        }

        private static string GetConfigName(UnityEngine.Object config)
        {
            return config ? config.name : "none";
        }
#endif
    }
}