using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.Battle.Units;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.UISystem;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.HUD
{
    public class BattleFieldViewModel : BaseViewModel
    {
        public AvatarSlotViewModel PlayerAvatar { get; private set; }
        public AvatarSlotViewModel EnemyAvatar { get; private set; }
        public HeroSlotViewModel[] PlayerHeroSlots => _playerHeroSlots;
        public HeroSlotViewModel[] EnemyHeroSlots => _enemyHeroSlots;
        public EventBus EventBus => _eventBus;
        public IGameStateService GameStateService => _gameStateService;
        public IBattleActionRuntimeService BattleActionRuntime => _battleActionRuntimeService;
        public IReadyPulseCoordinator PulseCoordinator { get; }
        public IAbilityExecutionService AbilityExecution { get; }
        public IAvatarGroupDefenseService GroupDefense { get; }
        public TileKind[] PlayerHeroKinds => _slotLayoutConfig.HeroSlotKinds;
        public string EnemyName => _levelConfig.BotConfig ? _levelConfig.BotConfig.OpponentName : string.Empty;
        public BattleAnimationConfig BattleAnimConfig => _battleAnimationConfig;
        public UnitDeathConfig DeathConfig { get; private set; }
        public ReadOnlyReactiveProperty<int> TimerSeconds => _timerSeconds;
        public ReadOnlyReactiveProperty<int> CurrentRound => _currentRound;
        public ReadOnlyReactiveProperty<bool> IsInteractionOverlayVisible => _isInteractionOverlayVisible;
        public ReadOnlyReactiveProperty<int> PlayerEnergy => _playerEnergy;
        public ReadOnlyReactiveProperty<int> EnemyEnergy => _enemyEnergy;
        public ReadOnlyReactiveProperty<int> EnergyCap => _energyCap;


        private readonly EventBus _eventBus;
        private readonly IEnemyStateService _enemyState;
        private readonly IPlayerStateService _playerState;
        private readonly BattleAnimationConfig _battleAnimationConfig;
        private readonly IHeroService _heroService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IAbilityPowerModifierService _abilityPowerModifierService;
        private readonly IUnitActivationCooldownService _unitActivationCooldownService;
        private readonly TileKindPaletteConfig _palette;
        private readonly LevelConfig _levelConfig;
        private readonly SlotLayoutConfig _slotLayoutConfig;
        private readonly IGameStateService _gameStateService;
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly BattleFlowConfig _battleFlowConfig;
        private readonly UnitDeathConfig _unitDeathConfig;
        private HeroSlotViewModel[] _playerHeroSlots;
        private HeroSlotViewModel[] _enemyHeroSlots;
        private readonly ReactiveProperty<int> _timerSeconds;
        private readonly ReactiveProperty<int> _currentRound;
        private readonly ReactiveProperty<bool> _isInteractionOverlayVisible;
        private readonly ReactiveProperty<int> _playerEnergy;
        private readonly ReactiveProperty<int> _enemyEnergy;
        private readonly ReactiveProperty<int> _energyCap;


        public BattleFieldViewModel(
            EventBus eventBus,
            IEnemyStateService enemyState,
            IPlayerStateService playerState,
            BattleAnimationConfig battleAnimationConfig,
            IHeroService heroService,
            IBattleSideEnergyService battleSideEnergyService,
            IAbilityPowerModifierService abilityPowerModifierService,
            IUnitActivationCooldownService unitActivationCooldownService,
            TileKindPaletteConfig palette,
            LevelConfig levelConfig,
            SlotLayoutConfig slotLayoutConfig,
            IGameStateService gameStateService,
            IBattleFlowService battleFlowService,
            IBattleActionRuntimeService battleActionRuntimeService,
            IReadyPulseCoordinator pulseCoordinator,
            IAbilityExecutionService abilityExecution,
            IAvatarGroupDefenseService groupDefense,
            BattleFlowConfig battleFlowConfig,
            UnitDeathConfig unitDeathConfig)
        {
            _eventBus = eventBus;
            _enemyState = enemyState;
            _playerState = playerState;
            _battleAnimationConfig = battleAnimationConfig;
            _heroService = heroService;
            _battleSideEnergyService = battleSideEnergyService;
            _abilityPowerModifierService = abilityPowerModifierService;
            _unitActivationCooldownService = unitActivationCooldownService;
            _palette = palette;
            _levelConfig = levelConfig;
            _slotLayoutConfig = slotLayoutConfig;
            _gameStateService = gameStateService;
            _battleFlowService = battleFlowService;
            _battleActionRuntimeService = battleActionRuntimeService;
            PulseCoordinator = pulseCoordinator;
            AbilityExecution = abilityExecution;
            GroupDefense = groupDefense;
            _battleFlowConfig = battleFlowConfig;
            _unitDeathConfig = unitDeathConfig;

            _timerSeconds = new ReactiveProperty<int>((int)battleFlowConfig.MatchPhaseDuration);
            _currentRound = new ReactiveProperty<int>(battleFlowService.Snapshot.CurrentRound);
            _isInteractionOverlayVisible = new ReactiveProperty<bool>(gameStateService.State.CurrentValue == GameState.Playing
                                                                      && false == battleActionRuntimeService.CanAcceptNormalActions);
            _playerEnergy = new ReactiveProperty<int>(battleSideEnergyService.GetDisplayEnergy(BattleSide.Player));
            _enemyEnergy = new ReactiveProperty<int>(battleSideEnergyService.GetDisplayEnergy(BattleSide.Enemy));
            _energyCap = new ReactiveProperty<int>(battleSideEnergyService.EnergyCap);
        }


        protected override UniTask OnInitializeAsync()
        {
            DeathConfig = _unitDeathConfig;
            var avatarColor = _palette.GetColor(_slotLayoutConfig.AvatarSlotKind, Color.gray);

            PlayerAvatar = new AvatarSlotViewModel(
                _eventBus,
                BattleSide.Player,
                avatarColor,
                _levelConfig.PlayerAvatarConfig.Portrait,
                _playerState.CurrentHP,
                _playerState.MaxHP,
                _battleAnimationConfig,
                _levelConfig.PlayerAvatarConfig.AbilityType,
                _levelConfig.PlayerAvatarConfig.ActivationEnergyCost,
                GetAvatarAbilityPower(BattleSide.Player, _levelConfig.PlayerAvatarConfig),
                _unitActivationCooldownService,
                _battleActionRuntimeService);

            EnemyAvatar = new AvatarSlotViewModel(
                _eventBus,
                BattleSide.Enemy,
                avatarColor,
                _levelConfig.EnemyAvatarConfig.Portrait,
                _enemyState.CurrentHP,
                _enemyState.MaxHP,
                _battleAnimationConfig,
                _levelConfig.EnemyAvatarConfig.AbilityType,
                _levelConfig.EnemyAvatarConfig.ActivationEnergyCost,
                GetAvatarAbilityPower(BattleSide.Enemy, _levelConfig.EnemyAvatarConfig),
                _unitActivationCooldownService,
                _battleActionRuntimeService);

            _playerHeroSlots = CreateHeroSlotViewModels(
                BattleSide.Player,
                _heroService.GetSlots(BattleSide.Player),
                _levelConfig.PlayerHeroes);

            _enemyHeroSlots = CreateHeroSlotViewModels(
                BattleSide.Enemy,
                _heroService.GetSlots(BattleSide.Enemy),
                _levelConfig.EnemyHeroes);

            Disposables.Add(_eventBus.Subscribe<HeroHPChangedEvent>(OnHeroHPChanged));
            Disposables.Add(_eventBus.Subscribe<HeroDefeatedEvent>(OnHeroDefeated));
            Disposables.Add(_eventBus.Subscribe<HeroResurrectedEvent>(OnHeroResurrected));
            Disposables.Add(_eventBus.Subscribe<HeroSlotKindPassiveStateChangedEvent>(OnHeroSlotKindPassiveStateChanged));
            Disposables.Add(_eventBus.Subscribe<HeroAbilityStatsChangedEvent>(OnHeroAbilityStatsChanged));
            Disposables.Add(_eventBus.Subscribe<BattleFlowPhaseChangedEvent>(_ => RefreshInteractionOverlay()));
            Disposables.Add(_eventBus.Subscribe<BattleFlowTimerChangedEvent>(OnBattleFlowTimerChanged));
            Disposables.Add(_eventBus.Subscribe<BattleFlowRoundChangedEvent>(OnBattleFlowRoundChanged));
            Disposables.Add(_eventBus.Subscribe<BattleSideEnergyChangedEvent>(OnBattleSideEnergyChanged));
            Disposables.Add(_battleActionRuntimeService.State.Subscribe(_ => RefreshInteractionOverlay()));
            Disposables.Add(_gameStateService.State.Subscribe(_ => RefreshInteractionOverlay()));
            RefreshInteractionOverlay();

            return UniTask.CompletedTask;
        }

        protected override void OnCleanup()
        {
            _timerSeconds.Dispose();
            _currentRound.Dispose();
            _isInteractionOverlayVisible.Dispose();
            _playerEnergy.Dispose();
            _enemyEnergy.Dispose();
            _energyCap.Dispose();
            PlayerAvatar?.Dispose();
            EnemyAvatar?.Dispose();

            if (null != _playerHeroSlots)
                for (var i = 0; i < _playerHeroSlots.Length; i++)
                    _playerHeroSlots[i]?.Dispose();

            if (null != _enemyHeroSlots)
                for (var i = 0; i < _enemyHeroSlots.Length; i++)
                    _enemyHeroSlots[i]?.Dispose();
        }


        private void OnHeroHPChanged(HeroHPChangedEvent e)
        {
            var slots = e.Side == BattleSide.Player ? _playerHeroSlots : _enemyHeroSlots;
            if (null == slots || e.SlotIndex < 0 || e.SlotIndex >= slots.Length)
                return;

            slots[e.SlotIndex]?.UpdateHP(e.Current, e.Max, e.Silent);
        }

        private void OnHeroDefeated(HeroDefeatedEvent e)
        {
            // Reserved for future side effects (sound, particles, etc.).
        }

        private void OnHeroResurrected(HeroResurrectedEvent e)
        {
            // Reserved for future side effects (sound, particles, etc.).
        }

        private void OnHeroSlotKindPassiveStateChanged(HeroSlotKindPassiveStateChangedEvent e)
        {
            var slots = e.Side == BattleSide.Player ? _playerHeroSlots : _enemyHeroSlots;
            if (null == slots || e.SlotIndex < 0 || e.SlotIndex >= slots.Length)
                return;

            slots[e.SlotIndex]?.SetSlotKindPassiveActive(e.IsActive);
        }

        private void OnHeroAbilityStatsChanged(HeroAbilityStatsChangedEvent e)
        {
            var slots = e.Side == BattleSide.Player ? _playerHeroSlots : _enemyHeroSlots;
            if (null == slots || e.SlotIndex < 0 || e.SlotIndex >= slots.Length)
                return;

            slots[e.SlotIndex]?.UpdateAbilityStats(e.ActivationEnergyCost, e.AbilityPower);
        }

        private void OnBattleFlowTimerChanged(BattleFlowTimerChangedEvent e)
        {
            _timerSeconds.Value = (int)e.TimeRemaining;
        }

        private void OnBattleFlowRoundChanged(BattleFlowRoundChangedEvent e)
        {
            _currentRound.Value = e.CurrentRound;
        }

        private void OnBattleSideEnergyChanged(BattleSideEnergyChangedEvent e)
        {
            if (e.Side == BattleSide.Player)
                _playerEnergy.Value = e.Current;
            else
                _enemyEnergy.Value = e.Current;

            if (_energyCap.Value != e.EnergyCap)
                _energyCap.Value = e.EnergyCap;
        }

        private void RefreshInteractionOverlay()
        {
            _isInteractionOverlayVisible.Value = ShouldShowBattleOverlay();
        }

        private bool ShouldShowBattleOverlay()
        {
            if (_gameStateService.State.CurrentValue != GameState.Playing)
                return false;

            if (_battleFlowService.IsPrePhase)
            {
                if (_battleFlowConfig.DimCurrentPhaseDuringPrePhase)
                    return true;

                return _battleFlowService.Snapshot.UpcomingPhase == BattlePhaseKind.Match;
            }

            return false == _battleActionRuntimeService.CanAcceptNormalActions;
        }

        private HeroSlotViewModel[] CreateHeroSlotViewModels(BattleSide side, IReadOnlyList<HeroSlotState> states,
            HeroConfig[] configs)
        {
            var slots = new HeroSlotViewModel[4];

            for (var i = 0; i < 4; i++)
            {
                var state = states[i];
                var config = null != configs && i < configs.Length ? configs[i] : null;
                var color = state.IsAssigned
                    ? _palette.GetColor(state.SlotKind, Color.gray)
                    : Color.gray;
                var portrait = config ? config.Portrait : null;

                slots[i] = new HeroSlotViewModel(i, side, state, color, portrait, _eventBus,
                    _unitActivationCooldownService, _battleActionRuntimeService);
            }

            return slots;
        }

        private int GetAvatarAbilityPower(BattleSide side, AvatarConfig config)
        {
            return _abilityPowerModifierService.GetAbilityPower(UnitDescriptor.Avatar(side, config.AbilityType),
                config.AbilityPower);
        }
    }
}