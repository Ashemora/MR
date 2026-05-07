using Project.Scripts.Shared;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Tiles.Behaviours
{
    [CreateAssetMenu(fileName = "LineTileBehaviour", menuName = "Configs/Behaviours/Line")]
    public class LineTileBehaviour : TileBehaviour
    {
        [Tooltip("True - очищает всю строку; False - очищает весь столбец")]
        [SerializeField] private bool _isHorizontal;


        public override bool IsActivatedBySwap => true;
        public bool IsHorizontal => _isHorizontal;


        public override void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context)
        {
            var orientation = _isHorizontal ? LineClearOrientation.Horizontal : LineClearOrientation.Vertical;
            var positions = LineClearRules.GetAffectedPositions(state, gridPos, orientation,
                context.LineRuneThicknessBonus);
            state.ScheduleRemove(positions);
        }
    }
}