using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface IBombRadiusModifierService
    {
        int GetBombRadiusBonus(BattleSide side);
    }
}