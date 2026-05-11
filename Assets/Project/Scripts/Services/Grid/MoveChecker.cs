using Project.Scripts.Configs.Grid;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Services.Grid
{
    public class MoveChecker : IMoveChecker
    {
        private readonly IGridState _state;
        private readonly IGridView _view;
        private readonly IMatchFinder _matchFinder;
        private readonly GridConfig _config;


        public MoveChecker(IGridState state, IGridView view, IMatchFinder matchFinder, GridConfig config)
        {
            _state = state;
            _view = view;
            _matchFinder = matchFinder;
            _config = config;
        }

        public bool HasPossibleMoves()
        {
            var state = _state.GetGridState();
            for (var x = 0; x < _config.Width; x++)
                for (var y = 0; y < _config.Height; y++)
                {
                    var tileA = _view.GetTile(new GridPoint(x, y));
                    if (!tileA)
                        continue;

                    if (x + 1 < _config.Width)
                    {
                        var tileB = _view.GetTile(new GridPoint(x + 1, y));
                        if (tileB && IsValidSwap(state, tileA, tileB, x, y, x + 1, y))
                            return true;
                    }

                    if (y + 1 < _config.Height)
                    {
                        var tileB = _view.GetTile(new GridPoint(x, y + 1));
                        if (tileB && IsValidSwap(state, tileA, tileB, x, y, x, y + 1))
                            return true;
                    }
                }

            return false;
        }


        private bool IsValidSwap(TileKind[,] state, Tiles.Tile tileA, Tiles.Tile tileB, int x1, int y1, int x2, int y2)
        {
            if (tileA.Config.Behaviour.IsActivatedBySwap || tileB.Config.Behaviour.IsActivatedBySwap)
                return true;

            (state[x1, y1], state[x2, y2]) = (state[x2, y2], state[x1, y1]);
            var hasMatch = _matchFinder.FindMatches(state).Count > 0;
            (state[x1, y1], state[x2, y2]) = (state[x2, y2], state[x1, y1]);

            return hasMatch;
        }
    }
}