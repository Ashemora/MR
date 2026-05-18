using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Battle.Bot;
using Project.Scripts.Configs.Battle.Layout;
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
using Project.Scripts.Services.Progression;
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
            

            var levelDatabase = Parent.Container.Resolve<LevelDatabase>();
            var session = Parent.Container.Resolve<IBattleSessionProvider>().Current;
            var levelId = session?.LevelId ?? Parent.Container.Resolve<ILevelProgressionService>().CurrentLevelId;
            var levelConfig = levelDatabase.GetById(levelId);
            builder.RegisterInstance(levelConfig);
#if DEV
            var devOverride = Parent.Container.Resolve<IDevOpponentOverrideService>();
#endif
            var slotLayoutConfig = Parent.Container.Resolve<SlotLayoutConfig>();
            var playerBattleConfig = Parent.Container.Resolve<PlayerBattleConfig>();

            BattleSetup battleSetup;
            BotConfig effectiveBotConfig = levelConfig.BotConfig;
#if DEV
            if (session != null && devOverride.TryBuildOpponent(session.Seed, out var devSelection))
            {
                var playerDeck = playerBattleConfig.DefaultUnitDeck;
                battleSetup = BattleSetupFactory.Create(
                    playerDeck ? playerDeck.AvatarConfig : null,
                    playerDeck ? playerDeck.Heroes : null,
                    devSelection.Avatar,
                    devSelection.Heroes,
                    slotLayoutConfig);
                effectiveBotConfig = devSelection.BotConfig;
                if (Parent.Container.Resolve<Project.Scripts.Configs.DebugConfig>().LogDevOpponentOptions)
                    UnityEngine.Debug.Log($"[DevOpponent] Random opponent applied seed={session.Seed} " +
                                          $"strength={devOverride.GetStrengthDisplayName(devOverride.StrengthIndex)}");
            }
            else
#endif
            {
#if DEV
                if (devOverride.Mode == DevOpponentMode.Random
                    && Parent.Container.Resolve<Project.Scripts.Configs.DebugConfig>().LogDevOpponentOptions)
                    UnityEngine.Debug.LogWarning($"[DevOpponent] Random opponent fallback to LevelConfig: " +
                                                 $"{devOverride.GetBuildBlockReason()}");
#endif
                battleSetup = BattleSetupFactory.Create(playerBattleConfig.DefaultUnitDeck,
                    levelConfig.OpponentUnitDeck, slotLayoutConfig);
            }

            builder.RegisterInstance(battleSetup);
            builder.RegisterInstance(new EffectiveBotConfigProvider(effectiveBotConfig));

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

            if (effectiveBotConfig)
            {
                builder.RegisterInstance(effectiveBotConfig);
                builder.RegisterEntryPoint<BotOpponentService>().As<IBotOpponentService>();
            }
        }
    }
}