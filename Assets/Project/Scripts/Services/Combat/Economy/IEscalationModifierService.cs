namespace Project.Scripts.Services.Combat.Economy
{
    public interface IEscalationModifierService : IBattleEconomyModifierService
    {
        bool IsEscalationActive { get; }
        void ActivateEscalation();
    }
}