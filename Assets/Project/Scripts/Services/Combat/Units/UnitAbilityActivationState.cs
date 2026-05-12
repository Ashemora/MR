using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public readonly struct UnitAbilityActivationState
    {
        public UnitActionType ActionType { get; }
        public bool IsAlive { get; }


        public UnitAbilityActivationState(UnitActionType actionType, bool isAlive)
        {
            ActionType = actionType;
            IsAlive = isAlive;
        }
    }
}