using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Gameplay.Layout;
using Project.Scripts.Services.SafeArea;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Gameplay.Battle.Board
{
    [ExecuteAlways]
    public class BoardPositioner : MonoBehaviour
    {
        private const float MinLayoutCellSize = 0.01f;
        
        
        [SerializeField] private BoardConfig _boardConfig;
        [SerializeField] private BattleWorldLayoutConfig _battleWorldLayoutConfig;
        [SerializeField] private GridConfig _gridConfig;
        [SerializeField] private GameplayScreenLayoutConfig _screenLayoutConfig;


        private ISafeAreaService _safeAreaService;


        [Inject]
        public void Construct(ISafeAreaService safeAreaService)
        {
            _safeAreaService = safeAreaService;
        }


        private void Update()
        {
            if (Application.isPlaying)
                return;

            Apply();
        }

        public void Apply(float tileCellSize = -1f)
        {
            var cam = Camera.main;
            if (!cam || !_boardConfig || !_battleWorldLayoutConfig || !_gridConfig || !_screenLayoutConfig)
                return;

            var layout = GetComponentInParent<BattleWorldLayout>();
            var worldLayout = ComputeBattleWorldLayout(cam, layout);

            if (tileCellSize < 0f)
                tileCellSize = worldLayout.TileCellSize;

            var boardView = GetComponent<BoardView>();
            if (boardView)
                boardView.Setup(worldLayout.FrameWidth, worldLayout.FrameHeight, tileCellSize, _boardConfig.MaskTopPadding);

            var boardCenter = ComputeBoardCenter(worldLayout.WorldRect, worldLayout.FrameHeight);
            if (layout)
            {
                layout.EnergyView?.SetLayoutScale(worldLayout.FitScale);
                layout.BattleFieldView?.SetLayoutScale(worldLayout.FitScale);
                layout.SetBoardWorldCenter(boardCenter);

                var boardTopWorldY = boardCenter.y + worldLayout.FrameHeight * 0.5f;
                layout.SetVerticalLayout(
                    boardTopWorldY,
                    worldLayout.FrameCellSize,
                    _battleWorldLayoutConfig.GapBoardToPlayerEnergy * worldLayout.GapScale,
                    _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy * worldLayout.GapScale,
                    _battleWorldLayoutConfig.GapEnemyEnergyToBattleField * worldLayout.GapScale);
                layout.RefreshBindings();
                return;
            }

            transform.position = boardCenter;
        }


        private GameplayWorldLayout ComputeBattleWorldLayout(Camera cam, BattleWorldLayout layout)
        {
            var gameplayScreenLayout = CalculateScreenLayout();
            var worldRect = ToScreenLayoutRect(ToWorldRect(cam, gameplayScreenLayout.WorldRect));
            var fixedHeight = GetBattleWorldFixedHeight(layout);
            var gapCellUnits = GetBattleWorldGapCellUnits();
            
            return GameplayWorldLayoutCalculator.Calculate(
                worldRect,
                _boardConfig.MaxAspectRatio,
                _boardConfig.FramePaddingPercent,
                _boardConfig.TilePaddingPercent,
                _gridConfig.Width,
                _gridConfig.Height,
                _boardConfig.FrameExtraHeight,
                fixedHeight,
                gapCellUnits,
                _screenLayoutConfig.WorldStackMinGapScale,
                MinLayoutCellSize);
        }

        private GameplayScreenLayout CalculateScreenLayout()
        {
            var safeArea = GetSafeAreaInfo();
            var screenRect = new ScreenLayoutRect(0f, 0f, safeArea.ScreenSize.x, safeArea.ScreenSize.y);
            var safeAreaRect = safeArea.Raw.width > 0f && safeArea.Raw.height > 0f
                ? new ScreenLayoutRect(safeArea.Raw.x, safeArea.Raw.y, safeArea.Raw.width, safeArea.Raw.height)
                : screenRect;

            return GameplayScreenLayoutCalculator.Calculate(
                screenRect,
                safeAreaRect,
                _screenLayoutConfig.UseSafeArea,
                _screenLayoutConfig.WorldExtendsIntoUnsafeBottomArea,
                _screenLayoutConfig.SafeAreaPadding,
                _screenLayoutConfig.GameplayAspect,
                _screenLayoutConfig.ReferenceResolutionWidth,
                _screenLayoutConfig.ReferenceResolutionHeight,
                _screenLayoutConfig.TopBarHeight,
                _screenLayoutConfig.TopBarSidePadding,
                _screenLayoutConfig.TopBarBottomPadding,
                _screenLayoutConfig.WorldBottomPadding,
                _screenLayoutConfig.WorldSidePadding);
        }

        private SafeAreaInfo GetSafeAreaInfo()
        {
            if (Application.isPlaying && null != _safeAreaService && null != _safeAreaService.Current)
                return _safeAreaService.Current.CurrentValue;

            var width = Screen.width > 0 ? Screen.width : UnityEngine.Device.Screen.width;
            var height = Screen.height > 0 ? Screen.height : UnityEngine.Device.Screen.height;
            var screenSize = new Vector2(width, height);
            
            return new SafeAreaInfo(Screen.safeArea, screenSize, Screen.orientation);
        }

        private Rect ToWorldRect(Camera cam, ScreenLayoutRect rect)
        {
            var min = cam.ScreenToWorldPoint(new Vector3(rect.XMin, rect.YMin, -cam.transform.position.z));
            var max = cam.ScreenToWorldPoint(new Vector3(rect.XMax, rect.YMax, -cam.transform.position.z));
            
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static ScreenLayoutRect ToScreenLayoutRect(Rect rect)
        {
            return new ScreenLayoutRect(rect.x, rect.y, rect.width, rect.height);
        }

        private float GetBattleWorldFixedHeight(BattleWorldLayout layout)
        {
            if (!layout)
                return 0f;

            var playerEnergyHeight = layout.EnergyView ? layout.EnergyView.PlayerEnergyBaseHeight : 0f;
            var enemyEnergyHeight = layout.EnergyView ? layout.EnergyView.EnemyEnergyBaseHeight : 0f;
            var battleFieldHeight = layout.BattleFieldView ? layout.BattleFieldView.BaseLayoutHeight : 0f;
            
            return playerEnergyHeight + enemyEnergyHeight + battleFieldHeight;
        }

        private float GetBattleWorldGapCellUnits()
        {
            return _battleWorldLayoutConfig.GapBoardToPlayerEnergy
                   + _battleWorldLayoutConfig.GapPlayerEnergyToEnemyEnergy
                   + _battleWorldLayoutConfig.GapEnemyEnergyToBattleField;
        }

        private static Vector3 ComputeBoardCenter(ScreenLayoutRect worldRect, float frameHeight)
        {
            return new Vector3(worldRect.X + worldRect.Width * 0.5f, worldRect.YMin + frameHeight * 0.5f, 0f);
        }
    }
}