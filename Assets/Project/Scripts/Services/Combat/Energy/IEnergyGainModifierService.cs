using Project.Scripts.Shared.Energy;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Energy
{
    public interface IEnergyGainModifierService
    {
        float CalculateEnergy(BattleSide side, EnergyGainBreakdown breakdown);
    }
}