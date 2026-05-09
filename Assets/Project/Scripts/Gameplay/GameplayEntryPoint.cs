using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Configs;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Gameplay.Results;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Services.Audio;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Combat;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Services.Audio.AudioSystem;
using Project.Scripts.Services.Grid;
using Project.Scripts.Services.Input;
using Project.Scripts.Services.Layout;
using Project.Scripts.Services.Timer;
using Project.Scripts.Services.UISystem;
using Project.Scripts.Shared.BattleFlow;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Layout;
using UnityEngine;
using VContainer;
using R3;
#if UNITY_EDITOR
using Project.Scripts.Services.BoardEdit;
#endif

namespace Project.Scripts.Gameplay
{
    public class GameplayEntryPoint : MonoBehaviour
    {
        private const float MinLayoutCellSize = 0.01f;
        
        
        [Tooltip("Родительский Transform для всех инстанцируемых объектов тайлов")]
        [SerializeField] private BattleWorldLayout _battleWorldLayout;


        private EventBus _eventBus;
        private AudioService _audioService;
        private BoardConfig _boardConfig;
        private GridConfig _gridConfig;
        private LevelConfig _levelConfig;
        private BoardAnimationConfig _animConfig;
        private InputConfig _inputConfig;
        private CascadeEnergyConfig _cascadeEnergyConfig;
        private SpecialTileConfig _specialTileConfig;
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
        private TopBarView _topBarView;
        private InputService _inputService;
        private SwapInputHandler _swapHandler;
        private BoardOrchestrator _orchestrator;
        private GameAudioController _gameAudioController;
        private IBattleFlowService _battleFlowService;
        private IBurndownService _burndownService;
        private BurndownConfig _burndownConfig;
        private IUnitActivationCooldownService _unitActivationCooldownService;
        private HintConfig _hintConfig;
        private TileKindPaletteConfig _palette;
        private IHeroPassiveService _heroPassiveService;
        private IBuffService _buffService;
        private IBombRadiusModifierService _bombRadiusModifierService;
        private ILineRuneModifierService _lineRuneModifierService;
        private HintService _hintService;
        private PassiveTileGlowService _passiveTileGlowService;
        private DebugConfig _debugConfig;
        private IBoardAnnouncementService _boardAnnouncementService;
        private IDisposable _boardRuntimeSubscription;
        private IDisposable _gameStateSubscription;
        private IDisposable _battleFlowPhaseSubscription;
        private IDisposable _burndownStartedSubscription;
        private Tween _battleFieldLayoutTween;

#if UNITY_EDITOR
        private GridManager _gridManager;
        private TilePool _pool;
        private float _cellSize;
        private int _lastWidth;
        private int _lastHeight;
        private Rect _lastSafeArea;
        private int _delayedTopBarLayoutVersion;
#endif
        
        private enum BattleFieldLayoutIntent
        {
            Compressed,
            Full,
            Preserve
        }
        

        private void Start()
        {
            InitAsync().Forget();
        }

        private void Update()
        {
            if (null == _moveBarService)
                return;

#if UNITY_EDITOR
            ApplyLiveResizeIfNeeded();
#endif

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
            _battleFieldLayoutTween?.Kill();
            _battleFieldLayoutTween = null;

            if (_moveBarService?.IsEnabled == true)
                _uiService?.Close<MoveBarView>();

            _uiService?.Close<TopBarView>();

            _hintService?.Dispose();
            _passiveTileGlowService?.Dispose();
            _orchestrator?.Dispose();
            _swapHandler?.Dispose();
            _inputService?.Dispose();
            _boardRuntimeSubscription?.Dispose();
            _boardRuntimeSubscription = null;
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = null;
            _battleFlowPhaseSubscription?.Dispose();
            _battleFlowPhaseSubscription = null;
            _burndownStartedSubscription?.Dispose();
            _burndownStartedSubscription = null;

#if UNITY_EDITOR
            BoardConfig.LayoutChanged -= OnLayoutChanged;
            BoardConfig.TileLayoutChanged -= OnTileLayoutChanged;
            BattleWorldLayoutConfig.LayoutChanged -= OnBattleLayoutChanged;
            GameplayScreenLayoutConfig.LayoutChanged -= OnScreenLayoutChanged;
            GameplayScreenLayoutConfig.TopBarLayoutChanged -= OnTopBarLayoutChanged;
#endif
        }

