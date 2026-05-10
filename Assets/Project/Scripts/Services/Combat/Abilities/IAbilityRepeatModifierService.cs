using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityRepeatModifierService
    {
        int GetRepeatCount(UnitDescriptor source);
    }
}