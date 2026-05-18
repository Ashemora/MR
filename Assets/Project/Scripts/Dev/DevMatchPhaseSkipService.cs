#if DEV
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Dev
{
    public class DevMatchPhaseSkipService
    {
        private const float MinimumSkipTickDelta = 0.001f;

        
        private readonly IBattleFlowService _battleFlowService;
        private readonly IBattleSideEnergyService _battleSideEnergyService;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IGameStateService _gameStateService;


        public DevMatchPhaseSkipService(IBattleFlowService battleFlowService, IBattleSideEnergyService battleSideEnergyService,
            IBoardRuntimeService boardRuntimeService, IGameStateService gameStateService)
        {
            _battleFlowService = battleFlowService;
            _battleSideEnergyService = battleSideEnergyService;
            _boardRuntimeService = boardRuntimeService;
            _gameStateService = gameStateService;
        }

        public bool CanSkip()
        {
            return ShouldShow() && false == _boardRuntimeService.IsResolving;
        }

        public bool ShouldShow()
        {
            if (false == IsVisibleDuringGameplay())
                return false;

            return _battleFlowService.Snapshot.Phase == BattlePhaseKind.Match;
        }

        public bool IsVisibleDuringGameplay()
        {
            if (false == _battleFlowService.IsInitialized)
                return false;

            return _gameStateService.State.CurrentValue == GameState.Playing;
        }

        public bool TrySkip()
        {
            if (false == CanSkip())
                return false;

            FillPlayerEnergyToCap();

            var snapshot = _battleFlowService.Snapshot;
            if (snapshot.Phase != BattlePhaseKind.Match)
                return false;

            var deltaTime = snapshot.TimeRemaining > 0f
                ? snapshot.TimeRemaining
                : MinimumSkipTickDelta;
            _battleFlowService.Tick(deltaTime);

            return true;
        }

        
        private void FillPlayerEnergyToCap()
        {
            var cap = _battleSideEnergyService.EnergyCap;
            var current = _battleSideEnergyService.GetDisplayEnergy(BattleSide.Player);
            var delta = cap - current;
            if (delta > 0)
                _battleSideEnergyService.AddEnergy(BattleSide.Player, delta);
        }
    }
}
#endif