        [Inject]
        public void Construct(
            EventBus eventBus,
            AudioService audioService,
            BoardConfig boardConfig,
            GridConfig gridConfig,
            LevelConfig levelConfig,
            BoardAnimationConfig animConfig,
            InputConfig inputConfig,
            CascadeEnergyConfig cascadeEnergyConfig,
            SpecialTileConfig specialTileConfig,
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
            IBattleFlowService battleFlowService,
            IBurndownService burndownService,
            BurndownConfig burndownConfig,
            IUnitActivationCooldownService unitActivationCooldownService,
            HintConfig hintConfig,
            TileKindPaletteConfig palette,
            IHeroPassiveService heroPassiveService,
            IBuffService buffService,
            IBombRadiusModifierService bombRadiusModifierService,
            ILineRuneModifierService lineRuneModifierService,
            DebugConfig debugConfig,
            IBoardAnnouncementService boardAnnouncementService)
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
            _battleFlowService = battleFlowService;
            _burndownService = burndownService;
            _burndownConfig = burndownConfig;
            _unitActivationCooldownService = unitActivationCooldownService;
            _hintConfig = hintConfig;
            _palette = palette;
            _heroPassiveService = heroPassiveService;
            _buffService = buffService;
            _bombRadiusModifierService = bombRadiusModifierService;
            _lineRuneModifierService = lineRuneModifierService;
            _debugConfig = debugConfig;
            _boardAnnouncementService = boardAnnouncementService;
        }

