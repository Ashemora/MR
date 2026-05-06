using Project.Scripts.Shared;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Tiles.Behaviours
{
    [CreateAssetMenu(fileName = "DefaultTileBehaviour", menuName = "Configs/Behaviours/Default")]
    public class DefaultTileBehaviour : TileBehaviour
    {
        public override void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context) { }
    }
}