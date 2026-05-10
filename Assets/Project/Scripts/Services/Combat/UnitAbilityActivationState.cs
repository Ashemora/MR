using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public readonly struct UnitAbilityActivationState
    {
        public HeroActionType ActionType { get; }
        public int ActionValue { get; }
        public bool IsAlive { get; }


        public UnitAbilityActivationState(HeroActionType actionType, int actionValue, bool isAlive)
        {
            ActionType = actionType;
            ActionValue = actionValue < 0 ? 0 : actionValue;
            IsAlive = isAlive;
        }
    }
}