using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle
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

    [Serializable]
    public class AbilityEffectEntryConfig
    {
        [Tooltip("К кому применяется эта группа прямых действий и бафов")]
        [SerializeField] private UnitTargetingConfig _targeting;

        [Tooltip("Мгновенные действия: урон, лечение, воскрешение")]
        [SerializeField] private DirectActionConfig[] _directActions;

        [Tooltip("Наложение длящихся бафов, статусов и модификаторов")]
        [SerializeField] private BuffApplicationConfig[] _buffApplications;


        public UnitTargetingConfig Targeting => _targeting;
        public DirectActionConfig[] DirectActions => _directActions;
        public BuffApplicationConfig[] BuffApplications => _buffApplications;
        public bool IsConfigured => HasConfiguredDirectActions() || HasConfiguredBuffApplications();


        public AbilityEffectEntryDefinition ToDefinition()
        {
            return new AbilityEffectEntryDefinition(
                null != _targeting ? _targeting.ToDefinition() : default,
                ToDirectActionDefinitions(),
                ToBuffApplicationDefinitions());
        }

        private DirectActionDefinition[] ToDirectActionDefinitions()
        {
            if (null == _directActions || _directActions.Length == 0)
                return Array.Empty<DirectActionDefinition>();

            var result = new DirectActionDefinition[_directActions.Length];
            for (var i = 0; i < _directActions.Length; i++)
                result[i] = null != _directActions[i] ? _directActions[i].ToDefinition() : default;

            return result;
        }

        private BuffApplicationDefinition[] ToBuffApplicationDefinitions()
        {
            if (null == _buffApplications || _buffApplications.Length == 0)
                return Array.Empty<BuffApplicationDefinition>();

            var result = new BuffApplicationDefinition[_buffApplications.Length];
            for (var i = 0; i < _buffApplications.Length; i++)
                result[i] = null != _buffApplications[i] ? _buffApplications[i].ToDefinition() : default;

            return result;
        }

        private bool HasConfiguredDirectActions()
        {
            if (null == _directActions)
                return false;

            for (var i = 0; i < _directActions.Length; i++)
                if (null != _directActions[i] && _directActions[i].IsConfigured)
                    return true;

            return false;
        }

        private bool HasConfiguredBuffApplications()
        {
            if (null == _buffApplications)
                return false;

            for (var i = 0; i < _buffApplications.Length; i++)
                if (null != _buffApplications[i] && _buffApplications[i].IsConfigured)
                    return true;

            return false;
        }
    }

    [Serializable]
    public class DirectActionConfig
    {
        [Tooltip("Тип мгновенного действия")]
        [SerializeField] private DirectActionKind _kind;

        [Tooltip("Значение действия: урон, лечение или HP для воскрешения")]
        [SerializeField] private int _value;


        public DirectActionKind Kind => _kind;
        public int Value => _value;
        public bool IsConfigured => _kind != DirectActionKind.None && _value > 0;


        public DirectActionDefinition ToDefinition()
        {
            return new DirectActionDefinition(_kind, _value);
        }
    }

    [Serializable]
    public class BuffApplicationConfig
    {
        [Tooltip("Какой длящийся баф, статус или модификатор накладывается")]
        [SerializeField] private BuffEffectConfig _buff;

        [Tooltip("Длительность в секундах для будущих duration-based эффектов. Ноль означает legacy lifetime из BuffEffectConfig")]
        [SerializeField] private float _durationSeconds;


        public BuffEffectConfig Buff => _buff;
        public float DurationSeconds => _durationSeconds;
        public bool IsConfigured => null != _buff && _buff.ToDefinition().IsConfigured;


        public BuffApplicationDefinition ToDefinition()
        {
            return new BuffApplicationDefinition(null != _buff ? _buff.ToDefinition() : default, _durationSeconds);
        }
    }
}