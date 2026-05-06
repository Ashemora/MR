using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface INextAttackBuffService
    {
        int Get(UnitDescriptor source);
        int Consume(UnitDescriptor source);
        void Grant(IReadOnlyList<UnitDescriptor> targets, int amount);
    }

    public interface INextActivationBuffService
    {
        bool Consume(UnitDescriptor source);
    }
}
