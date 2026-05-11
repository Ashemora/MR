namespace Project.Scripts.Shared.Units
{
    public readonly struct UnitDescriptor
    {
        public BattleSide Side { get; }
        public UnitKind Kind { get; }
        public int SlotIndex { get; }
        public UnitActionType ActionType { get; }

        
        public UnitDescriptor(BattleSide side, UnitKind kind, int slotIndex, UnitActionType actionType)
        {
            Side = side;
            Kind = kind;
            SlotIndex = slotIndex;
            ActionType = actionType;
        }

        public static UnitDescriptor Avatar(BattleSide side, UnitActionType actionType)
        {
            return new UnitDescriptor(side, UnitKind.Avatar, -1, actionType);
        }

        public static UnitDescriptor Hero(BattleSide side, int slotIndex, UnitActionType actionType)
        {
            return new UnitDescriptor(side, UnitKind.Hero, slotIndex, actionType);
        }
    }
}