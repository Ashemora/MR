namespace Project.Scripts.Shared.BattleFlow
{
    public readonly struct BattleFlowSnapshot
    {
        public int CurrentRound { get; }
        public int TotalRounds { get; }
        public BattlePhaseKind Phase { get; }
        public float TimeRemaining { get; }
        public EnergyCarryoverMode EnergyCarryoverMode { get; }
        public BattlePhaseKind? UpcomingPhase { get; }
        public bool IsTerminal => Phase == BattlePhaseKind.PendingBurndown || Phase == BattlePhaseKind.Finished;


        public BattleFlowSnapshot(int currentRound, int totalRounds, BattlePhaseKind phase,
            float timeRemaining, EnergyCarryoverMode energyCarryoverMode, BattlePhaseKind? upcomingPhase = null)
        {
            CurrentRound = currentRound;
            TotalRounds = totalRounds;
            Phase = phase;
            TimeRemaining = timeRemaining < 0f ? 0f : timeRemaining;
            EnergyCarryoverMode = energyCarryoverMode;
            UpcomingPhase = upcomingPhase;
        }

        public BattleFlowSnapshot WithTimeRemaining(float timeRemaining)
        {
            return new BattleFlowSnapshot(CurrentRound, TotalRounds, Phase, timeRemaining, EnergyCarryoverMode, UpcomingPhase);
        }
    }
}