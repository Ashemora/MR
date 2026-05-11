using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityPowerModifierService
    {
        int GetAbilityPower(UnitDescriptor target, int basePower);
    }
}