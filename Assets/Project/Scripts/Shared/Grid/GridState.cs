using System.Collections.Generic;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Grid
{
    public class GridState : IGridState
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyCollection<GridPoint> ScheduledRemovals => _scheduledRemovals;
        public void ClearScheduledRemovals() => _scheduledRemovals.Clear();
        
        
        private readonly TileKind[,] _kinds;
        private readonly HashSet<GridPoint> _scheduledRemovals = new();


        public GridState(int width, int height)
        {
            Width = width;
            Height = height;
            _kinds = new TileKind[width, height];
        }

        public bool IsValidPosition(GridPoint pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public TileKind GetKind(GridPoint pos)
        {
            return _kinds[pos.X, pos.Y];
        }

        public void SetKind(GridPoint pos, TileKind kind)
        {
            _kinds[pos.X, pos.Y] = kind;
        }

        public void ClearCell(GridPoint pos)
        {
            _kinds[pos.X, pos.Y] = TileKind.None;
        }

        public TileKind[,] GetGridState()
        {
            var copy = new TileKind[Width, Height];
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                    copy[x, y] = _kinds[x, y];
            
            return copy;
        }

        public List<GridPoint> GetNeighboursInRadius(GridPoint center, int radius)
        {
            var result = new List<GridPoint>();
            for (var dx = -radius; dx <= radius; dx++)
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    var pos = new GridPoint(center.X + dx, center.Y + dy);
                    if (IsValidPosition(pos))
                        result.Add(pos);
                }

            return result;
        }

        public List<GridPoint> GetAllInRow(int y)
        {
            var result = new List<GridPoint>(Width);
            for (var x = 0; x < Width; x++)
                result.Add(new GridPoint(x, y));
            
            return result;
        }

        public List<GridPoint> GetAllInColumn(int x)
        {
            var result = new List<GridPoint>(Height);
            for (var y = 0; y < Height; y++)
                result.Add(new GridPoint(x, y));
            
            return result;
        }

        public List<GridPoint> GetAllOfKind(TileKind kind)
        {
            var result = new List<GridPoint>();
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                    if (_kinds[x, y] == kind)
                        result.Add(new GridPoint(x, y));
            
            return result;
        }

        public List<GridPoint> GetAllOccupied()
        {
            var result = new List<GridPoint>();
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                    if (_kinds[x, y] != TileKind.None)
                        result.Add(new GridPoint(x, y));
            
            return result;
        }

        public TileKind GetMostCommonColor()
        {
            var counts = new Dictionary<TileKind, int>();
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                {
                    var kind = _kinds[x, y];
                    if (false == kind.IsColor())
                        continue;

                    counts.TryGetValue(kind, out var count);
                    counts[kind] = count + 1;
                }

            var bestKind = TileKind.None;
            var bestCount = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    bestKind = kvp.Key;
                }
            }

            return bestKind;
        }

        public void ScheduleRemove(List<GridPoint> positions)
        {
            for (var i = 0; i < positions.Count; i++)
                _scheduledRemovals.Add(positions[i]);
        }
    }
}