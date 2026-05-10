using System;
using Project.Scripts.Shared.Passives;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class ActivationConditionGroupConfig
    {
        [Tooltip("All = должны выполниться все условия. Any = достаточно любого одного условия")]
        [SerializeField] private ActivationConditionGroupOperator _operator = ActivationConditionGroupOperator.All;

        [Tooltip("Список условий активации пассивки")]
        [SerializeField] private ActivationConditionConfig[] _conditions;


        public ActivationConditionGroupDefinition ToDefinition()
        {
            return new ActivationConditionGroupDefinition(_operator, ToConditionDefinitions());
        }

        private ActivationConditionDefinition[] ToConditionDefinitions()
        {
            if (null == _conditions || _conditions.Length == 0)
                return Array.Empty<ActivationConditionDefinition>();

            var result = new ActivationConditionDefinition[_conditions.Length];
            for (var i = 0; i < _conditions.Length; i++)
                result[i] = null != _conditions[i] ? _conditions[i].ToDefinition() : default;

            return result;
        }
    }
}