using Project.Scripts.Shared.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Tiles.Behaviours
{
    public abstract class TileBehaviour : ScriptableObject
    {
        public virtual bool IsActivatedBySwap => false;


        public abstract void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context);
    }
}