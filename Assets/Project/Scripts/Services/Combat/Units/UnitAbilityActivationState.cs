using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public readonly struct UnitAbilityActivationState
    {
        public UnitActionType ActionType { get; }
        public int ActionValue { get; }
        public bool IsAlive { get; }


        public UnitAbilityActivationState(UnitActionType actionType, int actionValue, bool isAlive)
        {
            ActionType = actionType;
            ActionValue = actionValue < 0 ? 0 : actionValue;
            IsAlive = isAlive;
        }
    }
}