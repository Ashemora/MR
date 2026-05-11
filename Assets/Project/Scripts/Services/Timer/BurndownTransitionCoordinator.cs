using Project.Scripts.Services.Board;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;

namespace Project.Scripts.Services.Timer
{
    public class BurndownTransitionCoordinator : IBurndownTransitionCoordinator
    {
        public bool IsStarted { get; private set; }


        private readonly EventBus _eventBus;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IGameStateService _gameStateService;
        private readonly IBurndownService _burndownService;


        public BurndownTransitionCoordinator(EventBus eventBus, IBoardRuntimeService boardRuntimeService,
            IBattleActionRuntimeService battleActionRuntimeService, IGameStateService gameStateService,
            IBurndownService burndownService)
        {
            _eventBus = eventBus;
            _boardRuntimeService = boardRuntimeService;
            _battleActionRuntimeService = battleActionRuntimeService;
            _gameStateService = gameStateService;
            _burndownService = burndownService;
        }


        public void RequestStart()
        {
            if (IsStarted)
                return;

            var state = _gameStateService.State.CurrentValue;
            if (state == GameState.Win || state == GameState.Lose)
                return;

            IsStarted = true;
            _boardRuntimeService.RequestBurndownStop();
            _battleActionRuntimeService.RequestBurndownStop();

            if (state != GameState.Burndown)
                _gameStateService.SetState(GameState.Burndown);

            _eventBus.Publish(new BurndownStartedEvent());
            _burndownService.Begin();
            _battleActionRuntimeService.MarkBlocked();
        }
    }
}