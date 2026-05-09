using Project.Scripts.Configs.Levels;
using Project.Scripts.Gameplay;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.Results;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Bot;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Layout;
using Project.Scripts.Services.Progression;
using Project.Scripts.Services.Combat;
using Project.Scripts.Services.Timer;
using Project.Scripts.Services.Clock;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.DI
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            const bool EnableEscalationModule = false;
            const bool EnableBurndownModule = true;
            

            var levelDatabase = Parent.Container.Resolve<LevelDatabase>();
            var levelConfig = levelDatabase.GetById(LevelProgressionService.CurrentLevelId);
            builder.RegisterInstance(levelConfig);

            builder.RegisterComponentInHierarchy<GameplayEntryPoint>();

            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BattleClockService>().As<IBattleClock>();
            builder.Register<IBattleActionRuntimeService, BattleActionRuntimeService>(Lifetime.Singleton);
            builder.Register<IMoveCounterService, MoveCounterService>(Lifetime.Singleton);
            builder.Register<IAvatarGroupDefenseService, AvatarGroupDefenseService>(Lifetime.Singleton);
            builder.Register<IEnemyStateService, EnemyStateService>(Lifetime.Singleton);
            builder.Register<IPlayerStateService, PlayerStateService>(Lifetime.Singleton);
            builder.Register<ILevelProgressionService, LevelProgressionService>(Lifetime.Singleton);
            builder.Register<IMoveBarService, MoveBarService>(Lifetime.Singleton);
            builder.Register<IHeroService, HeroService>(Lifetime.Singleton);
            builder.Register<HeroBuffService>(Lifetime.Singleton)
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
                .As<IResurrectOnDeathBuffService>();
            builder.RegisterEntryPoint<HeroPassiveService>().As<IHeroPassiveService>();
            builder.RegisterEntryPoint<BattleSideEnergyService>().As<IBattleSideEnergyService>();
            builder.Register<IUnitActivationCooldownService, UnitActivationCooldownService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PlayerAvatarChargeService>().As<IPlayerAvatarChargeService>();
            builder.RegisterEntryPoint<EnemyAvatarChargeService>().As<IEnemyAvatarChargeService>();
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
            builder.Register<IAbilityExecutionService, AbilityExecutionService>(Lifetime.Singleton);

            builder.Register<MoveBarViewModel>(Lifetime.Singleton);
            builder.Register<BattleFieldViewModel>(Lifetime.Singleton);
            builder.Register<GameResultPresenter>(Lifetime.Singleton);
            builder.Register<GameResultSequenceController>(Lifetime.Singleton);
            builder.Register<IReadyPulseCoordinator, ReadyPulseCoordinator>(Lifetime.Singleton);

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

            if (levelConfig.BotConfig)
            {
                builder.RegisterInstance(levelConfig.BotConfig);
                builder.RegisterEntryPoint<BotOpponentService>().As<IBotOpponentService>();
            }
        }
    }
}