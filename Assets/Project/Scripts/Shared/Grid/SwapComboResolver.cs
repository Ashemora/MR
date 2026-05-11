using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Grid
{
    public class SwapComboResolver
    {
        public SwapComboType Resolve(TileKind kindA, TileKind kindB)
        {
            if (kindA == TileKind.Storm || kindB == TileKind.Storm)
            {
                var other = kindA == TileKind.Storm ? kindB : kindA;
                return other switch
                {
                    TileKind.Storm => SwapComboType.StormStorm,
                    TileKind.Bomb => SwapComboType.StormBomb,
                    _ => SwapComboType.StormLine
                };
            }

            if (kindA == TileKind.Bomb && kindB == TileKind.Bomb)
                return SwapComboType.BombBomb;

            var aIsBomb = kindA == TileKind.Bomb;
            var bIsBomb = kindB == TileKind.Bomb;
            if (aIsBomb || bIsBomb)
                return SwapComboType.BombLine;

            return SwapComboType.LineLine;
        }
    }
}