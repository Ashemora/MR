using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityRepeatModifierService
    {
        int GetRepeatCount(UnitDescriptor source);
    }
}