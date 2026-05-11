using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public interface ILineRuneModifierService
    {
        int GetLineRuneThicknessBonus(BattleSide side);
    }
}