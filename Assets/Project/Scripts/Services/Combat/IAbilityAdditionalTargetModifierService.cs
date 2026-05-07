using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IAbilityAdditionalTargetModifierService
    {
        int GetAdditionalTargetCount(UnitDescriptor source);
    }
}