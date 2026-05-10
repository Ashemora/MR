using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IHeroAbilityModifierService
    {
        int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost);
        int GetAbilityPower(BattleSide side, int slotIndex, int basePower);
    }
}