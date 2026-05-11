using System.Collections.Generic;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Grid
{
    public interface IGridState
    {
        int Width { get; }
        int Height { get; }

        bool IsValidPosition(GridPoint pos);
        TileKind GetKind(GridPoint pos);
        void SetKind(GridPoint pos, TileKind kind);
        void ClearCell(GridPoint pos);

        TileKind[,] GetGridState();

        List<GridPoint> GetNeighboursInRadius(GridPoint center, int radius);
        List<GridPoint> GetAllInRow(int y);
        List<GridPoint> GetAllInColumn(int x);
        List<GridPoint> GetAllOfKind(TileKind kind);
        List<GridPoint> GetAllOccupied();
        TileKind GetMostCommonColor();

        void ScheduleRemove(List<GridPoint> positions);
    }
}