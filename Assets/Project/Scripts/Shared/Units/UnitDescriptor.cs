namespace Project.Scripts.Shared.Units
{
    public readonly struct UnitDescriptor
    {
        public BattleSide Side { get; }
        public UnitKind Kind { get; }
        public int SlotIndex { get; }
        public HeroActionType ActionType { get; }

        
        public UnitDescriptor(BattleSide side, UnitKind kind, int slotIndex, HeroActionType actionType)
        {
            Side = side;
            Kind = kind;
            SlotIndex = slotIndex;
            ActionType = actionType;
        }

        public static UnitDescriptor Avatar(BattleSide side, HeroActionType actionType)
        {
            return new UnitDescriptor(side, UnitKind.Avatar, -1, actionType);
        }

        public static UnitDescriptor Hero(BattleSide side, int slotIndex, HeroActionType actionType)
        {
            return new UnitDescriptor(side, UnitKind.Hero, slotIndex, actionType);
        }
    }
}