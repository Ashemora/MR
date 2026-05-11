using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Targeting
{
    public readonly struct UnitTargetCandidate
    {
        public UnitDescriptor Descriptor { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAvailable { get; }


        public UnitTargetCandidate(UnitDescriptor descriptor, int currentHP, int maxHP, bool isAvailable)
        {
            Descriptor = descriptor;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            IsAvailable = isAvailable;
        }
    }
}