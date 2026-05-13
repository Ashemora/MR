using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Battle.Layout;
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
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
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
            var slotLayoutConfig = Parent.Container.Resolve<SlotLayoutConfig>();
            var playerBattleConfig = Parent.Container.Resolve<PlayerBattleConfig>();
            builder.RegisterInstance(BattleSetupFactory.Create(playerBattleConfig.DefaultUnitDeck,
                levelConfig.OpponentUnitDeck, slotLayoutConfig));

            builder.RegisterComponentInHierarchy<GameplayEntryPoint>();
            builder.Register<BoardSystemsFactory>(Lifetime.Singleton);

            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<BattleClockService>().As<IBattleClock>();
            builder.Register<IBattleActionRuntimeService, BattleActionRuntimeService>(Lifetime.Singleton);
            builder.Register<IMoveCounterService, MoveCounterService>(Lifetime.Singleton);
            builder.Register<IAvatarGroupDefenseService, AvatarGroupDefenseService>(Lifetime.Singleton);
            builder.Register<IAvatarService, AvatarService>(Lifetime.Singleton);
            builder.Register<IUnitStateService, UnitStateService>(Lifetime.Singleton);
            builder.Register<ILevelProgressionService, LevelProgressionService>(Lifetime.Singleton);
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