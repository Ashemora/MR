using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Services.Board;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Services.Grid
{
    public class GravityHandler : IGravityHandler
    {
        private readonly IGridState _state;
        private readonly IGridView _view;
        private readonly TilePool _pool;
        private readonly GridConfig _config;
        private readonly IBoardRuntimeService _boardRuntimeService;


        public GravityHandler(IGridState state, IGridView view, TilePool pool, GridConfig config,
            IBoardRuntimeService boardRuntimeService)
        {
            _state = state;
            _view = view;
            _pool = pool;
            _config = config;
            _boardRuntimeService = boardRuntimeService;
        }


        public async UniTask ApplyGravity()
        {
            if (false == IsBoardFlowRunning())
                return;

            var tasks = new List<UniTask>();
            for (var x = 0; x < _config.Width; x++)
            {
                var writeY = 0;
                for (var readY = 0; readY < _config.Height; readY++)
                {
                    if (false == IsBoardFlowRunning())
                        return;

                    var tile = _view.GetTile(new GridPoint(x, readY));
                    if (!tile)
                        continue;

                    if (readY != writeY)
                    {
                        var from = new GridPoint(x, readY);
                        var to = new GridPoint(x, writeY);
                        _view.ClearTile(from);
                        _view.SetTile(to, tile);
                        tile.GridPosition = to;
                        tasks.Add(tile.Animator.AnimateFallTo(_view.GridToWorld(to)));
                    }
                    writeY++;
                }
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask SpawnNewTiles()
        {
            if (false == IsBoardFlowRunning())
                return;

            var emptyPositions = new List<GridPoint>();
            for (var x = 0; x < _config.Width; x++)
                for (var y = _config.Height - 1; y >= 0; y--)
                {
                    if (false == IsBoardFlowRunning())
                        return;

                    var pos = new GridPoint(x, y);
                    if (!_view.GetTile(pos))
                        emptyPositions.Add(pos);
                }

            var spawnHeights = new int[_config.Width];
            for (var x = 0; x < _config.Width; x++)
                spawnHeights[x] = _config.Height;

            var tasks = new List<UniTask>();
            for (var i = 0; i < emptyPositions.Count; i++)
            {
                if (false == IsBoardFlowRunning())
                    return;

                var pos = emptyPositions[i];
                var tileConfig = _view.ResolveRegularTile();
                var tile = _pool.Get();
                tile.transform.position = _view.GridToWorld(new GridPoint(pos.X, spawnHeights[pos.X]));
                tile.Init(tileConfig, pos);
                _view.SetTile(pos, tile);
                tasks.Add(tile.Animator.AnimateFallTo(_view.GridToWorld(pos)));
                spawnHeights[pos.X]++;
            }

            await UniTask.WhenAll(tasks);
        }

        private bool IsBoardFlowRunning()
        {
            return _boardRuntimeService.CanContinueResolution;
        }
    }
}