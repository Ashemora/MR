using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct ActivationConditionDefinition
    {
        public ActivationConditionKind Kind { get; }
        public ActivationConditionSubject Subject { get; }
        public int RequiredCount { get; }
        public bool IsConfigured => Kind != ActivationConditionKind.None;


        public ActivationConditionDefinition(ActivationConditionKind kind, ActivationConditionSubject subject,
            int requiredCount)
        {
            Kind = kind;
            Subject = subject;
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
        }
    }

    public readonly struct ActivationConditionGroupDefinition
    {
        public ActivationConditionGroupOperator Operator { get; }
        public IReadOnlyList<ActivationConditionDefinition> Conditions =>
            _conditions ?? Array.Empty<ActivationConditionDefinition>();
        public bool IsConfigured => Conditions.Count > 0;


        private readonly ActivationConditionDefinition[] _conditions;


        public ActivationConditionGroupDefinition(ActivationConditionGroupOperator @operator,
            IReadOnlyList<ActivationConditionDefinition> conditions)
        {
            Operator = @operator;
            _conditions = CopyConfiguredConditions(conditions);
        }

        private static ActivationConditionDefinition[] CopyConfiguredConditions(
            IReadOnlyList<ActivationConditionDefinition> conditions)
        {
            if (null == conditions || conditions.Count == 0)
                return Array.Empty<ActivationConditionDefinition>();

            var result = new List<ActivationConditionDefinition>(conditions.Count);
            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition.IsConfigured)
                    result.Add(condition);
            }

            return result.ToArray();
        }
    }

    public readonly struct ActivationConditionEvent
    {
        public ActivationConditionKind Kind { get; }
        public UnitDescriptor Source { get; }
        public BattleSide Side { get; }
        public float Amount { get; }


        public ActivationConditionEvent(ActivationConditionKind kind, UnitDescriptor source, float amount = 1f)
        {
            Kind = kind;
            Source = source;
            Side = source.Side;
            Amount = amount <= 0f ? 0f : amount;
        }

        public ActivationConditionEvent(ActivationConditionKind kind, BattleSide side, float amount)
        {
            Kind = kind;
            Source = default;
            Side = side;
            Amount = amount <= 0f ? 0f : amount;
        }
    }
    
    
    public enum ActivationConditionKind
    {
        None,
        AbilityActivated,
        MatchEnergyCollected,
        MatchesCollected
    }

    public enum ActivationConditionSubject
    {
        Owner,
        OwnerSide
    }

    public enum ActivationConditionGroupOperator
    {
        All,
        Any
    }
}