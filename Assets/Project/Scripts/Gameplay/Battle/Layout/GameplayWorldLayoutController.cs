using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Grid;
using Project.Scripts.Services.Layout;
using Project.Scripts.Shared.Layout;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Layout
{
    public class GameplayWorldLayoutController : IDisposable
    {
        private const float MinLayoutCellSize = 0.01f;
        
        
        public event Action<string> LayoutApplied;
        
        
        public struct InitialWorldLayout
        {
            public float TileCellSize;
            public float FrameWidth;
            public float FrameHeight;
            public Vector3 BoardCenter;
            public Vector3 GridOrigin;
        }


        private readonly BattleWorldLayout _battleWorldLayout;
        private readonly BoardConfig _boardConfig;
        private readonly GridConfig _gridConfig;
        private readonly BattleWorldLayoutConfig _battleWorldLayoutConfig;
        private readonly GameplayScreenLayoutConfig _gameplayScreenLayoutConfig;
        private readonly IGameplayScreenLayoutService _gameplayScreenLayoutService;
        private readonly IBoardBoundsProvider _boardBoundsProvider;
        private readonly BattleFieldView _battleFieldView;
        private GridManager _gridManager;
        private TilePool _pool;
        private float _cellSize;
#if UNITY_EDITOR
        private int _lastWidth;
        private int _lastHeight;
        private Rect _lastSafeArea;
        private int _delayedTopBarLayoutVersion;
        private bool _editorEventsSubscribed;
#endif


        public GameplayWorldLayoutController(
            BattleWorldLayout battleWorldLayout,
            BoardConfig boardConfig,
            GridConfig gridConfig,
            BattleWorldLayoutConfig battleWorldLayoutConfig,
            GameplayScreenLayoutConfig gameplayScreenLayoutConfig,
            IGameplayScreenLayoutService gameplayScreenLayoutService,
            IBoardBoundsProvider boardBoundsProvider,
            BattleFieldView battleFieldView)
        {
            _battleWorldLayout = battleWorldLayout;
            _boardConfig = boardConfig;
            _gridConfig = gridConfig;
            _battleWorldLayoutConfig = battleWorldLayoutConfig;
            _gameplayScreenLayoutConfig = gameplayScreenLayoutConfig;
            _gameplayScreenLayoutService = gameplayScreenLayoutService;
            _boardBoundsProvider = boardBoundsProvider;
            _battleFieldView = battleFieldView;
        }


        public InitialWorldLayout Initialize()
        {
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

            _cellSize = worldLayout.TileCellSize;

            return new InitialWorldLayout
            {
                TileCellSize = worldLayout.TileCellSize,
                FrameWidth = worldLayout.FrameWidth,
                FrameHeight = worldLayout.FrameHeight,
                BoardCenter = boardCenter,
                GridOrigin = ComputeGridOrigin(boardCenter, worldLayout.TileCellSize)
            };
        }

        public void BindBoardSystems(GridManager gridManager, TilePool pool)
        {
            _gridManager = gridManager;
            _pool = pool;
#if UNITY_EDITOR
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
            if (false == _editorEventsSubscribed)
            {
                BoardConfig.LayoutChanged += OnLayoutChanged;
                BoardConfig.TileLayoutChanged += OnTileLayoutChanged;
                BattleWorldLayoutConfig.LayoutChanged += OnBattleLayoutChanged;
                GameplayScreenLayoutConfig.LayoutChanged += OnScreenLayoutChanged;
                GameplayScreenLayoutConfig.TopBarLayoutChanged += OnTopBarLayoutChanged;
                _editorEventsSubscribed = true;
            }
#endif
        }

        public void TickEditorResize()
        {
#if UNITY_EDITOR
            if (null == _gridManager)
                return;

            var safeArea = Screen.safeArea;
            if (Screen.width == _lastWidth && Screen.height == _lastHeight && safeArea == _lastSafeArea)
                return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = safeArea;
            ApplyLiveResize();
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (_editorEventsSubscribed)
            {
                BoardConfig.LayoutChanged -= OnLayoutChanged;
                BoardConfig.TileLayoutChanged -= OnTileLayoutChanged;
                BattleWorldLayoutConfig.LayoutChanged -= OnBattleLayoutChanged;
                GameplayScreenLayoutConfig.LayoutChanged -= OnScreenLayoutChanged;
                GameplayScreenLayoutConfig.TopBarLayoutChanged -= OnTopBarLayoutChanged;
                _editorEventsSubscribed = false;
            }
#endif
        }


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

#if UNITY_EDITOR
        private void OnLayoutChanged()
        {
            ApplyLiveLayout();
        }

        private void OnTileLayoutChanged()
        {
            ApplyLiveTileLayout();
        }

        private void OnBattleLayoutChanged()
        {
            ApplyLiveLayout();
        }

        private void OnScreenLayoutChanged()
        {
            ApplyLiveLayout();
        }

        private void OnTopBarLayoutChanged()
        {
            LayoutApplied?.Invoke("topbar config changed");
        }

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

            LayoutApplied?.Invoke("delayed resize");
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
            LayoutApplied?.Invoke("tile layout changed");
        }

        private void ApplyLiveLayout()
        {
            var worldLayout = ComputeGameplayWorldLayout();
            LogWorldFit("full layout changed", worldLayout);
            ApplyBattleWorldFitScale(worldLayout.FitScale);
            _cellSize = worldLayout.TileCellSize;
            var boardCenter = ComputeBoardCenter(worldLayout.WorldRect, worldLayout.FrameHeight);
            _battleWorldLayout.SetBoardWorldCenter(boardCenter);

            _gridManager?.SetCellSize(_cellSize);

            var newOrigin = ComputeGridOrigin(boardCenter, _cellSize);
            _gridManager?.SetOrigin(newOrigin);
            _gridManager?.RepositionAllTiles();

            _battleWorldLayout.BoardView.Setup(worldLayout.FrameWidth, worldLayout.FrameHeight, _cellSize, _boardConfig.MaskTopPadding);
            _pool?.UpdateScale(_cellSize, _boardConfig.TileFillPercent);

            var boardTopWorldY = boardCenter.y + worldLayout.FrameHeight * 0.5f;
            var boardHalfWidth = worldLayout.FrameWidth * 0.5f;
            _boardBoundsProvider.SetBounds(boardCenter.x, boardTopWorldY, boardHalfWidth, _cellSize);

            _battleWorldLayout.SetVerticalLayout(
                boardTopWorldY,
                worldLayout.FrameCellSize,
                _battleWorldLayoutConfig.GapBoardToPlayerEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy * worldLayout.GapScale,
                _battleWorldLayoutConfig.GapEnemyEnergyToBattleField * worldLayout.GapScale);
            _battleWorldLayout.RefreshBindings();
            _battleWorldLayout.PublishAnnouncementAnchors(_boardBoundsProvider);
            LayoutApplied?.Invoke("full layout changed");
        }
#endif
    }
}