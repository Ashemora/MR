using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Buffs
{
    public interface INextActivationBuffService
    {
        bool Consume(UnitDescriptor source);
    }
}