        private async UniTaskVoid InitAsync()
        {
            _moveBarService.Initialize();

            if (_moveBarService.IsEnabled)
                _uiService.RegisterView<MoveBarView>(_uiConfig.MoveBarViewPrefab, UILayer.MainDynamic);

            _uiService.RegisterView<TopBarView>(_uiConfig.TopBarViewPrefab, UILayer.Main);

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
            ApplyBattleFieldLayoutBlend(0f);

            var worldLayout = ComputeGameplayWorldLayout();
#if UNITY_EDITOR
            LogWorldFit("startup", worldLayout);
#endif
            ApplyBattleWorldFitScale(worldLayout.FitScale);
            var boardCenter = ComputeBoardCenter(worldLayout.WorldRect, worldLayout.FrameHeight);
            _battleWorldLayout.SetBoardWorldCenter(boardCenter);

            var boardTopWorldY = boardCenter.y + worldLayout.FrameHeight * 0.5f;
            var boardHalfWidth = worldLayout.FrameWidth * 0.5f;
            _boardBoundsProvider.SetBounds(boardCenter.x, boardTopWorldY, boardHalfWidth, worldLayout.TileCellSize);

            _battleWorldLayout.SetVerticalLayout(
                boardTopWorldY,
                worldLayout.FrameCellSize,
                _battleWorldLayoutConfig.GapBoardToPlayerEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapEnemyEnergyToBattleField * worldLayout.GapScale);
            _battleWorldLayout.RefreshBindings();
            _battleWorldLayout.PublishAnnouncementAnchors(_boardBoundsProvider);
            _topBarView = await _uiService.Show<TopBarView, BattleFieldViewModel>(_battleFieldViewModel);
            _topBarView.gameObject.SetActive(false);

            _gameResultSequenceController.BindVisuals(_battleFieldView);

            var pool = new TilePool(_boardConfig.TilePrefab, _battleWorldLayout.TileContainer, _animConfig, worldLayout.TileCellSize, _boardConfig.TileFillPercent);
            var matchFinder = new MatchFinder(MatchRules.MinMatchLength);
            var gridManager = new GridManager(_levelConfig, _gridConfig, _animConfig, pool, worldLayout.TileCellSize,
                _boardRuntimeService, _eventBus, _bombRadiusModifierService, _lineRuneModifierService);
            gridManager.SetOrigin(ComputeGridOrigin(boardCenter, worldLayout.TileCellSize));

#if UNITY_EDITOR
            _pool = pool;
            _gridManager = gridManager;
            _cellSize = worldLayout.TileCellSize;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
            BoardConfig.LayoutChanged += OnLayoutChanged;
            BoardConfig.TileLayoutChanged += OnTileLayoutChanged;
            BattleWorldLayoutConfig.LayoutChanged += OnBattleLayoutChanged;
            GameplayScreenLayoutConfig.LayoutChanged += OnScreenLayoutChanged;
            GameplayScreenLayoutConfig.TopBarLayoutChanged += OnTopBarLayoutChanged;
#endif

            _battleWorldLayout.BoardView.Setup(worldLayout.FrameWidth, worldLayout.FrameHeight, worldLayout.TileCellSize, _boardConfig.MaskTopPadding);
            await ApplyTopBarLayoutWhenReady();

            var gravityHandler = new GravityHandler(gridManager.State, gridManager, pool, _gridConfig, _boardRuntimeService);

            _swapHandler = new SwapInputHandler(_inputService, gridManager.State, gridManager, _inputConfig.WorldDragThreshold, _inputConfig.ReanchorOnUnlock);

            var moveChecker = new MoveChecker(gridManager.State, gridManager, matchFinder, _gridConfig);
            var specialTileResolver = new SpecialTileResolver(_specialTileConfig, _levelConfig);
            var swapComboResolver = new SwapComboResolver();

            _orchestrator = new BoardOrchestrator(
                _eventBus,
                gridManager.State,
                gridManager,
                gridManager,
                gravityHandler,
                matchFinder,
                _swapHandler,
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

            _hintService = new HintService(_hintConfig, gridManager.State, gridManager, matchFinder,
                _gridConfig, _gameStateService, _boardRuntimeService, _eventBus);
            
            _passiveTileGlowService = new PassiveTileGlowService(_eventBus, gridManager, _gridConfig,
                _buffService, _palette);

            _gameAudioController = new GameAudioController(_audioService, _eventBus, _gameStateService);
            _gameAudioController.StartMusic();

            _boardRuntimeSubscription?.Dispose();
            _boardRuntimeSubscription = _boardRuntimeService.State.Subscribe(_ => RefreshPhaseOverlays());
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = _gameStateService.State.Subscribe(_ => RefreshPhaseOverlays());
            _battleFlowPhaseSubscription?.Dispose();
            _battleFlowPhaseSubscription = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);
            RefreshPhaseOverlays();

            _burndownStartedSubscription?.Dispose();
            _burndownStartedSubscription = _eventBus.Subscribe<BurndownStartedEvent>(_ =>
            {
                gridManager.CollapseAll(_burndownConfig.CollapseAllDuration, _burndownConfig.CollapseAllEase).Forget();
            });

            _gameResultPresenter.Initialize();
            _gameResultSequenceController.Initialize();
            _battleFlowService.Initialize();
            ApplyBattleFieldLayoutForCurrentPhase(false);

#if UNITY_EDITOR
            var editHandler = gameObject.AddComponent<BoardEditClickHandler>();
            editHandler.Init(gridManager.State, gridManager, _gridConfig, worldLayout.TileCellSize);
#endif

            await _inputService.InitAsync();
            await _swapHandler.InitAsync();
            await _orchestrator.InitAsync();
            await _orchestrator.StartGame();
        }

        private bool ApplyTopBarLayout(string reason)
        {
            if (!_topBarView || null == _gameplayScreenLayoutService)
                return false;

            var layout = _gameplayScreenLayoutService.Calculate();
            var cam = Camera.main;
            if (!cam || !_battleFieldView)
                return false;

            var battleFieldTopScreenY = cam.WorldToScreenPoint(new Vector3(0f, _battleFieldView.LayoutTopWorldY, 0f)).y;
            var applied = _topBarView.ApplyLayout(
                _gameplayScreenLayoutService.ToUnityRect(layout.GameplayRect),
                battleFieldTopScreenY,
                _gameplayScreenLayoutConfig.TopBarSidePadding,
                _gameplayScreenLayoutConfig.TopBarBottomPadding,
                _gameplayScreenLayoutConfig.TopBarHeight);
            
            return applied;
        }

