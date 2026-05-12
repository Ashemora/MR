using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;
using Project.Scripts.Shared.ActivationConditions;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [CreateAssetMenu(fileName = "HeroPassiveConfig", menuName = "Configs/Battle/Hero Passive Config")]
    public class HeroPassiveConfig : ScriptableObject
    {
        [Tooltip("Стабильный id пассивки для будущего экспорта конфигов на сервер")]
        [SerializeField] private string _id;

        [Tooltip("Отображаемое имя пассивной способности")]
        [SerializeField] private string _displayName;

        [Space(10)]
        [Tooltip("Условия, при выполнении которых пассивка активируется")]
        [SerializeField] private ActivationConditionGroupConfig _activationConditionGroup;

        [Space(10)]
        [Tooltip("Прямое действие пассивки: урон, лечение или воскрешение. Не задано - пассивка только применяет баффы")]
        [SerializeField] private DirectActionConfig _directAction;

        [Tooltip("Дополнительные баффы, накладываемые при срабатывании. Каждая запись имеет собственный таргетинг")]
        [SerializeField] private BuffEntryConfig[] _buffEntries;

        [Tooltip("Может ли пассивная способность активироваться повторно, пока ее эффект уже активен")]
        [SerializeField] private bool _canActivateWhileActive;

        [Tooltip("Максимум активаций за бой. Ноль означает без ограничений")]
        [SerializeField] private int _maxActivations;


        public PassiveAbilityDefinition ToDefinition()
        {
            return new PassiveAbilityDefinition(_displayName, ToActivationConditionGroupDefinition(),
                ToDirectActionDefinition(), ToBuffEntryDefinitions(), _canActivateWhileActive, _maxActivations);
        }

        private ActivationConditionGroupDefinition ToActivationConditionGroupDefinition()
        {
            return null != _activationConditionGroup ? _activationConditionGroup.ToDefinition() : default;
        }

        private DirectActionDefinition ToDirectActionDefinition()
        {
            return null != _directAction ? _directAction.ToDefinition() : default;
        }

        private BuffEntryDefinition[] ToBuffEntryDefinitions()
        {
            if (null == _buffEntries || _buffEntries.Length == 0)
                return Array.Empty<BuffEntryDefinition>();

            var result = new BuffEntryDefinition[_buffEntries.Length];
            for (var i = 0; i < _buffEntries.Length; i++)
                result[i] = null != _buffEntries[i] ? _buffEntries[i].ToDefinition() : default;

            return result;
        }
    }
}