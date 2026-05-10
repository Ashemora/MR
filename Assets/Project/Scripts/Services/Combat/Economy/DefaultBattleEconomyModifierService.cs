namespace Project.Scripts.Services.Combat.Economy
{
    public class DefaultBattleEconomyModifierService : IEscalationModifierService
    {
        public bool IsEscalationActive => false;
        public float CascadeEnergyMultiplier => 1f;
        public float AutoEnergyIntervalMultiplier => 1f;


        public void ActivateEscalation()
        {
        }
    }
}