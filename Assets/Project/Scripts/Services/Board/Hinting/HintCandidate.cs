using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Services.Board.Hinting
{
    public readonly struct HintCandidate
    {
        public GridPoint FirstTile { get; }
        public GridPoint SecondTile { get; }
        public bool IsValid { get; }


        public static HintCandidate None => default;


        public HintCandidate(GridPoint firstTile, GridPoint secondTile)
        {
            FirstTile = firstTile;
            SecondTile = secondTile;
            IsValid = true;
        }
    }
}