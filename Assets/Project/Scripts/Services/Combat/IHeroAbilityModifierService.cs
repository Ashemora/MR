using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IHeroAbilityModifierService
    {
        int GetActivationEnergyCost(BattleSide side, int slotIndex, int baseCost);
        int GetAbilityPower(BattleSide side, int slotIndex, int basePower);
    }

    public interface IHeroCooldownModifierService
    {
        float GetActivationCooldown(BattleSide side, int slotIndex, float baseCooldown);
    }
}