using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IAbilityExecutionService
    {
        void Execute(UnitDescriptor source, UnitDescriptor target);
        bool TryExecute(UnitDescriptor source, UnitDescriptor target, out AbilityExecutionResult result);
    }
}