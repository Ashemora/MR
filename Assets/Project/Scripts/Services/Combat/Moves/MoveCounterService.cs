using System;
using Project.Scripts.Services.Events;

namespace Project.Scripts.Services.Combat.Moves
{
    public class MoveCounterService : IMoveCounterService, IDisposable
    {
        public int MovesUsed { get; private set; }


        private readonly EventBus _eventBus;
        private IDisposable _subscription;


        public MoveCounterService(EventBus eventBus)
        {
            _eventBus = eventBus;
            _subscription = _eventBus.Subscribe<MoveUsedEvent>(OnMoveUsed);
        }


        private void OnMoveUsed(MoveUsedEvent _)
        {
            MovesUsed++;
            _eventBus.Publish(new MoveCountChangedEvent(MovesUsed));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}