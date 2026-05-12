using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class ActiveAbilityConfig
    {
        [Tooltip("Отображаемое имя активной способности")]
        [InspectorName("Ability Display Name")]
        [SerializeField] private string _displayName;

        [Tooltip("Стоимость активации из общего пула энергии стороны")]
        [SerializeField] private int _activationEnergyCost = 10;

        [Tooltip("Кулдаун повторной активации в секундах")]
        [SerializeField] private float _activationCooldownSeconds = 3f;

        [Tooltip("Прямое действие активки: урон, лечение или воскрешение. Не задано - активка только применяет баффы")]
        [SerializeField] private DirectActionConfig _directAction;

        [Tooltip("Дополнительные баффы, накладываемые при активации. Каждая запись имеет собственный таргетинг")]
        [SerializeField] private BuffEntryConfig[] _buffEntries;


        public string DisplayName => _displayName;
        public int ActivationEnergyCost => _activationEnergyCost;
        public float ActivationCooldownSeconds => _activationCooldownSeconds;
        public DirectActionConfig DirectAction => _directAction;
        public BuffEntryConfig[] BuffEntries => _buffEntries;
        public bool IsConfigured => HasConfiguredDirectAction() || HasConfiguredBuffEntries();


        public ActiveAbilityDefinition ToDefinition()
        {
            return new ActiveAbilityDefinition(_displayName, _activationEnergyCost, _activationCooldownSeconds,
                ToDirectActionDefinition(), ToBuffEntryDefinitions());
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

        private bool HasConfiguredDirectAction()
        {
            return null != _directAction && _directAction.IsConfigured;
        }

        private bool HasConfiguredBuffEntries()
        {
            if (null == _buffEntries)
                return false;

            for (var i = 0; i < _buffEntries.Length; i++)
                if (null != _buffEntries[i] && _buffEntries[i].IsConfigured)
                    return true;

            return false;
        }
    }
}