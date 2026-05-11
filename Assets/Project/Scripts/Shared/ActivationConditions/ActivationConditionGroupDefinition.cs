using System;
using System.Collections.Generic;

namespace Project.Scripts.Shared.ActivationConditions
{
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
}