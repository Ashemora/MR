using System.Collections.Generic;
using Project.Scripts.Shared.Grid;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Energy
{
    public static class MatchEnergyRules
    {
        public static Dictionary<TileKind, float> CalculateMatchEnergy(
            IReadOnlyList<IReadOnlyList<MatchResult>> waves,
            CascadeEnergySettings settings)
        {
            var energy = new Dictionary<TileKind, float>();
            AccumulateMatchEnergy(waves, settings, energy);

            return energy;
        }

        public static void AccumulateMatchEnergy(
            IReadOnlyList<IReadOnlyList<MatchResult>> waves,
            CascadeEnergySettings settings,
            Dictionary<TileKind, float> energy)
        {
            for (var i = 0; i < waves.Count; i++)
            {
                var matches = waves[i];
                for (var j = 0; j < matches.Count; j++)
                {
                    var match = matches[j];
                    if (false == match.TileKind.IsColor())
                        continue;

                    energy.TryGetValue(match.TileKind, out var current);
                    energy[match.TileKind] = current + CalculateMatchEnergy(match, i, matches.Count, settings);
                }
            }
        }

        public static float CalculateMatchEnergy(MatchResult match, int waveIndex, int matchCountInWave,
            CascadeEnergySettings settings)
        {
            if (null == match || false == match.TileKind.IsColor())
                return 0f;

            return match.Positions.Count
                   * GetShapeMultiplier(match.Shape, settings)
                   * GetCascadeMultiplier(waveIndex, settings)
                   * GetMultiMatchMultiplier(matchCountInWave, settings);
        }

        public static float GetCascadeMultiplier(int waveIndex, CascadeEnergySettings settings)
        {
            return 1f + settings.CascadeMultiplierStep * waveIndex;
        }

        public static float GetMultiMatchMultiplier(int matchCountInWave, CascadeEnergySettings settings)
        {
            return matchCountInWave > 1 ? settings.MultiMatchMultiplier : 1f;
        }

        public static float GetShapeMultiplier(MatchShape shape, CascadeEnergySettings settings) => shape switch
        {
            MatchShape.LShape => settings.LShapeMultiplier,
            MatchShape.TShape => settings.TShapeMultiplier,
            _ => 1f
        };

        public static Dictionary<TileKind, float> CalculateGridDiffEnergy(TileKind[,] before, TileKind[,] after,
            float multiplier = 1f)
        {
            var energy = new Dictionary<TileKind, float>();
            AccumulateGridDiffEnergy(before, after, energy, multiplier);

            return energy;
        }

        public static void AccumulateGridDiffEnergy(TileKind[,] before, TileKind[,] after,
            Dictionary<TileKind, float> energy, float multiplier = 1f)
        {
            var width = before.GetLength(0);
            var height = before.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var kindBefore = before[x, y];
                    if (false == kindBefore.IsColor())
                        continue;

                    if (after[x, y] != kindBefore)
                    {
                        energy.TryGetValue(kindBefore, out var current);
                        energy[kindBefore] = current + multiplier;
                    }
                }
            }
        }
    }
}