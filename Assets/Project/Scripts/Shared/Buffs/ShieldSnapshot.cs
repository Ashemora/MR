using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Buffs
{
    public readonly struct ShieldSnapshot
    {
        public UnitDescriptor Target { get; }
        public int Current { get; }
        public int Capacity { get; }
        public bool IsActive => Current > 0 && Capacity > 0;


        public ShieldSnapshot(UnitDescriptor target, int current, int capacity)
        {
            Target = target;
            Current = current < 0 ? 0 : current;
            Capacity = capacity < 0 ? 0 : capacity;
        }
    }
}
