using Project.Scripts.Shared;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Tiles.Behaviours
{
    public abstract class TileBehaviour : ScriptableObject
    {
        public virtual bool IsActivatedBySwap => false;


        public abstract void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context);
    }
}