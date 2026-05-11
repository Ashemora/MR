using System.Collections.Generic;
using Project.Scripts.Services.Grid;
using Project.Scripts.Shared;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;
using Project.Scripts.Utils;
using UnityEngine;

namespace Project.Scripts.Services.Board
{
    public class EnergySourcePositionCollector
    {
        private readonly Dictionary<TileKind, PositionAccumulator> _positionsByKind = new();


        public void CollectFromMatches(List<List<MatchResult>> waves, IGridView view)
        {
            if (null == waves || null == view)
                return;

            for (var i = 0; i < waves.Count; i++)
            {
                var matches = waves[i];
                if (null == matches)
                    continue;

                for (var j = 0; j < matches.Count; j++)
                {
                    var match = matches[j];
                    if (null == match || false == match.TileKind.IsColor())
                        continue;

                    Add(match.TileKind, view.GridToWorld(match.Center));
                }
            }
        }

        public void CollectFromGridDiff(TileKind[,] before, TileKind[,] after, IGridView view)
        {
            if (null == before || null == after || null == view)
                return;

            var width = before.GetLength(0);
            var height = before.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var kindBefore = before[x, y];
                    if (false == kindBefore.IsColor())
                        continue;

                    if (after[x, y] == kindBefore)
                        continue;

                    Add(kindBefore, view.GridToWorld(new GridPoint(x, y)));
                }
            }
        }

        public Dictionary<TileKind, SharedVector3> Build()
        {
            var result = new Dictionary<TileKind, SharedVector3>(_positionsByKind.Count);
            foreach (var pair in _positionsByKind)
                result[pair.Key] = (pair.Value.Sum / pair.Value.Count).ToSharedVector3();

            return result;
        }


        private void Add(TileKind kind, Vector3 position)
        {
            if (_positionsByKind.TryGetValue(kind, out var accumulator))
            {
                accumulator.Sum += position;
                accumulator.Count++;
                _positionsByKind[kind] = accumulator;
                
                return;
            }

            _positionsByKind[kind] = new PositionAccumulator
            {
                Sum = position,
                Count = 1,
            };
        }


        private struct PositionAccumulator
        {
            public Vector3 Sum;
            public int Count;
        }
    }
}