namespace Project.Scripts.Services.Combat.Economy
{
    public interface IBattleEconomyModifierService
    {
        float CascadeEnergyMultiplier { get; }
        float AutoEnergyIntervalMultiplier { get; }
    }
}