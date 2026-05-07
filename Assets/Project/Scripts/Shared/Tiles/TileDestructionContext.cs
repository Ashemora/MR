using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Tiles
{
    public readonly struct TileDestructionContext
    {
        public BattleSide Side { get; }
        public int BombRadiusBonus { get; }
        public int LineRuneThicknessBonus { get; }


        public TileDestructionContext(BattleSide side, int bombRadiusBonus, int lineRuneThicknessBonus)
        {
            Side = side;
            BombRadiusBonus = bombRadiusBonus < 0 ? 0 : bombRadiusBonus;
            LineRuneThicknessBonus = lineRuneThicknessBonus < 0 ? 0 : lineRuneThicknessBonus;
        }
    }
}