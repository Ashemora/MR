using System;
using Project.Scripts.Configs.UI;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Moves;

namespace Project.Scripts.Services.Combat.Moves
{
    public class MoveBarService : IMoveBarService, IDisposable
    {
        public bool IsEnabled => _config.IsEnabled;
        public bool HasMoves => !_config.IsEnabled || _engine.Snapshot.CurrentMoves > 0;
        
        
        private readonly MoveBarConfig _config;
        private readonly EventBus _eventBus;
        private readonly MoveBarEngine _engine;


        public MoveBarService(MoveBarConfig config, EventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
            _engine = new MoveBarEngine();
        }


        public MoveBarSnapshot GetSnapshot() => _engine.Snapshot;

        public void Initialize()
        {
            if (false == _config.IsEnabled)
                return;

            _engine.Initialize(_config.ToSettings());
            PublishSnapshot();
        }

        public void Tick(float deltaTime)
        {
            if (false == _config.IsEnabled)
                return;

            _engine.Tick(deltaTime);
            PublishSnapshot();
        }

        public bool TryConsume()
        {
            if (false == _config.IsEnabled)
                return true;

            if (false == _engine.TryConsume())
                return false;

            PublishSnapshot();
            return true;
        }

        public void Dispose() { }


        private void PublishSnapshot()
        {
            var snapshot = _engine.Snapshot;
            _eventBus.Publish(new MoveBarChangedEvent(snapshot.CurrentMoves, snapshot.FillProgress, snapshot.IsAtMax));
        }
    }
}