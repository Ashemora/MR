using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IAbilityRepeatModifierService
    {
        int GetRepeatCount(UnitDescriptor source);
    }
}