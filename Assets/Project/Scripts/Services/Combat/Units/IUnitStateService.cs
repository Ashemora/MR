using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IUnitStateService
    {
        bool TryGetUnit(UnitDescriptor unit, out UnitRuntimeState state);
    }
}