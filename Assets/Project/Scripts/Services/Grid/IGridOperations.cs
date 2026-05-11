using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Tiles;

namespace Project.Scripts.Services.Grid
{
    public interface IGridOperations
    {
        UniTask PopulateGrid();
        UniTask SwapTiles(GridPoint from, GridPoint to);
        UniTask RemoveMatches(List<MatchResult> matches, Dictionary<GridPoint, SpecialTileSpawnData> specialPlacements);
        UniTask ActivateBySwap(GridPoint pos);
        UniTask ActivateTiles(List<GridPoint> positions);
        UniTask ConsumeTile(GridPoint pos);
        UniTask ShuffleGrid();
        UniTask CollapseAll(float duration, DG.Tweening.Ease ease);
        void ForceInjectMove();
        bool RepairBoardState();
    }
}