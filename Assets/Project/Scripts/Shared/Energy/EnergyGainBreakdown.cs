using System.Collections.Generic;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Energy
{
    public readonly struct EnergyGainBreakdown
    {
        public IReadOnlyDictionary<TileKind, float> MatchEnergyByKind => _matchEnergyByKind ?? EmptyEnergyByKind;
        public IReadOnlyDictionary<TileKind, float> SpecialActivationEnergyByKind => _specialActivationEnergyByKind ?? EmptyEnergyByKind;
        public IReadOnlyDictionary<TileKind, float> TotalEnergyByKind => _totalEnergyByKind ?? EmptyEnergyByKind;
        public bool IsEmpty => HasPositiveEnergy(MatchEnergyByKind) == false
                               && HasPositiveEnergy(SpecialActivationEnergyByKind) == false;


        private static readonly IReadOnlyDictionary<TileKind, float> EmptyEnergyByKind =
            new Dictionary<TileKind, float>(0);

        private readonly IReadOnlyDictionary<TileKind, float> _matchEnergyByKind;
        private readonly IReadOnlyDictionary<TileKind, float> _specialActivationEnergyByKind;
        private readonly IReadOnlyDictionary<TileKind, float> _totalEnergyByKind;


        public EnergyGainBreakdown(IReadOnlyDictionary<TileKind, float> matchEnergyByKind)
            : this(matchEnergyByKind, null)
        {
            
        }

        public EnergyGainBreakdown(IReadOnlyDictionary<TileKind, float> matchEnergyByKind,
            IReadOnlyDictionary<TileKind, float> specialActivationEnergyByKind)
        {
            _matchEnergyByKind = CopyPositiveEnergy(matchEnergyByKind);
            _specialActivationEnergyByKind = CopyPositiveEnergy(specialActivationEnergyByKind);
            _totalEnergyByKind = CombineEnergy(_matchEnergyByKind, _specialActivationEnergyByKind);
        }

        private static IReadOnlyDictionary<TileKind, float> CopyPositiveEnergy(IReadOnlyDictionary<TileKind, float> energyByKind)
        {
            if (null == energyByKind || energyByKind.Count == 0)
                return EmptyEnergyByKind;

            var result = new Dictionary<TileKind, float>(energyByKind.Count);
            foreach (var pair in energyByKind)
            {
                if (pair.Value <= 0f)
                    continue;

                result[pair.Key] = pair.Value;
            }

            return result.Count > 0 ? result : EmptyEnergyByKind;
        }

        private static IReadOnlyDictionary<TileKind, float> CombineEnergy(IReadOnlyDictionary<TileKind, float> first,
            IReadOnlyDictionary<TileKind, float> second)
        {
            if ((null == first || first.Count == 0) && (null == second || second.Count == 0))
                return EmptyEnergyByKind;

            var result = new Dictionary<TileKind, float>();
            AddEnergy(result, first);
            AddEnergy(result, second);

            return result.Count > 0 ? result : EmptyEnergyByKind;
        }

        private static void AddEnergy(Dictionary<TileKind, float> result, IReadOnlyDictionary<TileKind, float> source)
        {
            if (null == source)
                return;

            foreach (var pair in source)
            {
                if (pair.Value <= 0f)
                    continue;

                result.TryGetValue(pair.Key, out var current);
                result[pair.Key] = current + pair.Value;
            }
        }

        private static bool HasPositiveEnergy(IReadOnlyDictionary<TileKind, float> energyByKind)
        {
            foreach (var pair in energyByKind)
                if (pair.Value > 0f)
                    return true;

            return false;
        }
    }
}