        private async UniTask ApplyTopBarLayoutWhenReady()
        {
            _topBarView.gameObject.SetActive(false);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (ApplyTopBarLayout("startup after screen settled"))
                _topBarView.gameObject.SetActive(true);
            else
                Debug.LogWarning("TopBar layout was not ready; keeping TopBar hidden to avoid showing prefab position.");
        }

#if UNITY_EDITOR
        private void OnLayoutChanged()  => ApplyLiveLayout();
        private void OnTileLayoutChanged() => ApplyLiveTileLayout();
        private void OnBattleLayoutChanged() => ApplyLiveLayout();
        private void OnScreenLayoutChanged() => ApplyLiveLayout();
        private void OnTopBarLayoutChanged() => ApplyTopBarLayout("topbar config changed");

        private static void LogWorldFit(string reason, GameplayWorldLayout worldLayout)
        {
            // Debug.Log(
            //     $"Gameplay world fit [{reason}] " +
            //     $"desired={worldLayout.DesiredStackHeight:0.###}, " +
            //     $"available={worldLayout.AvailableStackHeight:0.###}, " +
            //     $"fitScale={worldLayout.FitScale:0.###}, " +
            //     $"currentGapScale={worldLayout.GapScale:0.###}, " +
            //     $"frameCell={worldLayout.FrameCellSize:0.###}, " +
            //     $"tileCell={worldLayout.TileCellSize:0.###}");
        }

        private void ApplyLiveResize()
        {
            ApplyLiveLayout();
            ApplyTopBarLayoutAfterResizeAsync(++_delayedTopBarLayoutVersion).Forget();
        }

        private async UniTaskVoid ApplyTopBarLayoutAfterResizeAsync(int version)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (version != _delayedTopBarLayoutVersion)
                return;

            ApplyTopBarLayout("delayed resize");
        }

        private void ApplyLiveResizeIfNeeded()
        {
            if (null == _gridManager)
                return;

            var safeArea = Screen.safeArea;
            if (Screen.width == _lastWidth && Screen.height == _lastHeight && safeArea == _lastSafeArea)
                return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = safeArea;
            ApplyLiveResize();
        }

        private void ApplyLiveTileLayout()
        {
            if (null == _gridManager || null == _pool || !_battleWorldLayout)
                return;

            var worldLayout = ComputeGameplayWorldLayout();
            LogWorldFit("tile layout changed", worldLayout);
            ApplyBattleWorldFitScale(worldLayout.FitScale);
            _cellSize = worldLayout.TileCellSize;
            var boardCenter = ComputeBoardCenter(worldLayout.WorldRect, worldLayout.FrameHeight);

            _gridManager.SetCellSize(_cellSize);

            var newOrigin = ComputeGridOrigin(boardCenter, _cellSize);
            _gridManager.SetOrigin(newOrigin);
            _gridManager.RepositionAllTiles();

            _pool.UpdateScale(_cellSize, _boardConfig.TileFillPercent);

            var boardTopWorldY = boardCenter.y + worldLayout.FrameHeight * 0.5f;
            var boardHalfWidth = worldLayout.FrameWidth * 0.5f;
            _boardBoundsProvider.SetBounds(boardCenter.x, boardTopWorldY, boardHalfWidth, _cellSize);
            ApplyTopBarLayout("tile layout changed");
        }

        private void ApplyLiveLayout()
        {
            var worldLayout = ComputeGameplayWorldLayout();
            LogWorldFit("full layout changed", worldLayout);
            ApplyBattleWorldFitScale(worldLayout.FitScale);
            _cellSize = worldLayout.TileCellSize;
            var boardCenter = ComputeBoardCenter(worldLayout.WorldRect, worldLayout.FrameHeight);
            _battleWorldLayout.SetBoardWorldCenter(boardCenter);

            _gridManager.SetCellSize(_cellSize);

            var newOrigin = ComputeGridOrigin(boardCenter, _cellSize);
            _gridManager.SetOrigin(newOrigin);
            _gridManager.RepositionAllTiles();

            _battleWorldLayout.BoardView.Setup(worldLayout.FrameWidth, worldLayout.FrameHeight, _cellSize, _boardConfig.MaskTopPadding);
            _pool.UpdateScale(_cellSize, _boardConfig.TileFillPercent);

            var boardTopWorldY = boardCenter.y + worldLayout.FrameHeight * 0.5f;
            var boardHalfWidth = worldLayout.FrameWidth * 0.5f;
            _boardBoundsProvider.SetBounds(boardCenter.x, boardTopWorldY, boardHalfWidth, _cellSize);

            _battleWorldLayout?.SetVerticalLayout(
                boardTopWorldY,
                worldLayout.FrameCellSize,
                _battleWorldLayoutConfig.GapBoardToPlayerEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapEnemyEnergyToBattleField * worldLayout.GapScale);
            _battleWorldLayout?.RefreshBindings();
            _battleWorldLayout?.PublishAnnouncementAnchors(_boardBoundsProvider);
            ApplyTopBarLayout("full layout changed");
        }
