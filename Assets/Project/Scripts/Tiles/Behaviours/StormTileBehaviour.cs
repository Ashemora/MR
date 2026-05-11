using Project.Scripts.Shared.Tiles;
using UnityEngine;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Tiles.Behaviours
{
    [CreateAssetMenu(fileName = "StormTileBehaviour", menuName = "Configs/Behaviours/Storm")]
    public class StormTileBehaviour : TileBehaviour
    {
        public override bool IsActivatedBySwap => true;


        public override void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context)
        {
            var targetKind = payloadKind.IsColor()
                ? payloadKind
                : state.GetMostCommonColor();

            if (false == targetKind.IsColor())
                return;

            var positions = state.GetAllOfKind(targetKind);
            state.ScheduleRemove(positions);
        }
    }
}