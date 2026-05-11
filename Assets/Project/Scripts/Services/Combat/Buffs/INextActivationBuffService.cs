using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Buffs
{
    public interface INextActivationBuffService
    {
        bool Consume(UnitDescriptor source);
    }
}