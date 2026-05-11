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

        [Tooltip("Что активная способность применяет после успешной активации")]
        [SerializeField] private AbilityEffectEntryConfig[] _effectEntries;


        public string DisplayName => _displayName;
        public int ActivationEnergyCost => _activationEnergyCost;
        public float ActivationCooldownSeconds => _activationCooldownSeconds;
        public AbilityEffectEntryConfig[] EffectEntries => _effectEntries;
        public bool IsConfigured => HasConfiguredEffectEntries();


        public ActiveAbilityDefinition ToDefinition()
        {
            return new ActiveAbilityDefinition(_displayName, _activationEnergyCost, _activationCooldownSeconds,
                ToEffectEntryDefinitions());
        }

        private AbilityEffectEntryDefinition[] ToEffectEntryDefinitions()
        {
            if (null == _effectEntries || _effectEntries.Length == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new AbilityEffectEntryDefinition[_effectEntries.Length];
            for (var i = 0; i < _effectEntries.Length; i++)
                result[i] = null != _effectEntries[i] ? _effectEntries[i].ToDefinition() : default;

            return result;
        }

        private bool HasConfiguredEffectEntries()
        {
            if (null == _effectEntries)
                return false;

            for (var i = 0; i < _effectEntries.Length; i++)
                if (null != _effectEntries[i] && _effectEntries[i].IsConfigured)
                    return true;

            return false;
        }
    }
}