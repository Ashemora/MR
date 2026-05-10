using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IBombRadiusModifierService
    {
        int GetBombRadiusBonus(BattleSide side);
    }
}