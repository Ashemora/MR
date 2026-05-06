using System;
using System.Collections.Generic;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Tiles;

namespace Project.Scripts.Shared.Passives
{
    public readonly struct ActivationConditionDefinition
    {
        public ActivationConditionKind Kind { get; }
        public ActivationConditionSubject Subject { get; }
        public int RequiredCount { get; }
        public float WindowSeconds { get; }
        public bool IsConfigured => Kind != ActivationConditionKind.None
                                    && (RequiresWindow(Kind) == false || WindowSeconds > 0f);


        public ActivationConditionDefinition(ActivationConditionKind kind, ActivationConditionSubject subject,
            int requiredCount, float windowSeconds = 0f)
        {
            Kind = kind;
            Subject = subject;
            RequiredCount = requiredCount < 1 ? 1 : requiredCount;
            WindowSeconds = windowSeconds < 0f ? 0f : windowSeconds;
        }

        private static bool RequiresWindow(ActivationConditionKind kind)
        {
            return kind is ActivationConditionKind.UnitActivationsInTimeWindow
                or ActivationConditionKind.SlotKindMatchesInTimeWindow
                or ActivationConditionKind.EnemyHeroDefeatsInTimeWindow;
        }
    }

    public readonly struct ActivationConditionGroupDefinition
    {
        public ActivationConditionGroupOperator Operator { get; }
        public IReadOnlyList<ActivationConditionDefinition> Conditions => _conditions ?? Array.Empty<ActivationConditionDefinition>();
        public bool IsConfigured => Conditions.Count > 0;


        private readonly ActivationConditionDefinition[] _conditions;


        public ActivationConditionGroupDefinition(ActivationConditionGroupOperator @operator,
            IReadOnlyList<ActivationConditionDefinition> conditions)
        {
            Operator = @operator;
            _conditions = CopyConfiguredConditions(conditions);
        }

        private static ActivationConditionDefinition[] CopyConfiguredConditions(IReadOnlyList<ActivationConditionDefinition> conditions)
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
        public TileKind TileKind { get; }
        public float Amount { get; }
        public long OccurredAtTick { get; }


        public ActivationConditionEvent(ActivationConditionKind kind, UnitDescriptor source, float amount = 1f,
            long occurredAtTick = 0)
        {
            Kind = kind;
            Source = source;
            Side = source.Side;
            TileKind = TileKind.None;
            Amount = amount <= 0f ? 0f : amount;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }

        public ActivationConditionEvent(ActivationConditionKind kind, BattleSide side, float amount,
            TileKind tileKind = TileKind.None, long occurredAtTick = 0)
        {
            Kind = kind;
            Source = default;
            Side = side;
            TileKind = tileKind;
            Amount = amount <= 0f ? 0f : amount;
            OccurredAtTick = occurredAtTick < 0 ? 0 : occurredAtTick;
        }
    }
    
    
    public enum ActivationConditionKind
    {
        None,
        AbilityActivated,
        MatchEnergyCollected,
        MatchesCollected,
        LineRuneUsed,
        BombUsed,
        StormUsed,
        SlotKindMatchesCollected,
        UnitActivationsInTimeWindow,
        SlotKindMatchesInTimeWindow,
        EnemyHeroDefeatsInTimeWindow
    }

    public enum ActivationConditionSubject
    {
        Owner,
        OwnerSide,
        OwnerSlotKind,
        OpponentSide
    }

    public enum ActivationConditionGroupOperator
    {
        All,
        Any
    }
}