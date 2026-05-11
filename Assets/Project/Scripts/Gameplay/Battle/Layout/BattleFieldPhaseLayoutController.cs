using System;
using DG.Tweening;
using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Gameplay.Battle.HUD;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.BattleFlow;
using R3;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Layout
{
    public class BattleFieldPhaseLayoutController : IDisposable
    {
        public event Action LayoutBlendApplied;
        
        
        private enum BattleFieldLayoutIntent
        {
            Compressed,
            Full,
            Preserve
        }

        private readonly EventBus _eventBus;
        private readonly IGameStateService _gameStateService;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly IBattleFlowService _battleFlowService;
        private readonly BattleFieldLayoutConfig _battleFieldLayoutConfig;
        private readonly BattleFlowConfig _battleFlowConfig;
        private readonly BattleWorldLayout _battleWorldLayout;
        private readonly BattleFieldView _battleFieldView;
        private readonly IBoardBoundsProvider _boardBoundsProvider;
        private IDisposable _boardRuntimeSubscription;
        private IDisposable _gameStateSubscription;
        private IDisposable _battleFlowPhaseSubscription;
        private Tween _battleFieldLayoutTween;


        public BattleFieldPhaseLayoutController(
            EventBus eventBus,
            IGameStateService gameStateService,
            IBoardRuntimeService boardRuntimeService,
            IBattleFlowService battleFlowService,
            BattleFieldLayoutConfig battleFieldLayoutConfig,
            BattleFlowConfig battleFlowConfig,
            BattleWorldLayout battleWorldLayout,
            BattleFieldView battleFieldView,
            IBoardBoundsProvider boardBoundsProvider)
        {
            _eventBus = eventBus;
            _gameStateService = gameStateService;
            _boardRuntimeService = boardRuntimeService;
            _battleFlowService = battleFlowService;
            _battleFieldLayoutConfig = battleFieldLayoutConfig;
            _battleFlowConfig = battleFlowConfig;
            _battleWorldLayout = battleWorldLayout;
            _battleFieldView = battleFieldView;
            _boardBoundsProvider = boardBoundsProvider;
        }


        public void ApplyInitialBlend()
        {
            ApplyBattleFieldLayoutBlend(0f);
        }

        public void Initialize()
        {
            _boardRuntimeSubscription?.Dispose();
            _boardRuntimeSubscription = _boardRuntimeService.State.Subscribe(_ => RefreshPhaseOverlays());
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = _gameStateService.State.Subscribe(_ => RefreshPhaseOverlays());
            _battleFlowPhaseSubscription?.Dispose();
            _battleFlowPhaseSubscription = _eventBus.Subscribe<BattleFlowPhaseChangedEvent>(OnBattleFlowPhaseChanged);

            RefreshPhaseOverlays();
            ApplyBattleFieldLayoutForCurrentPhase(false);
        }

        public void Dispose()
        {
            _battleFieldLayoutTween?.Kill();
            _battleFieldLayoutTween = null;
            _boardRuntimeSubscription?.Dispose();
            _boardRuntimeSubscription = null;
            _gameStateSubscription?.Dispose();
            _gameStateSubscription = null;
            _battleFlowPhaseSubscription?.Dispose();
            _battleFlowPhaseSubscription = null;
        }


        private void OnBattleFlowPhaseChanged(BattleFlowPhaseChangedEvent e)
        {
            RefreshPhaseOverlays();
            ApplyBattleFieldLayoutForPhase(e.Phase, true);
        }

        private void RefreshPhaseOverlays()
        {
            var showOverlay = ShouldShowBoardOverlay();
            _battleWorldLayout?.BoardView?.SetInteractionOverlayActive(showOverlay);
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
            LayoutBlendApplied?.Invoke();
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
    }
}