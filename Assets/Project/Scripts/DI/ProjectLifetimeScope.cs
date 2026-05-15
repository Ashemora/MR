using Project.Scripts.Configs;
using Project.Scripts.Services.Audio.AudioSystem;
using Project.Scripts.Services.Audio.Settings;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Progression;
using Project.Scripts.Services.SceneLoading;
using Project.Scripts.Services.UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.DI
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainConfig _mainConfig;


        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_mainConfig.BoardConfig);
            builder.RegisterInstance(_mainConfig.GridConfig);
            builder.RegisterInstance(_mainConfig.BoardAnimationConfig);
            builder.RegisterInstance(_mainConfig.BattleAnimationConfig);
            builder.RegisterInstance(_mainConfig.GameResultSequenceConfig);
            builder.RegisterInstance(_mainConfig.InputConfig);
            builder.RegisterInstance(_mainConfig.CascadeEnergyConfig);
            builder.RegisterInstance(_mainConfig.AudioMusicConfig);
            builder.RegisterInstance(_mainConfig.AudioSFXConfig);
            builder.RegisterInstance(_mainConfig.SpecialTileConfig);
            builder.RegisterInstance(_mainConfig.LevelDatabase);
            builder.RegisterInstance(_mainConfig.PlayerBattleConfig);
            builder.RegisterInstance(_mainConfig.UIConfig);
            builder.RegisterInstance(_mainConfig.BoardAnnouncementConfig);
            builder.RegisterInstance(_mainConfig.MoveBarConfig);
            builder.RegisterInstance(_mainConfig.BattleWorldLayoutConfig);
            builder.RegisterInstance(_mainConfig.BattleFieldLayoutConfig);
            builder.RegisterInstance(_mainConfig.GameplayScreenLayoutConfig);
            builder.RegisterInstance(_mainConfig.TileKindPaletteConfig);
            builder.RegisterInstance(_mainConfig.SlotLayoutConfig);
            builder.RegisterInstance(_mainConfig.BattleFlowConfig);
            builder.RegisterInstance(_mainConfig.BurndownConfig);
            builder.RegisterInstance(_mainConfig.UnitDeathConfig);
            builder.RegisterInstance(_mainConfig.AutoEnergyConfig);
            builder.RegisterInstance(_mainConfig.EscalationConfig);
            builder.RegisterInstance(_mainConfig.HintConfig);
            builder.RegisterInstance(_mainConfig.DebugConfig);

            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<AudioService>(Lifetime.Singleton);
            builder.Register<AudioSettingsService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ILevelProgressionService, LevelProgressionService>(Lifetime.Singleton);
            builder.Register<ISceneLoadingService, SceneLoadingService>(Lifetime.Singleton);
            builder.Register<IBattleSessionProvider, BattleSessionProvider>(Lifetime.Singleton);
            builder.Register<IAppStateMachine, AppStateMachine>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<AudioManager>();
            builder.RegisterComponentInHierarchy<UIService>();
        }
    }
}