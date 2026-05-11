using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public readonly struct AbilityStatsChangeResult
    {
        public UnitDescriptor Target { get; }
        public int ActivationEnergyCost { get; }
        public int AbilityPower { get; }


        public AbilityStatsChangeResult(UnitDescriptor target, int activationEnergyCost, int abilityPower)
        {
            Target = target;
            ActivationEnergyCost = activationEnergyCost < 0 ? 0 : activationEnergyCost;
            AbilityPower = abilityPower < 0 ? 0 : abilityPower;
        }
    }
}