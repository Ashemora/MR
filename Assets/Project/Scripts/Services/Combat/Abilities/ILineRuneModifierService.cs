using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface ILineRuneModifierService
    {
        int GetLineRuneThicknessBonus(BattleSide side);
    }
}