using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Energy
{
    public interface IBattleSideEnergyService
    {
        int EnergyCap { get; }
        int GetDisplayEnergy(BattleSide side);
        bool CanSpend(BattleSide side, int amount);
        bool TrySpend(BattleSide side, int amount);
        void AddEnergy(BattleSide side, float amount);
        void Reset(BattleSide side);
    }
}