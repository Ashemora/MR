using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Heroes
{
    public struct HeroSlotState
    {
        public TileKind SlotKind;
        public bool IsAssigned;
        public int ActivationEnergyCost;
        public HeroActionType ActionType;
        public int ActionValue;
        public int CurrentHP;
        public int MaxHP;

        
        public bool IsAlive => MaxHP <= 0 || CurrentHP > 0;
        public bool CanAccumulateEnergy => IsAssigned && IsAlive;
    }
}