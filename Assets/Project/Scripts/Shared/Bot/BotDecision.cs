using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Bot
{
    public readonly struct BotDecision
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public float Score { get; }


        public BotDecision(UnitDescriptor source, UnitDescriptor target, float score)
        {
            Source = source;
            Target = target;
            Score = score;
        }
    }
}