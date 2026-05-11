using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.ActivationConditions
{
    public readonly struct ActivationConditionEvent
    {
        public ActivationConditionKind Kind { get; }
        public UnitDescriptor Source { get; }
        public BattleSide Side { get; }
        public TileKind TileKind { get; }
        public float Amount { get; }
        public long OccurredAtTick { get; }


        public ActivationConditionEvent(ActivationConditionKind kind, UnitDescriptor source, float amount = 1f,
            long occurredAtTick = 0)
        {
            Kind = kind;
            Source = source;
            Side = source.Side;
            TileKind = TileKind.None;
            Amount = amount <= 0f ? 0f : amount;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }

        public ActivationConditionEvent(ActivationConditionKind kind, BattleSide side, float amount,
            TileKind tileKind = TileKind.None, long occurredAtTick = 0)
        {
            Kind = kind;
            Source = default;
            Side = side;
            TileKind = tileKind;
            Amount = amount <= 0f ? 0f : amount;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }
}