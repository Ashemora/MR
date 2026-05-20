using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IAbilityExecutionService
    {
        void Execute(UnitDescriptor source, UnitDescriptor target);
        bool TryExecute(UnitDescriptor source, UnitDescriptor target, out AbilityExecutionResult result);
        bool CanTarget(UnitDescriptor source, UnitDescriptor target);
    }
}