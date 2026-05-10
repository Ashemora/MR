using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IHeroCooldownModifierService
    {
        float GetActivationCooldown(BattleSide side, int slotIndex, float baseCooldown);
    }
}