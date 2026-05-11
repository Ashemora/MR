using System.Collections.Generic;

namespace Project.Scripts.Shared.Energy
{
    public readonly struct RoundEnergyCapSchedule
    {
        public const int DefaultCap = 150;

        
        public bool HasExplicitCaps => _caps is { Count: > 0 };
        
        
        private readonly IReadOnlyList<int> _caps;


        public RoundEnergyCapSchedule(IReadOnlyList<int> caps)
        {
            _caps = caps;
        }

        public int GetCapForRound(int round)
        {
            if (false == HasExplicitCaps)
                return DefaultCap;

            var index = round < 1 ? 0 : round - 1;
            if (index >= _caps.Count)
                index = _caps.Count - 1;

            var cap = _caps[index];
            
            return cap < 1 ? 1 : cap;
        }
    }
}