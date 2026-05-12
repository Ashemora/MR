using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IHeroAbilityModifierService
    {
        int GetActivationEnergyCost(UnitDescriptor unit, int baseCost);
        int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost);
        int GetAbilityPower(BattleSide side, int slotIndex, int basePower);
    }
}