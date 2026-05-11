using Cysharp.Threading.Tasks;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Gameplay.Results;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Services.Input;
using Project.Scripts.Services.Layout;
using Project.Scripts.Services.Timer;
using Project.Scripts.Services.UISystem;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using Project.Scripts.Services.BoardEdit;
#endif

namespace Project.Scripts.Gameplay
{
    public class GameplayEntryPoint : MonoBehaviour
    {
        [Tooltip("Родительский Transform для всех инстанцируемых объектов тайлов")]
        [SerializeField] private BattleWorldLayout _battleWorldLayout;


        private EventBus _eventBus;
        private BoardConfig _boardConfig;
        private GridConfig _gridConfig;
        private InputConfig _inputConfig;
        private UIConfig _uiConfig;
        private BattleWorldLayoutConfig _battleWorldLayoutConfig;
        private BattleFieldLayoutConfig _battleFieldLayoutConfig;
        private GameplayScreenLayoutConfig _gameplayScreenLayoutConfig;
        private BattleFlowConfig _battleFlowConfig;
        private UIService _uiService;
        private MoveBarViewModel _moveBarViewModel;
        private IGameStateService _gameStateService;
        private IBoardRuntimeService _boardRuntimeService;
        private IMoveBarService _moveBarService;
        private GameResultPresenter _gameResultPresenter;
        private GameResultSequenceController _gameResultSequenceController;
        private BattleFieldViewModel _battleFieldViewModel;
        private IBoardBoundsProvider _boardBoundsProvider;
        private IGameplayScreenLayoutService _gameplayScreenLayoutService;
        private BattleFieldView _battleFieldView;
        private InputService _inputService;
        private BoardSystemsFactory _boardSystemsFactory;
        private BoardSystems _boardSystems;
        private IBattleFlowService _battleFlowService;
        private IBurndownService _burndownService;
        private IUnitActivationCooldownService _unitActivationCooldownService;
        private TileKindPaletteConfig _palette;
        private IBuffService _buffService;
        private IBoardAnnouncementService _boardAnnouncementService;
        private BattleFieldPhaseLayoutController _phaseLayout;
        private GameplayWorldLayoutController _worldLayout;
        private TopBarLayoutDriver _topBarLayout;


        private void Start()
        {
            InitAsync().Forget();
        }

        private void Update()
        {
            if (null == _moveBarService)
                return;

            _worldLayout?.TickEditorResize();

            var gameState = _gameStateService.State.CurrentValue;
            if (gameState != GameState.Playing && gameState != GameState.Burndown)
                return;

            if (gameState == GameState.Burndown)
            {
                _burndownService?.Tick(Time.deltaTime);
                return;
            }

            var isPrePhase = _battleFlowService is { IsInitialized: true, IsPrePhase: true };
            _battleFlowService?.Tick(Time.deltaTime);

            if (_gameStateService.State.CurrentValue == GameState.Burndown)
            {
                _burndownService?.Tick(Time.deltaTime);
                return;
            }

            if (isPrePhase)
                return;

            _burndownService?.Tick(Time.deltaTime);
            _unitActivationCooldownService?.Tick(Time.deltaTime);
            _buffService?.Tick(Time.deltaTime);

            if (_moveBarService.IsEnabled && _boardRuntimeService.CanAcceptInput)
                _moveBarService.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _battleFieldView?.ReleaseSceneInstance();
            _battleWorldLayout?.EnergyView?.Cleanup();

            if (true == _moveBarService?.IsEnabled)
                _uiService?.Close<MoveBarView>();

            _topBarLayout?.Dispose();
            _phaseLayout?.Dispose();
            _worldLayout?.Dispose();
            _boardSystems?.Dispose();
            _boardSystems = null;
            _inputService?.Dispose();
        }

