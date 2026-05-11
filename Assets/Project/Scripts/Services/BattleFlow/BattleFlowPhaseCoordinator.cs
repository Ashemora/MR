using System;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Timer;
using Project.Scripts.Shared.BattleFlow;
using VContainer.Unity;
using R3;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.BattleFlow
{
    public class BattleFlowPhaseCoordinator : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IGameStateService _gameStateService;
        private readonly IBurndownTransitionCoordinator _burndownTransitionCoordinator;
        private readonly IAvatarService _avatarService;
        private IDisposable _phaseChangedSubscription;
        private IDisposable _boardResolvingSubscription;
        private IDisposable _gameStateSubscription;
        private bool _pendingHeroPhaseOpen;


        public BattleFlowPhaseCoordinator(EventBus eventBus, IBattleFlowService battleFlowService,
            IBoardRuntimeService boardRuntimeService, IBattleActionRuntimeService battleActionRuntimeService,
            IGameStateService gameStateService, IBurndownTransitionCoordinator burndownTransitionCoordinator,
            IAvatarService avatarService)
        {
            _eventBus = eventBus;
            _battleFlowService = battleFlowService;
            _boardRuntimeService = boardRuntimeService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _gameStateService = gameStateService;
            _burndownTransitionCoordinator = burndownTransitionCoordinator;
            _avatarService = avatarService;
        }


        public void Start()
        {
            _phaseChangedSubscription = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);
            _boardResolvingSubscription = _boardRuntimeService.IsResolvingState.Subscribe(OnBoardResolvingChanged);
            _gameStateSubscription = _gameStateService.State.Subscribe(OnGameStateChanged);

            if (_battleFlowService.IsInitialized)
                ApplyPhase(_battleFlowService.Snapshot.Phase);
        }


        public void Dispose()
        {
            _phaseChangedSubscription?.Dispose();
            _phaseChangedSubscription = null;
            _boardResolvingSubscription?.Dispose();
            _boardResolvingSubscription = null;
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = null;
        }


        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            ApplyPhase(e.Phase);
        }

        private void OnBoardResolvingChanged(bool isResolving)
        {
            if (isResolving)
                return;

            TryCompletePendingHeroPhase();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Win && state != GameState.Lose)
                return;

            _battleFlowService.MarkFinished();
        }

        private void ApplyPhase(BattlePhaseKind phase)
        {
            if (phase == BattlePhaseKind.PendingHero)
            {
                _pendingHeroPhaseOpen = true;
                _boardRuntimeService.RequestMatchPhaseClose();
                TryCompletePendingHeroPhase();
                
                return;
            }

            _pendingHeroPhaseOpen = false;
            _boardRuntimeService.ApplyBattleFlowPhase(phase);
            _battleActionRuntimeService.ApplyBattleFlowPhase(phase);

            if (phase == BattlePhaseKind.PendingBurndown && _avatarService.IsAlive(BattleSide.Player)
                                                         && _avatarService.IsAlive(BattleSide.Enemy))
            {
                _burndownTransitionCoordinator.RequestStart();
            }
        }

        private void TryCompletePendingHeroPhase()
        {
            if (false == _pendingHeroPhaseOpen)
                return;

            if (_boardRuntimeService.IsResolving)
                return;

            _battleFlowService.BeginHeroPhase();
        }
    }
}