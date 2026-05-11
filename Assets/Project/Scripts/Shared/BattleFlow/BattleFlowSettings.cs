namespace Project.Scripts.Shared.BattleFlow
{
    public readonly struct BattleFlowSettings
    {
        public int RoundCount { get; }
        public float MatchPhaseDuration { get; }
        public float HeroPhaseDuration { get; }
        public int PrePhaseDuration { get; }
        public bool EnablePrePhaseOnBattleStart { get; }
        public int CountdownThreshold { get; }
        public EnergyCarryoverMode EnergyCarryoverMode { get; }
        public float MinUnitActivationCooldownSeconds { get; }


        public BattleFlowSettings(int roundCount, float matchPhaseDuration, float heroPhaseDuration,
            int prePhaseDuration, bool enablePrePhaseOnBattleStart, int countdownThreshold,
            EnergyCarryoverMode energyCarryoverMode, float minUnitActivationCooldownSeconds)
        {
            RoundCount = roundCount < 1 ? 1 : roundCount;
            MatchPhaseDuration = matchPhaseDuration < 0f ? 0f : matchPhaseDuration;
            HeroPhaseDuration = heroPhaseDuration < 0f ? 0f : heroPhaseDuration;
            PrePhaseDuration = prePhaseDuration < 0 ? 0 : prePhaseDuration;
            EnablePrePhaseOnBattleStart = enablePrePhaseOnBattleStart;
            CountdownThreshold = countdownThreshold < 0 ? 0 : countdownThreshold;
            EnergyCarryoverMode = energyCarryoverMode;
            MinUnitActivationCooldownSeconds = minUnitActivationCooldownSeconds < 0f
                ? 0f
                : minUnitActivationCooldownSeconds;
        }
    }
}