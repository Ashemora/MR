namespace Project.Scripts.Shared.BattleFlow
{
    public class BattleFlowEngine
    {
        public BattleFlowSnapshot Snapshot => _snapshot;


        private BattleFlowSettings _settings;
        private BattleFlowSnapshot _snapshot;
        private bool _initialized;


        public void Initialize(BattleFlowSettings settings)
        {
            _settings = settings;
            _initialized = true;

            _snapshot = settings.PrePhaseDuration > 0 && settings.EnablePrePhaseOnBattleStart
                ? MakePrePhase(1, BattlePhaseKind.Match)
                : MakeActionPhase(1, BattlePhaseKind.Match);
        }

        public bool Tick(float deltaTime)
        {
            if (false == _initialized || deltaTime <= 0f || _snapshot.IsTerminal)
                return false;

            if (_snapshot.Phase == BattlePhaseKind.PendingHero)
                return false;

            var nextTimeRemaining = _snapshot.TimeRemaining - deltaTime;
            if (nextTimeRemaining > 0f)
            {
                _snapshot = _snapshot.WithTimeRemaining(nextTimeRemaining);
                return false;
            }

            AdvancePhase();
            
            return true;
        }

        public bool BeginHeroPhase()
        {
            if (false == _initialized || _snapshot.Phase != BattlePhaseKind.PendingHero)
                return false;

            _snapshot = _settings.PrePhaseDuration > 0
                ? MakePrePhase(_snapshot.CurrentRound, BattlePhaseKind.Hero)
                : MakeActionPhase(_snapshot.CurrentRound, BattlePhaseKind.Hero);
            
            return true;
        }

        public void MarkFinished()
        {
            if (false == _initialized)
                return;

            _snapshot = new BattleFlowSnapshot(_snapshot.CurrentRound, _snapshot.TotalRounds,
                BattlePhaseKind.Finished, 0f, _snapshot.EnergyCarryoverMode);
        }


        private void AdvancePhase()
        {
            if (_snapshot.Phase == BattlePhaseKind.PrePhase)
            {
                CompletePrePhase();
                
                return;
            }

            if (_snapshot.Phase == BattlePhaseKind.Match)
            {
                _snapshot = new BattleFlowSnapshot(_snapshot.CurrentRound, _snapshot.TotalRounds,
                    BattlePhaseKind.PendingHero, 0f, _snapshot.EnergyCarryoverMode);
                
                return;
            }

            if (_snapshot.Phase == BattlePhaseKind.Hero && _snapshot.CurrentRound < _snapshot.TotalRounds)
            {
                var nextRound = _snapshot.CurrentRound + 1;
                _snapshot = _settings.PrePhaseDuration > 0
                    ? MakePrePhase(nextRound, BattlePhaseKind.Match)
                    : MakeActionPhase(nextRound, BattlePhaseKind.Match);
                
                return;
            }

            _snapshot = new BattleFlowSnapshot(_snapshot.CurrentRound, _snapshot.TotalRounds,
                BattlePhaseKind.PendingBurndown, 0f, _snapshot.EnergyCarryoverMode);
        }

        private BattleFlowSnapshot MakePrePhase(int round, BattlePhaseKind upcomingPhase)
        {
            return new BattleFlowSnapshot(round, _settings.RoundCount, BattlePhaseKind.PrePhase,
                _settings.PrePhaseDuration, _settings.EnergyCarryoverMode, upcomingPhase);
        }

        private BattleFlowSnapshot MakeActionPhase(int round, BattlePhaseKind phase)
        {
            var duration = phase == BattlePhaseKind.Hero
                ? _settings.HeroPhaseDuration
                : _settings.MatchPhaseDuration;

            return new BattleFlowSnapshot(round, _settings.RoundCount, phase, duration,
                _settings.EnergyCarryoverMode);
        }

        private void CompletePrePhase()
        {
            var upcomingPhase = _snapshot.UpcomingPhase ?? BattlePhaseKind.Match;
            _snapshot = MakeActionPhase(_snapshot.CurrentRound, upcomingPhase);
        }
    }
}