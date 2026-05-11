using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IUnitAbilityActivationService
    {
        bool TryPreview(UnitDescriptor source, out UnitAbilityActivationState state);
        bool TryCommit(UnitDescriptor source, out UnitAbilityActivationState state);
    }
}