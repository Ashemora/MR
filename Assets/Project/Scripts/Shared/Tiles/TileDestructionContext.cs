using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Tiles
{
    public readonly struct TileDestructionContext
    {
        public BattleSide Side { get; }
        public int BombRadiusBonus { get; }


        public TileDestructionContext(BattleSide side, int bombRadiusBonus)
        {
            Side = side;
            BombRadiusBonus = bombRadiusBonus < 0 ? 0 : bombRadiusBonus;
        }
    }
}
