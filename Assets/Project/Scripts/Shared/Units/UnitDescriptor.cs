namespace Project.Scripts.Shared.Units
{
    public readonly struct UnitDescriptor
    {
        public BattleSide Side { get; }
        public UnitKind Kind { get; }
        public int SlotIndex { get; }

        
        public UnitDescriptor(BattleSide side, UnitKind kind, int slotIndex)
        {
            Side = side;
            Kind = kind;
            SlotIndex = slotIndex;
        }

        public static UnitDescriptor Avatar(BattleSide side)
        {
            return new UnitDescriptor(side, UnitKind.Avatar, -1);
        }

        public static UnitDescriptor Hero(BattleSide side, int slotIndex)
        {
            return new UnitDescriptor(side, UnitKind.Hero, slotIndex);
        }
    }
}