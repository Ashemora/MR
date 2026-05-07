using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public interface IResurrectOnDeathBuffService
    {
        int GetResurrectOnDeath(UnitDescriptor target, int maxHP);
    }
}