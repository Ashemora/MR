using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class DirectActionConfig
    {
        [Tooltip("Тип мгновенного действия")]
        [SerializeField] private DirectActionKind _kind;

        [Tooltip("Значение действия: урон, лечение или HP для воскрешения")]
        [SerializeField] private int _value;


        public bool IsConfigured => _kind != DirectActionKind.None && _value > 0;


        public DirectActionDefinition ToDefinition()
        {
            return new DirectActionDefinition(_kind, _value);
        }
    }
}