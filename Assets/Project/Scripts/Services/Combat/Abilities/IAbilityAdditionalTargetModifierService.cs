using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityAdditionalTargetModifierService
    {
        int GetAdditionalTargetCount(UnitDescriptor source);
    }
}