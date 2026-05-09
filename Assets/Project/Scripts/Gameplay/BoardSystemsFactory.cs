using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Services.Audio;
using Project.Scripts.Services.Audio.AudioSystem;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Combat;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Grid;
using Project.Scripts.Services.Input;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Rules;

namespace Project.Scripts.Gameplay
{
    public sealed class BoardSystems : IDisposable
    {
        private readonly HintService _hintService;
        private readonly PassiveTileGlowService _passiveTileGlowService;
        private readonly GameAudioController _gameAudioController;
        private readonly IDisposable _burndownStartedSubscription;


        public GridManager GridManager { get; }
        public TilePool TilePool { get; }
        public SwapInputHandler SwapHandler { get; }
        public BoardOrchestrator Orchestrator { get; }


        public BoardSystems(
            GridManager gridManager,
            TilePool tilePool,
            SwapInputHandler swapHandler,
            BoardOrchestrator orchestrator,
            HintService hintService,
            PassiveTileGlowService passiveTileGlowService,
            GameAudioController gameAudioController,
            IDisposable burndownStartedSubscription)
        {
            GridManager = gridManager;
            TilePool = tilePool;
            SwapHandler = swapHandler;
            Orchestrator = orchestrator;
            _hintService = hintService;
            _passiveTileGlowService = passiveTileGlowService;
            _gameAudioController = gameAudioController;
            _burndownStartedSubscription = burndownStartedSubscription;
        }

        public void Dispose()
        {
            _hintService?.Dispose();
            _passiveTileGlowService?.Dispose();
            Orchestrator?.Dispose();
            SwapHandler?.Dispose();
            _gameAudioController?.Dispose();
            _burndownStartedSubscription?.Dispose();
        }
    }

    public sealed class BoardSystemsFactory
    {
        private readonly EventBus _eventBus;
        private readonly AudioService _audioService;
        private readonly BoardConfig _boardConfig;
        private readonly GridConfig _gridConfig;
        private readonly LevelConfig _levelConfig;
        private readonly BoardAnimationConfig _animConfig;
        private readonly InputConfig _inputConfig;
        private readonly CascadeEnergyConfig _cascadeEnergyConfig;
        private readonly SpecialTileConfig _specialTileConfig;
        private readonly IGameStateService _gameStateService;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IMoveBarService _moveBarService;
        private readonly BurndownConfig _burndownConfig;
        private readonly HintConfig _hintConfig;
        private readonly TileKindPaletteConfig _palette;
        private readonly IBuffService _buffService;
        private readonly IBombRadiusModifierService _bombRadiusModifierService;
        private readonly ILineRuneModifierService _lineRuneModifierService;
        private readonly DebugConfig _debugConfig;


        public BoardSystemsFactory(
            EventBus eventBus,
            AudioService audioService,
            BoardConfig boardConfig,
            GridConfig gridConfig,
            LevelConfig levelConfig,
            BoardAnimationConfig animConfig,
            InputConfig inputConfig,
            CascadeEnergyConfig cascadeEnergyConfig,
            SpecialTileConfig specialTileConfig,
            IGameStateService gameStateService,
            IBoardRuntimeService boardRuntimeService,
            IMoveBarService moveBarService,
            BurndownConfig burndownConfig,
            HintConfig hintConfig,
            TileKindPaletteConfig palette,
            IBuffService buffService,
            IBombRadiusModifierService bombRadiusModifierService,
            ILineRuneModifierService lineRuneModifierService,
            DebugConfig debugConfig)
        {
            _eventBus = eventBus;
            _audioService = audioService;
            _boardConfig = boardConfig;
            _gridConfig = gridConfig;
            _levelConfig = levelConfig;
            _animConfig = animConfig;
            _inputConfig = inputConfig;
            _cascadeEnergyConfig = cascadeEnergyConfig;
            _specialTileConfig = specialTileConfig;
            _gameStateService = gameStateService;
            _boardRuntimeService = boardRuntimeService;
            _moveBarService = moveBarService;
            _burndownConfig = burndownConfig;
            _hintConfig = hintConfig;
            _palette = palette;
            _buffService = buffService;
            _bombRadiusModifierService = bombRadiusModifierService;
            _lineRuneModifierService = lineRuneModifierService;
            _debugConfig = debugConfig;
        }

        public BoardSystems Create(InputService inputService, BattleWorldLayout battleWorldLayout,
            GameplayWorldLayoutController.InitialWorldLayout initialLayout)
        {
            var pool = new TilePool(_boardConfig.TilePrefab, battleWorldLayout.TileContainer, _animConfig,
                initialLayout.TileCellSize, _boardConfig.TileFillPercent);
            var matchFinder = new MatchFinder(MatchRules.MinMatchLength);
            var gridManager = new GridManager(_levelConfig, _gridConfig, _animConfig, pool,
                initialLayout.TileCellSize, _boardRuntimeService, _eventBus, _bombRadiusModifierService,
                _lineRuneModifierService);
            gridManager.SetOrigin(initialLayout.GridOrigin);

            var gravityHandler = new GravityHandler(gridManager.State, gridManager, pool, _gridConfig,
                _boardRuntimeService);
            var swapHandler = new SwapInputHandler(inputService, gridManager.State, gridManager,
                _inputConfig.WorldDragThreshold, _inputConfig.ReanchorOnUnlock);
            var moveChecker = new MoveChecker(gridManager.State, gridManager, matchFinder, _gridConfig);
            var specialTileResolver = new SpecialTileResolver(_specialTileConfig, _levelConfig);
            var swapComboResolver = new SwapComboResolver();

            var orchestrator = new BoardOrchestrator(
                _eventBus,
                gridManager.State,
                gridManager,
                gridManager,
                gravityHandler,
                matchFinder,
                swapHandler,
                moveChecker,
                _cascadeEnergyConfig,
                _gameStateService,
                _boardRuntimeService,
                _moveBarService,
                specialTileResolver,
                swapComboResolver,
                _bombRadiusModifierService,
                _lineRuneModifierService,
                _debugConfig);

            var hintService = new HintService(_hintConfig, gridManager.State, gridManager, matchFinder,
                _gridConfig, _gameStateService, _boardRuntimeService, _eventBus);
            var passiveTileGlowService = new PassiveTileGlowService(_eventBus, gridManager, _gridConfig,
                _buffService, _palette);

            var gameAudioController = new GameAudioController(_audioService, _eventBus, _gameStateService);
            gameAudioController.StartMusic();

            var burndownStartedSubscription = _eventBus.Subscribe<BurndownStartedEvent>(_ =>
            {
                gridManager.CollapseAll(_burndownConfig.CollapseAllDuration, _burndownConfig.CollapseAllEase).Forget();
            });

            return new BoardSystems(
                gridManager,
                pool,
                swapHandler,
                orchestrator,
                hintService,
                passiveTileGlowService,
                gameAudioController,
                burndownStartedSubscription);
        }
    }
}