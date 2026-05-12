using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct UnitPassiveSetup
    {
        public UnitDescriptor Owner { get; }
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public TileKind SlotKind { get; }
        public PassiveAbilityDefinition Definition { get; }


        public UnitPassiveSetup(BattleSide side, int slotIndex, TileKind slotKind, PassiveAbilityDefinition definition)
            : this(UnitDescriptor.Hero(side, slotIndex), slotKind, definition)
        {
        }

        public UnitPassiveSetup(UnitDescriptor owner, TileKind slotKind, PassiveAbilityDefinition definition)
        {
            Owner = owner;
            Side = owner.Side;
            SlotIndex = owner.SlotIndex;
            SlotKind = slotKind;
            Definition = definition;
        }
    }
}