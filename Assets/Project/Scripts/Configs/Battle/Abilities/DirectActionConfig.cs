using System;
using Project.Scripts.Shared.Abilities;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class DirectActionConfig
    {
        [Tooltip("Тип мгновенного действия: урон, лечение или воскрешение")]
        [SerializeField] private DirectActionKind _kind;

        [Tooltip("Значение действия: урон, лечение или HP для воскрешения")]
        [SerializeField] private int _value;

        [Tooltip("К кому применяется это прямое действие")]
        [SerializeField] private UnitTargetingConfig _targeting;

        [Tooltip("Если включено, прямое действие попадает по вражескому аватару даже когда он защищен живой группой героев")]
        [SerializeField] private bool _ignoresAvatarGroupDefense;


        public bool IsConfigured => _kind != DirectActionKind.None && _value > 0;


        public DirectActionDefinition ToDefinition()
        {
            return new DirectActionDefinition(_kind, _value, ToTargetingDefinition(), _ignoresAvatarGroupDefense);
        }

        private Shared.Targeting.UnitTargetingDefinition ToTargetingDefinition()
        {
            return null != _targeting ? _targeting.ToDefinition() : default;
        }
    }
}