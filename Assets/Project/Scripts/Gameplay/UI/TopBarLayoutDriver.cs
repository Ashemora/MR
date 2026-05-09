using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Gameplay.Battle.Layout;
using Project.Scripts.Services.Layout;
using Project.Scripts.Services.UISystem;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI
{
    public class TopBarLayoutDriver : IDisposable
    {
        private readonly UIService _uiService;
        private readonly UIConfig _uiConfig;
        private readonly IGameplayScreenLayoutService _gameplayScreenLayoutService;
        private readonly GameplayScreenLayoutConfig _gameplayScreenLayoutConfig;
        private readonly BattleFieldView _battleFieldView;
        private TopBarView _topBarView;
        private GameplayWorldLayoutController _worldLayout;
        private BattleFieldPhaseLayoutController _phaseLayout;


        public TopBarLayoutDriver(UIService uiService, UIConfig uiConfig, IGameplayScreenLayoutService gameplayScreenLayoutService,
            GameplayScreenLayoutConfig gameplayScreenLayoutConfig, BattleFieldView battleFieldView)
        {
            _uiService = uiService;
            _uiConfig = uiConfig;
            _gameplayScreenLayoutService = gameplayScreenLayoutService;
            _gameplayScreenLayoutConfig = gameplayScreenLayoutConfig;
            _battleFieldView = battleFieldView;
        }


        public void RegisterView()
        {
            _uiService.RegisterView<TopBarView>(_uiConfig.TopBarViewPrefab, UILayer.Main);
        }

        public void Subscribe(GameplayWorldLayoutController worldLayout, BattleFieldPhaseLayoutController phaseLayout)
        {
            _worldLayout = worldLayout;
            _phaseLayout = phaseLayout;
            if (null != _worldLayout)
                _worldLayout.LayoutApplied += OnWorldLayoutApplied;

            if (null != _phaseLayout)
                _phaseLayout.LayoutBlendApplied += OnPhaseLayoutBlendApplied;
        }

        public async UniTask ShowAsync(BattleFieldViewModel battleFieldViewModel)
        {
            _topBarView = await _uiService.Show<TopBarView, BattleFieldViewModel>(battleFieldViewModel);
            _topBarView.gameObject.SetActive(false);
        }

        public async UniTask ApplyLayoutWhenReadyAsync()
        {
            if (!_topBarView)
                return;

            _topBarView.gameObject.SetActive(false);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (ApplyLayout("startup after screen settled"))
                _topBarView.gameObject.SetActive(true);
            else
                Debug.LogWarning("TopBar layout was not ready; keeping TopBar hidden to avoid showing prefab position.");
        }

        public void Dispose()
        {
            if (null != _worldLayout)
                _worldLayout.LayoutApplied -= OnWorldLayoutApplied;

            if (null != _phaseLayout)
                _phaseLayout.LayoutBlendApplied -= OnPhaseLayoutBlendApplied;

            _worldLayout = null;
            _phaseLayout = null;

            _uiService?.Close<TopBarView>();
            _topBarView = null;
        }


        private void OnWorldLayoutApplied(string reason)
        {
            ApplyLayout(reason);
        }

        private void OnPhaseLayoutBlendApplied()
        {
            ApplyLayout("battlefield layout phase changed");
        }

        private bool ApplyLayout(string reason)
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
    }
}