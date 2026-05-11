using System;
using System.Collections.Generic;

namespace Project.Scripts.Services.Combat.Abilities
{
    public readonly struct AbilityEffectApplicationResult
    {
        public int DirectActionCount { get; }
        public int BuffApplicationCount { get; }
        public IReadOnlyList<AbilityDirectApplicationResult> DirectApplications =>
            _directApplications ?? Array.Empty<AbilityDirectApplicationResult>();
        public IReadOnlyList<AbilityStatsChangeResult> AbilityStatsChanges =>
            _abilityStatsChanges ?? Array.Empty<AbilityStatsChangeResult>();
        public bool WasChanged => DirectActionCount > 0 || BuffApplicationCount > 0;


        private readonly AbilityDirectApplicationResult[] _directApplications;
        private readonly AbilityStatsChangeResult[] _abilityStatsChanges;


        public AbilityEffectApplicationResult(int buffApplicationCount,
            IReadOnlyList<AbilityDirectApplicationResult> directApplications,
            IReadOnlyList<AbilityStatsChangeResult> abilityStatsChanges)
        {
            BuffApplicationCount = buffApplicationCount < 0 ? 0 : buffApplicationCount;
            _directApplications = CopyDirectApplications(directApplications);
            _abilityStatsChanges = CopyAbilityStatsChanges(abilityStatsChanges);
            DirectActionCount = _directApplications.Length;
        }

        private static AbilityDirectApplicationResult[] CopyDirectApplications(
            IReadOnlyList<AbilityDirectApplicationResult> directApplications)
        {
            if (null == directApplications || directApplications.Count == 0)
                return Array.Empty<AbilityDirectApplicationResult>();

            var result = new AbilityDirectApplicationResult[directApplications.Count];
            for (var i = 0; i < directApplications.Count; i++)
                result[i] = directApplications[i];

            return result;
        }

        private static AbilityStatsChangeResult[] CopyAbilityStatsChanges(
            IReadOnlyList<AbilityStatsChangeResult> abilityStatsChanges)
        {
            if (null == abilityStatsChanges || abilityStatsChanges.Count == 0)
                return Array.Empty<AbilityStatsChangeResult>();

            var result = new AbilityStatsChangeResult[abilityStatsChanges.Count];
            for (var i = 0; i < abilityStatsChanges.Count; i++)
                result[i] = abilityStatsChanges[i];

            return result;
        }
    }
}