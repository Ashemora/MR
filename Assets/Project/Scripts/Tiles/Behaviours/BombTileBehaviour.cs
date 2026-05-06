using Project.Scripts.Shared;
using Project.Scripts.Shared.Rules;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Tiles.Behaviours
{
    [CreateAssetMenu(fileName = "BombTileBehaviour", menuName = "Configs/Behaviours/Bomb")]
    public class BombTileBehaviour : TileBehaviour
    {
        [Tooltip("Радиус взрыва в ячейках для стандартной активации - уничтожает все тайлы в пределах этого радиуса")]
        [SerializeField] private int _radius = 1;

        [Tooltip("Радиус в ячейках при обмене двух бомб между собой (комбо Бомба + Бомба)")]
        [SerializeField] private int _doubleRadius = 2;


        public override bool IsActivatedBySwap => true;
        public int Radius => _radius;
        public int DoubleRadius => _doubleRadius;


        public override void OnTileDestroyed(GridPoint gridPos, IGridState state, TileKind payloadKind,
            TileDestructionContext context)
        {
            var radius = BombRadiusRules.GetEffectiveRadius(_radius, context.BombRadiusBonus);
            var neighbours = state.GetNeighboursInRadius(gridPos, radius);
            state.ScheduleRemove(neighbours);
        }
    }
}