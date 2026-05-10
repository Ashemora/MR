using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Buffs
{
    public interface INextAttackBuffService
    {
        int Get(UnitDescriptor source);
        int Consume(UnitDescriptor source);
        void Grant(IReadOnlyList<UnitDescriptor> targets, int amount);
    }
}