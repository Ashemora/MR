using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public readonly struct AbilityDirectApplicationResult
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public UnitActionType ActionType { get; }
        public int Value { get; }
        public int ApplicationIndex { get; }
        public bool IsRepeat { get; }
        public long OccurredAtTick { get; }


        public AbilityDirectApplicationResult(UnitDescriptor source, UnitDescriptor target, UnitActionType actionType,
            int value, int applicationIndex, bool isRepeat, long occurredAtTick)
        {
            Source = source;
            Target = target;
            ActionType = actionType;
            Value = value < 0 ? 0 : value;
            ApplicationIndex = applicationIndex < 0 ? 0 : applicationIndex;
            IsRepeat = isRepeat;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }
}