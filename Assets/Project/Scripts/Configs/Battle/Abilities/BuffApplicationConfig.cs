using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class BuffApplicationConfig
    {
        [Tooltip("Какой длящийся баф, статус или модификатор накладывается")]
        [SerializeField] private BuffEffectConfig _buff;

        [Tooltip("Длительность в секундах для будущих duration-based эффектов. Ноль означает legacy lifetime из BuffEffectConfig")]
        [SerializeField] private float _durationSeconds;


        public bool IsConfigured => null != _buff && _buff.ToDefinition().IsConfigured;


        public BuffApplicationDefinition ToDefinition()
        {
            return new BuffApplicationDefinition(null != _buff ? _buff.ToDefinition() : default, _durationSeconds);
        }
    }
}