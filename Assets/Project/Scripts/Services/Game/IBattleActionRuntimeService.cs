using Project.Scripts.Shared.BattleFlow;
using R3;

namespace Project.Scripts.Services.Game
{
    public interface IBattleActionRuntimeService
    {
        ReadOnlyReactiveProperty<BattleActionRuntimeState> State { get; }
        int CurrentVersion { get; }
        bool IsRunning { get; }
        bool IsStoppingForBurndown { get; }
        bool IsBlocked { get; }
        bool CanAcceptNormalActions { get; }
        BattleActionPhase CurrentPhase { get; }
        int CaptureVersion();
        bool IsCurrent(int version);
        BattleActionGateResult Evaluate(BattleActionKind actionKind);
        void ApplyBattleFlowPhase(BattlePhaseKind phase);
        void RequestBurndownStop();
        void MarkBlocked();
    }
}