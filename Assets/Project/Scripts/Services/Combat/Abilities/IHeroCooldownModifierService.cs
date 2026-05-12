using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IHeroCooldownModifierService
    {
        float GetActivationCooldown(UnitDescriptor unit, float baseCooldown);
    }
}