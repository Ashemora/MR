using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Units
{
    public interface IUnitStateService
    {
        bool TryGetUnit(UnitDescriptor unit, out UnitRuntimeState state);
    }
}