        [Inject]
        public void Construct(
            EventBus eventBus,
            BoardConfig boardConfig,
            GridConfig gridConfig,
            InputConfig inputConfig,
            UIConfig uiConfig,
            BattleWorldLayoutConfig battleWorldLayoutConfig,
            BattleFieldLayoutConfig battleFieldLayoutConfig,
            GameplayScreenLayoutConfig gameplayScreenLayoutConfig,
            BattleFlowConfig battleFlowConfig,
            UIService uiService,
            MoveBarViewModel moveBarViewModel,
            IGameStateService gameStateService,
            IBoardRuntimeService boardRuntimeService,
            IMoveBarService moveBarService,
            GameResultPresenter gameResultPresenter,
            GameResultSequenceController gameResultSequenceController,
            BattleFieldViewModel battleHUDViewModel,
            IBoardBoundsProvider boardBoundsProvider,
            IGameplayScreenLayoutService gameplayScreenLayoutService,
            BoardSystemsFactory boardSystemsFactory,
            IBattleFlowService battleFlowService,
            IBurndownService burndownService,
            IUnitActivationCooldownService unitActivationCooldownService,
            TileKindPaletteConfig palette,
            IBuffService buffService,
            IBoardAnnouncementService boardAnnouncementService)
        {
            _eventBus = eventBus;
            _boardConfig = boardConfig;
            _gridConfig = gridConfig;
            _inputConfig = inputConfig;
            _uiConfig = uiConfig;
            _battleWorldLayoutConfig = battleWorldLayoutConfig;
            _battleFieldLayoutConfig = battleFieldLayoutConfig;
            _gameplayScreenLayoutConfig = gameplayScreenLayoutConfig;
            _battleFlowConfig = battleFlowConfig;
            _uiService = uiService;
            _moveBarViewModel = moveBarViewModel;
            _gameStateService = gameStateService;
            _boardRuntimeService = boardRuntimeService;
            _moveBarService = moveBarService;
            _gameResultPresenter = gameResultPresenter;
            _gameResultSequenceController = gameResultSequenceController;
            _battleFieldViewModel = battleHUDViewModel;
            _boardBoundsProvider = boardBoundsProvider;
            _gameplayScreenLayoutService = gameplayScreenLayoutService;
            _boardSystemsFactory = boardSystemsFactory;
            _battleFlowService = battleFlowService;
            _burndownService = burndownService;
            _unitActivationCooldownService = unitActivationCooldownService;
            _palette = palette;
            _buffService = buffService;
            _boardAnnouncementService = boardAnnouncementService;
        }

        private async UniTaskVoid InitAsync()
        {
            _moveBarService.Initialize();

            if (_moveBarService.IsEnabled)
                _uiService.RegisterView<MoveBarView>(_uiConfig.MoveBarViewPrefab, UILayer.MainDynamic);

            if (_moveBarService.IsEnabled)
                await _uiService.Show<MoveBarView, MoveBarViewModel>(_moveBarViewModel);

            _inputService = new InputService(_inputConfig);

            _battleFieldView = _battleWorldLayout.BattleFieldView;
            _battleFieldView.SetDependencies(
                _inputService,
                _palette,
                _battleWorldLayout.EnergyView ? _battleWorldLayout.EnergyView.PlayerEnergyAbsorbTarget : null);
            await _battleFieldView.InitializeAsync(_battleFieldViewModel);
            await _battleFieldView.ShowAsync();
            _battleWorldLayout.EnergyView?.Bind(_battleFieldViewModel, _boardAnnouncementService);

            _topBarLayout = new TopBarLayoutDriver(
                _uiService,
                _uiConfig,
                _gameplayScreenLayoutService,
                _gameplayScreenLayoutConfig,
                _battleFieldView);
            _topBarLayout.RegisterView();

            _phaseLayout = new BattleFieldPhaseLayoutController(_eventBus, _gameStateService,
                _boardRuntimeService, _battleFlowService, _battleFieldLayoutConfig, _battleFlowConfig,
                _battleWorldLayout, _battleFieldView, _boardBoundsProvider);

            _worldLayout = new GameplayWorldLayoutController(_battleWorldLayout, _boardConfig,
                _gridConfig, _battleWorldLayoutConfig, _gameplayScreenLayoutConfig,
                _gameplayScreenLayoutService, _boardBoundsProvider, _battleFieldView);

            _topBarLayout.Subscribe(_worldLayout, _phaseLayout);

            _phaseLayout.ApplyInitialBlend();
            var initialLayout = _worldLayout.Initialize();

            await _topBarLayout.ShowAsync(_battleFieldViewModel);

            _gameResultSequenceController.BindVisuals(_battleFieldView);

            _boardSystems = _boardSystemsFactory.Create(_inputService, _battleWorldLayout, initialLayout);
            _worldLayout.BindBoardSystems(_boardSystems.GridManager, _boardSystems.TilePool);

            _battleWorldLayout.BoardView.Setup(initialLayout.FrameWidth, initialLayout.FrameHeight, initialLayout.TileCellSize, _boardConfig.MaskTopPadding);
            await _topBarLayout.ApplyLayoutWhenReadyAsync();

            _gameResultPresenter.Initialize();
            _gameResultSequenceController.Initialize();
            _battleFlowService.Initialize();
            _phaseLayout.Initialize();

#if UNITY_EDITOR
            var editHandler = gameObject.AddComponent<BoardEditClickHandler>();
            editHandler.Init(_boardSystems.GridManager.State, _boardSystems.GridManager, _gridConfig, initialLayout.TileCellSize);
#endif

            await _inputService.InitAsync();
            await _boardSystems.SwapHandler.InitAsync();
            await _boardSystems.Orchestrator.InitAsync();
            await _boardSystems.Orchestrator.StartGame();
        }
    }
}