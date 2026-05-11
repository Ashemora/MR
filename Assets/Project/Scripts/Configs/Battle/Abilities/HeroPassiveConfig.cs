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
        [Tooltip("Что пассивка применяет при срабатывании")]
        [SerializeField] private AbilityEffectEntryConfig[] _abilityEffectEntries;

        [Tooltip("Может ли пассивная способность активироваться повторно, пока ее эффект уже активен")]
        [SerializeField] private bool _canActivateWhileActive;

        [Tooltip("Максимум активаций за бой. Ноль означает без ограничений")]
        [SerializeField] private int _maxActivations;

        
        public PassiveAbilityDefinition ToDefinition()
        {
            return new PassiveAbilityDefinition(_displayName, ToActivationConditionGroupDefinition(),
                ToAbilityEffectEntryDefinitions(), _canActivateWhileActive, _maxActivations);
        }

        private ActivationConditionGroupDefinition ToActivationConditionGroupDefinition()
        {
            return null != _activationConditionGroup ? _activationConditionGroup.ToDefinition() : default;
        }

        private AbilityEffectEntryDefinition[] ToAbilityEffectEntryDefinitions()
        {
            if (null == _abilityEffectEntries || _abilityEffectEntries.Length == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new AbilityEffectEntryDefinition[_abilityEffectEntries.Length];
            for (var i = 0; i < _abilityEffectEntries.Length; i++)
                result[i] = null != _abilityEffectEntries[i] ? _abilityEffectEntries[i].ToDefinition() : default;

            return result;
        }
    }
}