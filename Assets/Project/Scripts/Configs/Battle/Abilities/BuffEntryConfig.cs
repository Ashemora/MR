using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class BuffEntryConfig
    {
        [Tooltip("К кому применяется этот набор баффов")]
        [SerializeField] private UnitTargetingConfig _targeting;

        [Tooltip("Баффы, статусы и модификаторы, накладываемые на выбранные цели")]
        [SerializeField] private BuffApplicationConfig[] _buffApplications;


        public bool IsConfigured => HasConfiguredBuffApplications();


        public BuffEntryDefinition ToDefinition()
        {
            return new BuffEntryDefinition(null != _targeting ? _targeting.ToDefinition() : default,
                ToBuffApplicationDefinitions());
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