using System.Collections.Generic;
using Project.Scripts.Configs.Grid;
using Project.Scripts.Services.Grid;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Services.Board.Hinting
{
    public static class HintSelector
    {
        public static HintCandidate Select(IGridState gridState, IGridView gridView,
            IMatchFinder matchFinder, GridConfig gridConfig)
        {
            var state = gridState.GetGridState();
            var tier1 = new List<HintCandidate>();
            var tier2 = new List<HintCandidate>();
            var tier3 = new List<HintCandidate>();

            for (var x = 0; x < gridConfig.Width; x++)
            {
                for (var y = 0; y < gridConfig.Height; y++)
                {
                    TrySwapDirection(x, y, x + 1, y, state, gridView, matchFinder, gridConfig,
                        tier1, tier2, tier3);
                    TrySwapDirection(x, y, x, y + 1, state, gridView, matchFinder, gridConfig,
                        tier1, tier2, tier3);
                }
            }

            if (tier1.Count > 0)
                return tier1[Random.Range(0, tier1.Count)];

            if (tier2.Count > 0)
                return tier2[Random.Range(0, tier2.Count)];

            if (tier3.Count > 0)
                return tier3[Random.Range(0, tier3.Count)];

            return HintCandidate.None;
        }


        private static void TrySwapDirection(int x1, int y1, int x2, int y2,
            TileKind[,] state, IGridView gridView, IMatchFinder matchFinder, GridConfig gridConfig,
            List<HintCandidate> tier1, List<HintCandidate> tier2, List<HintCandidate> tier3)
        {
            if (x2 >= gridConfig.Width || y2 >= gridConfig.Height)
                return;

            var posA = new GridPoint(x1, y1);
            var posB = new GridPoint(x2, y2);

            var tileA = gridView.GetTile(posA);
            var tileB = gridView.GetTile(posB);
            if (!tileA || !tileB)
                return;

            var aIsSpecial = tileA.Config.Behaviour.IsActivatedBySwap;
            var bIsSpecial = tileB.Config.Behaviour.IsActivatedBySwap;

            (state[x1, y1], state[x2, y2]) = (state[x2, y2], state[x1, y1]);
            var matches = matchFinder.FindMatches(state);
            (state[x1, y1], state[x2, y2]) = (state[x2, y2], state[x1, y1]);

            if (matches.Count == 0)
                return;

            var candidate = new HintCandidate(posA, posB);

            if (aIsSpecial || bIsSpecial)
            {
                tier3.Add(candidate);
                return;
            }

            if (IsSimple3Match(matches))
                tier1.Add(candidate);
            else
                tier2.Add(candidate);
        }

        private static bool IsSimple3Match(List<MatchResult> matches)
        {
            for (var i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                if (m.Shape != MatchShape.Horizontal && m.Shape != MatchShape.Vertical)
                    return false;
                if (m.Positions.Count != 3)
                    return false;
            }

            return true;
        }
    }
}