using Project.Scripts.Configs.Board;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Services.Grid
{
    public interface IGridView
    {
        void SetOrigin(Vector3 origin);
        Vector3 GridToWorld(GridPoint pos);
        GridPoint WorldToGrid(Vector3 worldPos);

        Tile GetTile(GridPoint pos);
        void SetTile(GridPoint pos, Tile tile);
        void ClearTile(GridPoint pos);

        TileConfig ResolveRegularTile();

#if UNITY_EDITOR
        void ReplaceForEdit(GridPoint pos, TileKind kind);
#endif
    }
}