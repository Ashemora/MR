using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Board;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Grid
{
    public class GridManager : IGridView, IGridOperations
    {
        public IGridState State => _state;
        
        
        private readonly TileSetConfig _tileSetConfig;
        private readonly GridConfig _gridConfig;
        private readonly BoardAnimationConfig _animConfig;
        private readonly TilePool _pool;
        private readonly Tile[,] _tiles;
        private readonly GridState _state;
        private readonly IBoardRuntimeService _boardRuntimeService;
        private readonly EventBus _eventBus;
        private readonly IBombRadiusModifierService _bombRadiusModifierService;
        private readonly ILineRuneModifierService _lineRuneModifierService;
        private float _cellSize;
        private Vector3 _origin;
       

        public GridManager(TileSetConfig tileSetConfig, GridConfig gridConfig, BoardAnimationConfig animConfig,
            TilePool pool, float cellSize, IBoardRuntimeService boardRuntimeService, EventBus eventBus,
            IBombRadiusModifierService bombRadiusModifierService, ILineRuneModifierService lineRuneModifierService)
        {
            _tileSetConfig = tileSetConfig;
            _gridConfig = gridConfig;
            _animConfig = animConfig;
            _pool = pool;
            _cellSize = cellSize;
            _tiles = new Tile[gridConfig.Width, gridConfig.Height];
            _state = new GridState(gridConfig.Width, gridConfig.Height);
            _boardRuntimeService = boardRuntimeService;
            _eventBus = eventBus;
            _bombRadiusModifierService = bombRadiusModifierService;
            _lineRuneModifierService = lineRuneModifierService;
        }

        // IGridView
        public void SetOrigin(Vector3 origin) => _origin = origin;

#if UNITY_EDITOR
        public void SetCellSize(float cellSize) => _cellSize = cellSize;
#endif

        public Vector3 GridToWorld(GridPoint pos) =>
            _origin + new Vector3(pos.X * _cellSize, pos.Y * _cellSize, 0f);

        public GridPoint WorldToGrid(Vector3 worldPos)
        {
            var relative = worldPos - _origin;
            
            return new GridPoint(
                Mathf.RoundToInt(relative.x / _cellSize),
                Mathf.RoundToInt(relative.y / _cellSize)
            );
        }

        public Tile GetTile(GridPoint pos) => _tiles[pos.X, pos.Y];

        public void SetTile(GridPoint pos, Tile tile)
        {
            _tiles[pos.X, pos.Y] = tile;
            _state.SetKind(pos, tile ? tile.Kind : TileKind.None);
        }

        public void ClearTile(GridPoint pos)
        {
            _tiles[pos.X, pos.Y] = null;
            _state.ClearCell(pos);
        }

        public TileConfig ResolveRegularTile() =>
            _tileSetConfig.RegularTiles[UnityEngine.Random.Range(0, _tileSetConfig.RegularTiles.Length)];

#if UNITY_EDITOR
        public void RepositionAllTiles()
        {
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _tiles[x, y];
                    if (tile)
                        tile.transform.position = GridToWorld(new GridPoint(x, y));
                }
        }

        public void ReplaceForEdit(GridPoint pos, TileKind kind)
        {
            var tile = _tiles[pos.X, pos.Y];
            if (false == tile)
                return;

            var config = FindConfigForKind(kind);
            if (!config)
                return;

            tile.Init(config, pos);
            _state.SetKind(pos, config.Kind);
        }

        private TileConfig FindConfigForKind(TileKind kind)
        {
            for (var i = 0; i < _tileSetConfig.RegularTiles.Length; i++)
                if (_tileSetConfig.RegularTiles[i].Kind == kind)
                    return _tileSetConfig.RegularTiles[i];

            if (_tileSetConfig.SpecialTiles != null)
                for (var i = 0; i < _tileSetConfig.SpecialTiles.Length; i++)
                    if (_tileSetConfig.SpecialTiles[i].Kind == kind)
                        return _tileSetConfig.SpecialTiles[i];

            return null;
        }
#endif

        // IGridOperations
        public async UniTask PopulateGrid()
        {
            var kindCache = new TileKind[_gridConfig.Width, _gridConfig.Height];
            var tasks = new UniTask[_gridConfig.Width * _gridConfig.Height];
            var idx = 0;
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var pos = new GridPoint(x, y);
                    var tileConfig = GetNoMatchConfig(x, y, kindCache);
                    kindCache[x, y] = tileConfig.Kind;
                    var tile = _pool.Get();
                    tile.transform.position = GridToWorld(pos);
                    tile.Init(tileConfig, pos);
                    _tiles[x, y] = tile;
                    _state.SetKind(pos, tileConfig.Kind);
                    tasks[idx++] = tile.Animator.AnimateSpawn();
                }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask RemoveMatches(List<MatchResult> matches, Dictionary<GridPoint, SpecialTileSpawnData> specialPlacements)
        {
            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            var posSet = new HashSet<GridPoint>();
            for (var i = 0; i < matches.Count; i++)
                for (var j = 0; j < matches[i].Positions.Count; j++)
                    posSet.Add(matches[i].Positions[j]);

            await ProcessRemovals(new List<GridPoint>(posSet), specialPlacements);

            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            await ProcessScheduledRemovals();
        }

        public async UniTask ActivateBySwap(GridPoint pos)
        {
            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            await ProcessRemovals(new List<GridPoint> { pos }, null);

            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            await ProcessScheduledRemovals();
        }

        public async UniTask ActivateTiles(List<GridPoint> positions)
        {
            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            if (positions.Count == 0)
                return;

            await ProcessRemovals(positions, null);

            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            await ProcessScheduledRemovals();
        }

        public async UniTask ConsumeTile(GridPoint pos)
        {
            if (false == IsBoardFlowRunning())
                return;

            if (false == _state.IsValidPosition(pos))
                return;

            var tile = _tiles[pos.X, pos.Y];
            if (false == tile)
                return;

            await tile.Animator.AnimateDestroy();

            if (false == IsBoardFlowRunning())
                return;

            if (_tiles[pos.X, pos.Y] != tile)
                return;

            _tiles[pos.X, pos.Y] = null;
            _state.ClearCell(pos);
            _pool.Release(tile);
        }

        public async UniTask SwapTiles(GridPoint from, GridPoint to)
        {
            var tileA = _tiles[from.X, from.Y];
            var tileB = _tiles[to.X, to.Y];

            _tiles[from.X, from.Y] = tileB;
            _tiles[to.X, to.Y] = tileA;
            _state.SetKind(from, tileB ? tileB.Kind : TileKind.None);
            _state.SetKind(to, tileA ? tileA.Kind : TileKind.None);

            if (tileA)
                tileA.GridPosition = to;
            if (tileB)
                tileB.GridPosition = from;

            await UniTask.WhenAll(
                tileA ? tileA.Animator.AnimateSwapTo(GridToWorld(to)) : UniTask.CompletedTask,
                tileB ? tileB.Animator.AnimateSwapTo(GridToWorld(from)) : UniTask.CompletedTask
            );
        }

        public async UniTask ShuffleGrid()
        {
            if (false == IsBoardFlowRunning())
                return;

            var positions = new List<GridPoint>();
            var configs = new List<TileConfig>();
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _tiles[x, y];
                    if (tile)
                    {
                        positions.Add(new GridPoint(x, y));
                        configs.Add(tile.Config);
                    }
                }

            for (var i = configs.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (configs[i], configs[j]) = (configs[j], configs[i]);
            }

            var assignedKinds = new TileKind[_gridConfig.Width, _gridConfig.Height];
            for (var i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                for (var j = i; j < configs.Count; j++)
                {
                    if (false == WouldCreateMatch(pos.X, pos.Y, configs[j].Kind, assignedKinds))
                    {
                        if (j != i) (configs[i], configs[j]) = (configs[j], configs[i]);
                        break;
                    }
                }
                assignedKinds[pos.X, pos.Y] = configs[i].Kind;
            }

            var tasks = new List<UniTask>();
            for (var i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                _tiles[pos.X, pos.Y].Init(configs[i], pos);
                _state.SetKind(pos, configs[i].Kind);
                tasks.Add(_tiles[pos.X, pos.Y].Animator.AnimateSpawn());
            }

            await UniTask.WhenAll(tasks);
        }

        public async UniTask CollapseAll(float duration, DG.Tweening.Ease ease)
        {
            ClearPendingScheduledRemovals();

            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _tiles[x, y];
                    if (tile)
                        tile.Animator.StopActiveAnimations();
                }

            var tasks = new List<UniTask>();
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var tile = _tiles[x, y];

                    if (tile)
                        tasks.Add(tile.Animator.AnimateCollapse(duration, ease));
                }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            _boardRuntimeService.MarkFrozen();
        }

        public void ForceInjectMove()
        {
            if (_tileSetConfig.RegularTiles.Length < 2 || _gridConfig.Width < 4)
                return;

            var configT = _tileSetConfig.RegularTiles[0];
            var configX = _tileSetConfig.RegularTiles[1];

            ReInitTileAt(0, 0, configT);
            ReInitTileAt(1, 0, configX);
            ReInitTileAt(2, 0, configT);
            ReInitTileAt(3, 0, configT);
        }

        public bool RepairBoardState()
        {
            var repaired = false;
            if (_state.ScheduledRemovals.Count > 0)
            {
                _state.ClearScheduledRemovals();
                repaired = true;
            }

            var kinds = _state.GetGridState();
            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    var pos = new GridPoint(x, y);
                    var tile = _tiles[x, y];
                    if (false == tile)
                    {
                        kinds[x, y] = TileKind.None;
                        continue;
                    }

                    var worldPos = GridToWorld(pos);
                    if (tile.GridPosition != pos)
                    {
                        tile.Animator.StopActiveAnimations();
                        tile.GridPosition = pos;
                        repaired = true;
                    }

                    if ((tile.transform.position - worldPos).sqrMagnitude > 0.0001f)
                    {
                        tile.Animator.StopActiveAnimations();
                        tile.transform.position = worldPos;
                        repaired = true;
                    }

                    if (_state.GetKind(pos) != tile.Kind)
                    {
                        _state.SetKind(pos, tile.Kind);
                        repaired = true;
                    }

                    kinds[x, y] = tile.Kind;
                }

            for (var x = 0; x < _gridConfig.Width; x++)
                for (var y = 0; y < _gridConfig.Height; y++)
                {
                    if (_tiles[x, y])
                        continue;

                    var pos = new GridPoint(x, y);
                    var tileConfig = GetRepairConfig(x, y, kinds);
                    var tile = _pool.Get();
                    tile.transform.position = GridToWorld(pos);
                    tile.Init(tileConfig, pos);
                    _tiles[x, y] = tile;
                    _state.SetKind(pos, tileConfig.Kind);
                    kinds[x, y] = tileConfig.Kind;
                    repaired = true;
                }

            return repaired;
        }

        private async UniTask ProcessScheduledRemovals()
        {
            while (_state.ScheduledRemovals.Count > 0)
            {
                if (false == IsBoardFlowRunning())
                {
                    ClearPendingScheduledRemovals();
                    return;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_animConfig.ChainReactionWaveDelay));

                if (false == IsBoardFlowRunning())
                {
                    ClearPendingScheduledRemovals();
                    return;
                }

                var batch = new List<GridPoint>(_state.ScheduledRemovals);
                _state.ClearScheduledRemovals();
                await ProcessRemovals(batch, null);

                if (false == IsBoardFlowRunning())
                {
                    ClearPendingScheduledRemovals();
                    return;
                }
            }
        }

        private async UniTask ProcessRemovals(List<GridPoint> positions, Dictionary<GridPoint, SpecialTileSpawnData> specialPlacements)
        {
            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            var toDestroy = new List<(GridPoint pos, Tile tile)>(positions.Count);
            for (var i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                if (false == _state.IsValidPosition(pos))
                    continue;

                var tile = _tiles[pos.X, pos.Y];
                if (false == tile)
                    continue;

                toDestroy.Add((pos, tile));
            }

            if (toDestroy.Count == 0)
                return;

            var destroyTasks = new UniTask[toDestroy.Count];
            for (var i = 0; i < toDestroy.Count; i++)
                destroyTasks[i] = toDestroy[i].tile.Animator.AnimateDestroy();

            await UniTask.WhenAll(destroyTasks);

            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            ReleaseDestroyedTiles(toDestroy, invokeBehaviours: true);

            if (null == specialPlacements || specialPlacements.Count == 0)
                return;

            if (false == IsBoardFlowRunning())
            {
                ClearPendingScheduledRemovals();
                return;
            }

            var spawnTasks = new List<UniTask>(specialPlacements.Count);
            var specialPairs = new List<KeyValuePair<GridPoint, SpecialTileSpawnData>>(specialPlacements);
            for (var i = 0; i < specialPairs.Count; i++)
            {
                if (false == IsBoardFlowRunning())
                {
                    ClearPendingScheduledRemovals();
                    return;
                }

                var pos = specialPairs[i].Key;
                if (false == _state.IsValidPosition(pos))
                    continue;

                if (_tiles[pos.X, pos.Y])
                    continue;

                var data = specialPairs[i].Value;
                var specialTile = _pool.Get();
                specialTile.transform.position = GridToWorld(pos);
                specialTile.Init(data.Config, pos, data.PayloadKind);
                _tiles[pos.X, pos.Y] = specialTile;
                _state.SetKind(pos, data.Config.Kind);

                spawnTasks.Add(specialTile.Animator.AnimateSpawn());
            }

            if (spawnTasks.Count > 0)
                await UniTask.WhenAll(spawnTasks);
        }

        private bool IsBoardFlowRunning()
        {
            return _boardRuntimeService.CanContinueResolution;
        }

        private void ClearPendingScheduledRemovals()
        {
            _state.ClearScheduledRemovals();
        }

        private void ReleaseDestroyedTiles(List<(GridPoint pos, Tile tile)> toDestroy, bool invokeBehaviours)
        {
            for (var i = 0; i < toDestroy.Count; i++)
            {
                var (pos, tile) = toDestroy[i];
                if (_tiles[pos.X, pos.Y] != tile)
                    continue;

                if (invokeBehaviours)
                {
                    tile.Config.Behaviour.OnTileDestroyed(pos, _state, tile.PayloadKind,
                        CreateTileDestructionContext(BattleSide.Player));
                    PublishSpecialTileUsed(tile.Kind);
                }

                _tiles[pos.X, pos.Y] = null;
                _state.ClearCell(pos);
                _pool.Release(tile);
            }
        }

        private void PublishSpecialTileUsed(TileKind kind)
        {
            if (false == kind.IsSpecial())
                return;

            _eventBus.Publish(new BattleSideSpecialTileUsedEvent(BattleSide.Player, kind));
        }

        private TileDestructionContext CreateTileDestructionContext(BattleSide side)
        {
            return new TileDestructionContext(side, _bombRadiusModifierService.GetBombRadiusBonus(side),
                _lineRuneModifierService.GetLineRuneThicknessBonus(side));
        }

        private void ReInitTileAt(int x, int y, TileConfig config)
        {
            var tile = _tiles[x, y];
            if (tile)
            {
                tile.Init(config, new GridPoint(x, y));
                _state.SetKind(new GridPoint(x, y), config.Kind);
            }
        }

        private TileConfig GetRepairConfig(int x, int y, TileKind[,] kinds)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var config = _tileSetConfig.RegularTiles[UnityEngine.Random.Range(0, _tileSetConfig.RegularTiles.Length)];
                if (false == WouldCreateRepairMatch(x, y, config.Kind, kinds))
                    return config;
            }

            return _tileSetConfig.RegularTiles[0];
        }

        private bool WouldCreateRepairMatch(int x, int y, TileKind kind, TileKind[,] kinds)
        {
            if (false == kind.IsColor())
                return false;

            return IsSameKindAt(x - 2, y, kind, kinds) && IsSameKindAt(x - 1, y, kind, kinds)
                || IsSameKindAt(x - 1, y, kind, kinds) && IsSameKindAt(x + 1, y, kind, kinds)
                || IsSameKindAt(x + 1, y, kind, kinds) && IsSameKindAt(x + 2, y, kind, kinds)
                || IsSameKindAt(x, y - 2, kind, kinds) && IsSameKindAt(x, y - 1, kind, kinds)
                || IsSameKindAt(x, y - 1, kind, kinds) && IsSameKindAt(x, y + 1, kind, kinds)
                || IsSameKindAt(x, y + 1, kind, kinds) && IsSameKindAt(x, y + 2, kind, kinds);
        }

        private bool IsSameKindAt(int x, int y, TileKind kind, TileKind[,] kinds)
        {
            if (x < 0 || x >= _gridConfig.Width || y < 0 || y >= _gridConfig.Height)
                return false;

            return kinds[x, y] == kind;
        }

        private bool WouldCreateMatch(int x, int y, TileKind kind, TileKind[,] assigned)
        {
            if (false == kind.IsColor())
                return false;

            if (x >= 2 && assigned[x - 1, y] == kind && assigned[x - 2, y] == kind)
                return true;

            if (y >= 2 && assigned[x, y - 1] == kind && assigned[x, y - 2] == kind)
                return true;

            return false;
        }

        private TileConfig GetNoMatchConfig(int x, int y, TileKind[,] kinds)
        {
            var forbidden = new HashSet<TileKind>();
            if (x >= 2 && kinds[x - 1, y] == kinds[x - 2, y] && kinds[x - 1, y].IsColor())
                forbidden.Add(kinds[x - 1, y]);
            if (y >= 2 && kinds[x, y - 1] == kinds[x, y - 2] && kinds[x, y - 1].IsColor())
                forbidden.Add(kinds[x, y - 1]);

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var config = _tileSetConfig.RegularTiles[UnityEngine.Random.Range(0, _tileSetConfig.RegularTiles.Length)];
                if (false == forbidden.Contains(config.Kind))
                    return config;
            }

            return _tileSetConfig.RegularTiles[0];
        }
    }
}