using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface INextActivationBuffService
    {
        bool Consume(UnitDescriptor source);
    }
}