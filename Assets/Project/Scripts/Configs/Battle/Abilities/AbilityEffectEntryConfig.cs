using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class AbilityEffectEntryConfig
    {
        [Tooltip("К кому применяется эта группа прямых действий и бафов")]
        [SerializeField] private UnitTargetingConfig _targeting;

        [Tooltip("Мгновенные действия: урон, лечение, воскрешение")]
        [SerializeField] private DirectActionConfig[] _directActions;

        [Tooltip("Наложение длящихся бафов, статусов и модификаторов")]
        [SerializeField] private BuffApplicationConfig[] _buffApplications;


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
}