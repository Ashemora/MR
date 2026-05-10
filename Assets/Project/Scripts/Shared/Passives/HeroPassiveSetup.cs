using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct HeroPassiveSetup
    {
        public BattleSide Side { get; }
        public int SlotIndex { get; }
        public TileKind SlotKind { get; }
        public PassiveAbilityDefinition Definition { get; }


        public HeroPassiveSetup(BattleSide side, int slotIndex, TileKind slotKind, PassiveAbilityDefinition definition)
        {
            Side = side;
            SlotIndex = slotIndex;
            SlotKind = slotKind;
            Definition = definition;
        }
    }
}