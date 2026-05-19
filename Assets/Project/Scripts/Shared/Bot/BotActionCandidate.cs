using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Bot
{
    public readonly struct BotActionCandidate
    {
        public UnitDescriptor Source { get; }
        public UnitDescriptor Target { get; }
        public UnitActionType ActionType { get; }
        public DirectActionKind DirectActionKind { get; }
        public int ActionValue { get; }
        public int TargetCurrentHP { get; }
        public int TargetMaxHP { get; }
        public bool TargetIsAlive { get; }
        public bool TargetIsExposed { get; }
        public bool WouldBreakDefense { get; }


        public BotActionCandidate(UnitDescriptor source, UnitDescriptor target, UnitActionType actionType,
            DirectActionKind directActionKind, int actionValue, int targetCurrentHP, int targetMaxHP,
            bool targetIsAlive, bool targetIsExposed, bool wouldBreakDefense)
        {
            Source = source;
            Target = target;
            ActionType = actionType;
            DirectActionKind = directActionKind;
            ActionValue = actionValue < 0 ? 0 : actionValue;
            TargetCurrentHP = targetCurrentHP < 0 ? 0 : targetCurrentHP;
            TargetMaxHP = targetMaxHP < 0 ? 0 : targetMaxHP;
            TargetIsAlive = targetIsAlive;
            TargetIsExposed = targetIsExposed;
            WouldBreakDefense = wouldBreakDefense;
        }
    }
}