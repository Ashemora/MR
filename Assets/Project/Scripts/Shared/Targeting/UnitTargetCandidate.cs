using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Targeting
{
    public readonly struct UnitTargetCandidate
    {
        public UnitDescriptor Descriptor { get; }
        public UnitActionType ActionType { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAvailable { get; }
        public bool IsAssigned { get; }


        public UnitTargetCandidate(UnitDescriptor descriptor, UnitActionType actionType, int currentHP, int maxHP,
            bool isAvailable, bool isAssigned = false)
        {
            Descriptor = descriptor;
            ActionType = actionType;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            IsAvailable = isAvailable;
            IsAssigned = isAssigned || isAvailable;
        }
    }
}