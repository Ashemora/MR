using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Abilities
{
    public readonly struct AbilityTargetCandidate
    {
        public UnitDescriptor Descriptor { get; }
        public UnitActionType ActionType { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAlive { get; }
        public bool IsExposed { get; }


        public bool IsHpFull => CurrentHP >= MaxHP;


        public AbilityTargetCandidate(UnitDescriptor descriptor, UnitActionType actionType, int currentHP, int maxHP,
            bool isAlive, bool isExposed)
        {
            Descriptor = descriptor;
            ActionType = actionType;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            IsAlive = isAlive;
            IsExposed = isExposed;
        }
    }
}