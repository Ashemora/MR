using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IEnergyGainModifierService
    {
        float CalculateEnergy(BattleSide side, EnergyGainBreakdown breakdown);
    }
}