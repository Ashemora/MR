using System;
using VContainer.Unity;

namespace Project.Scripts.Services.Clock
{
    public class BattleClockService : IBattleClock, ITickable
    {
        private const int DefaultTickRate = 30;


        public long CurrentTick { get; private set; }
        public int TickRate => DefaultTickRate;


        private double _accumulator;


        public void Tick()
        {
            Advance(UnityEngine.Time.deltaTime);
        }

        public int SecondsToTicks(float seconds)
        {
            if (seconds <= 0f)
                return 0;

            return (int)Math.Ceiling(seconds * TickRate);
        }

        private void Advance(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _accumulator += deltaTime;
            var tickDuration = 1d / TickRate;
            while (_accumulator >= tickDuration)
            {
                CurrentTick++;
                _accumulator -= tickDuration;
            }
        }
    }
}