#endif

        private GameplayWorldLayout ComputeGameplayWorldLayout()
        {
            var cam = Camera.main;
            var layout = _gameplayScreenLayoutService.Calculate();
            var worldRect = _gameplayScreenLayoutService.ToWorldRect(cam, layout.WorldRect);
            var fixedHeight = GetBattleWorldBaseFixedHeight();
            var gapCellUnits = GetBattleWorldGapCellUnits();
            
            return GameplayWorldLayoutCalculator.Calculate(
                ToScreenLayoutRect(worldRect),
                _boardConfig.MaxAspectRatio,
                _boardConfig.FramePaddingPercent,
                _boardConfig.TilePaddingPercent,
                _gridConfig.Width,
                _gridConfig.Height,
                _boardConfig.FrameExtraHeight,
                fixedHeight,
                gapCellUnits,
                _gameplayScreenLayoutConfig.WorldStackMinGapScale,
                MinLayoutCellSize);
        }

        private void ApplyBattleWorldFitScale(float fitScale)
        {
            _battleWorldLayout?.EnergyView?.SetLayoutScale(fitScale);
            _battleFieldView?.SetLayoutScale(fitScale);
        }

        private float GetBattleWorldBaseFixedHeight()
        {
            var playerEnergyHeight = _battleWorldLayout.EnergyView ? _battleWorldLayout.EnergyView.PlayerEnergyBaseHeight : 0f;
            var enemyEnergyHeight = _battleWorldLayout.EnergyView ? _battleWorldLayout.EnergyView.EnemyEnergyBaseHeight : 0f;
            var battleFieldHeight = _battleFieldView ? _battleFieldView.BaseLayoutHeight : 0f;
            
            return playerEnergyHeight + enemyEnergyHeight + battleFieldHeight;
        }

        private float GetBattleWorldGapCellUnits()
        {
            return _battleWorldLayoutConfig.GapBoardToPlayerEnergy
                   + _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy
                   + _battleWorldLayoutConfig.GapEnemyEnergyToBattleField;
        }

        private Vector3 ComputeBoardCenter(ScreenLayoutRect worldRect, float frameHeight)
        {
            return new Vector3(worldRect.X + worldRect.Width * 0.5f, worldRect.YMin + frameHeight * 0.5f, 0f);
        }

        private static ScreenLayoutRect ToScreenLayoutRect(Rect rect)
        {
            return new ScreenLayoutRect(rect.x, rect.y, rect.width, rect.height);
        }

        private Vector3 ComputeGridOrigin(Vector3 boardCenter, float cellSize)
        {
            return boardCenter + new Vector3(
                -(_gridConfig.Width - 1) * cellSize * 0.5f,
                -(_gridConfig.Height - 1) * cellSize * 0.5f,
                0f
            );
        }

        private void RefreshPhaseOverlays()
        {
            var showOverlay = ShouldShowBoardOverlay();
            _battleWorldLayout?.BoardView?.SetInteractionOverlayActive(showOverlay);
        }

        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            RefreshPhaseOverlays();
            ApplyBattleFieldLayoutForPhase(e.Phase, true);
        }

        private void ApplyBattleFieldLayoutForCurrentPhase(bool animate)
        {
            if (_battleFlowService is not { IsInitialized: true })
            {
                ApplyBattleFieldLayoutBlend(0f);
                return;
            }

            ApplyBattleFieldLayoutForPhase(_battleFlowService.Snapshot.Phase, animate);
        }

        private void ApplyBattleFieldLayoutForPhase(BattlePhaseKind phase, bool animate)
        {
            var intent = ResolveBattleFieldLayoutIntent(phase);
            if (intent == BattleFieldLayoutIntent.Preserve)
                return;

            var target = intent == BattleFieldLayoutIntent.Full ? 1f : 0f;
            if (animate)
                AnimateBattleFieldLayoutBlend(target);
            else
                ApplyBattleFieldLayoutBlend(target);
        }

        private static BattleFieldLayoutIntent ResolveBattleFieldLayoutIntent(BattlePhaseKind phase)
        {
            switch (phase)
            {
                case BattlePhaseKind.Hero:
                    return BattleFieldLayoutIntent.Full;
                case BattlePhaseKind.PendingBurndown:
                case BattlePhaseKind.Finished:
                    return BattleFieldLayoutIntent.Preserve;
                default:
                    return BattleFieldLayoutIntent.Compressed;
            }
        }

        private void AnimateBattleFieldLayoutBlend(float target)
        {
            if (!_battleFieldView || !_battleFieldLayoutConfig)
                return;

            _battleFieldLayoutTween?.Kill();
            var current = GetCurrentBattleFieldLayoutBlend();
            if (Mathf.Approximately(current, target) || _battleFieldLayoutConfig.TransitionDuration <= 0f)
            {
                ApplyBattleFieldLayoutBlend(target);
                return;
            }

            _battleFieldLayoutTween = DOTween.To(
                    () => current,
                    value =>
                    {
                        current = value;
                        ApplyBattleFieldLayoutBlend(value);
                    },
                    target,
                    _battleFieldLayoutConfig.TransitionDuration)
                .SetEase(_battleFieldLayoutConfig.TransitionEase);
        }

        private float GetCurrentBattleFieldLayoutBlend()
        {
            if (!_battleWorldLayout)
                return 0f;

            var fullOffset = CalculateHeroPhaseBoardOffset();
            if (Mathf.Approximately(fullOffset, 0f))
                return 0f;

            return Mathf.Clamp01(GetBoardPreviewYOffset() / fullOffset);
        }

        private float GetBoardPreviewYOffset()
        {
            if (!_battleWorldLayout)
                return 0f;

            return _battleWorldLayout.GetBoardAndEnergyPreviewYOffset();
        }

        private void ApplyBattleFieldLayoutBlend(float blend)
        {
            if (!_battleFieldView || !_battleFieldLayoutConfig)
                return;

            blend = Mathf.Clamp01(blend);
            _battleFieldView.ApplyLayoutBlendPreservingTop(
                _battleFieldLayoutConfig.CompressedProfile,
                _battleFieldLayoutConfig.FullProfile,
                blend);

            var boardOffset = CalculateHeroPhaseBoardOffset() * blend;
            _battleWorldLayout?.SetBoardAndEnergyPreviewYOffset(boardOffset);
            _battleWorldLayout?.PublishAnnouncementAnchors(_boardBoundsProvider);
            ApplyTopBarLayout("battlefield layout phase changed");
        }

        private float CalculateHeroPhaseBoardOffset()
        {
            if (!_battleWorldLayout)
                return 0f;

            var heightDelta = Mathf.Max(0f,
                _battleFieldLayoutConfig.FullProfile.LayoutHeight - _battleFieldLayoutConfig.CompressedProfile.LayoutHeight);
            var extraOffset = _battleWorldLayout.GetBoardWorldHeight() * _battleFieldLayoutConfig.HeroPhaseBoardOffsetFrameHeight;
            
            return -(heightDelta * _battleFieldView.LayoutScale + extraOffset);
        }

        private bool ShouldShowBoardOverlay()
        {
            if (_gameStateService.State.CurrentValue != GameState.Playing)
                return false;

            if (_battleFlowService is { IsInitialized: true, IsPrePhase: true })
            {
                if (_battleFlowConfig.DimCurrentPhaseDuringPrePhase)
                    return true;

                return _battleFlowService.Snapshot.UpcomingPhase == BattlePhaseKind.Hero;
            }

            return false == _boardRuntimeService.CanAcceptInput;
        }
    }
}