using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityAdditionalTargetModifierService
    {
        int GetAdditionalTargetCount(UnitDescriptor source);
    }
}