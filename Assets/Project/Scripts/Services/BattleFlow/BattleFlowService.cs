using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleFlow;

namespace Project.Scripts.Services.BattleFlow
{
    public class BattleFlowService : IBattleFlowService
    {
        public bool IsInitialized { get; private set; }
        public bool IsPrePhase => IsInitialized && _engine.Snapshot.Phase == BattlePhaseKind.PrePhase;
        public BattleFlowSnapshot Snapshot => _engine.Snapshot;


        private readonly BattleFlowConfig _config;
        private readonly EventBus _eventBus;
        private readonly BattleFlowEngine _engine = new BattleFlowEngine();
        private int _lastPublishedSecond;


        public BattleFlowService(BattleFlowConfig config, EventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }


        public void Initialize()
        {
            var settings = new BattleFlowSettings(
                _config.RoundCount,
                _config.MatchPhaseDuration,
                _config.HeroPhaseDuration,
                _config.PrePhaseDuration,
                _config.EnablePrePhaseOnBattleStart,
                _config.CountdownThreshold,
                _config.EnergyCarryoverMode,
                _config.MinUnitActivationCooldownSeconds);

            _engine.Initialize(settings);
            IsInitialized = true;
            _lastPublishedSecond = (int)_engine.Snapshot.TimeRemaining;

            PublishRoundChanged(_engine.Snapshot);
            PublishPhaseChanged(_engine.Snapshot);
            PublishTimerChanged(_engine.Snapshot);

            if (_engine.Snapshot.Phase == BattlePhaseKind.PrePhase)
                _eventBus.Publish(new BattlePrePhaseStartedEvent(_engine.Snapshot.UpcomingPhase ?? BattlePhaseKind.Match));
        }

        public void Tick(float deltaTime)
        {
            if (false == IsInitialized)
                return;

            var before = _engine.Snapshot;
            _engine.Tick(deltaTime);
            var after = _engine.Snapshot;

            if (after.CurrentRound != before.CurrentRound)
                PublishRoundChanged(after);

            if (after.Phase != before.Phase)
            {
                _lastPublishedSecond = (int)after.TimeRemaining;
                PublishPrePhaseLifecycle(before, after);
                PublishPhaseChanged(after);
                PublishTimerChanged(after);
                TryPublishCountdown(after);
                
                return;
            }

            var currentSecond = (int)after.TimeRemaining;
            if (currentSecond < _lastPublishedSecond)
            {
                _lastPublishedSecond = currentSecond;
                PublishTimerChanged(after);
                TryPublishCountdown(after);
            }
        }

        public void BeginHeroPhase()
        {
            if (false == IsInitialized)
                return;

            var before = _engine.Snapshot;
            if (false == _engine.BeginHeroPhase())
                return;

            var after = _engine.Snapshot;
            if (after.Phase == before.Phase)
                return;

            _lastPublishedSecond = (int)after.TimeRemaining;
            PublishPrePhaseLifecycle(before, after);
            PublishPhaseChanged(after);
            PublishTimerChanged(after);
            TryPublishCountdown(after);
        }

        public void MarkFinished()
        {
            if (false == IsInitialized)
                return;

            var before = _engine.Snapshot;
            _engine.MarkFinished();
            var after = _engine.Snapshot;

            if (after.Phase != before.Phase)
            {
                PublishPrePhaseLifecycle(before, after);
                PublishPhaseChanged(after);
                PublishTimerChanged(after);
            }
        }


        private void PublishRoundChanged(BattleFlowSnapshot snapshot)
        {
            _eventBus.Publish(new BattleFlowRoundChangedEvent(snapshot.CurrentRound, snapshot.TotalRounds));
        }

        private void PublishPhaseChanged(BattleFlowSnapshot snapshot)
        {
            _eventBus.Publish(new BattleFlowPhaseChangedEvent(
                snapshot.Phase,
                snapshot.CurrentRound,
                snapshot.TotalRounds,
                snapshot.EnergyCarryoverMode));
        }

        private void PublishTimerChanged(BattleFlowSnapshot snapshot)
        {
            _eventBus.Publish(new BattleFlowTimerChangedEvent(snapshot.Phase, snapshot.TimeRemaining));
        }

        private void TryPublishCountdown(BattleFlowSnapshot snapshot)
        {
            if (snapshot.Phase == BattlePhaseKind.PrePhase)
                return;

            var remainingSeconds = (int)snapshot.TimeRemaining;
            if (remainingSeconds <= 0 || remainingSeconds > _config.CountdownThreshold)
                return;

            _eventBus.Publish(new BattleFlowCountdownTickEvent(snapshot.Phase, remainingSeconds));
        }

        private void PublishPrePhaseLifecycle(BattleFlowSnapshot before, BattleFlowSnapshot after)
        {
            if (after.Phase == BattlePhaseKind.PrePhase && before.Phase != BattlePhaseKind.PrePhase)
            {
                _eventBus.Publish(new BattlePrePhaseStartedEvent(after.UpcomingPhase ?? BattlePhaseKind.Match));
                return;
            }

            if (before.Phase == BattlePhaseKind.PrePhase && after.Phase != BattlePhaseKind.PrePhase)
                _eventBus.Publish(new BattlePrePhaseEndedEvent(after.Phase));
        }
    }
}