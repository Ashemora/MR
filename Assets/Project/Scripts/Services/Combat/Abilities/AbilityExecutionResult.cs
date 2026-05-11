using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Abilities
{
    public readonly struct AbilityExecutionResult
    {
        public bool WasExecuted { get; }
        public UnitDescriptor Source { get; }
        public UnitDescriptor PrimaryTarget { get; }
        public UnitActionType ActionType { get; }
        public int ActionValue { get; }
        public long OccurredAtTick { get; }
        public bool BuffsChanged { get; }
        public IReadOnlyList<AbilityExecutionApplicationResult> DirectApplications =>
            _directApplications ?? Array.Empty<AbilityExecutionApplicationResult>();
        public IReadOnlyList<AbilityStatsChangeResult> AbilityStatsChanges =>
            _abilityStatsChanges ?? Array.Empty<AbilityStatsChangeResult>();


        private readonly AbilityExecutionApplicationResult[] _directApplications;
        private readonly AbilityStatsChangeResult[] _abilityStatsChanges;


        public AbilityExecutionResult(bool wasExecuted, UnitDescriptor source, UnitDescriptor primaryTarget,
            UnitActionType actionType, int actionValue, long occurredAtTick, bool buffsChanged,
            IReadOnlyList<AbilityExecutionApplicationResult> directApplications,
            IReadOnlyList<AbilityStatsChangeResult> abilityStatsChanges)
        {
            WasExecuted = wasExecuted;
            Source = source;
            PrimaryTarget = primaryTarget;
            ActionType = actionType;
            ActionValue = actionValue < 0 ? 0 : actionValue;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
            BuffsChanged = buffsChanged;
            _directApplications = CopyDirectApplications(directApplications);
            _abilityStatsChanges = CopyAbilityStatsChanges(abilityStatsChanges);
        }

        private static AbilityExecutionApplicationResult[] CopyDirectApplications(
            IReadOnlyList<AbilityExecutionApplicationResult> directApplications)
        {
            if (null == directApplications || directApplications.Count == 0)
                return Array.Empty<AbilityExecutionApplicationResult>();

            var result = new AbilityExecutionApplicationResult[directApplications.Count];
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