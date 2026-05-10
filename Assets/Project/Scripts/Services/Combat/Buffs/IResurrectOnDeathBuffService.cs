using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Buffs
{
    public interface IResurrectOnDeathBuffService
    {
        int GetResurrectOnDeath(UnitDescriptor target, int maxHP);
    }
}