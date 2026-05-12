using Project.Scripts.Shared.Tiles;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public readonly struct UnitRuntimeState
    {
        public UnitDescriptor Unit { get; }
        public UnitActionType ActionType { get; }
        public bool IsAssigned { get; }
        public bool IsAlive { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsHpFull => MaxHP > 0 && CurrentHP >= MaxHP;
        public int BaseAbilityPower { get; }
        public int BaseActivationEnergyCost { get; }
        public TileKind SlotKind { get; }


        public UnitRuntimeState(UnitDescriptor unit, UnitActionType actionType, bool isAssigned, bool isAlive,
            int currentHP, int maxHP, int baseAbilityPower, int baseActivationEnergyCost, TileKind slotKind)
        {
            Unit = unit;
            ActionType = actionType;
            IsAssigned = isAssigned;
            IsAlive = isAlive;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            BaseAbilityPower = baseAbilityPower < 0 ? 0 : baseAbilityPower;
            BaseActivationEnergyCost = baseActivationEnergyCost < 0 ? 0 : baseActivationEnergyCost;
            SlotKind = slotKind;
        }
    }
}