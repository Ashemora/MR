using System;
using Project.Scripts.Shared.Passives;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class UnitTargetingConfig
    {
        [Tooltip("Откуда брать цель: Self = сам владелец пассивки, ByRelation = искать цели по отношению к владельцу")]
        [SerializeField] private UnitTargetScope _scope = UnitTargetScope.Self;

        [Tooltip("Кого выбирать при Scope = ByRelation: союзников, врагов или всех. При Self не используется")]
        [SerializeField] private UnitTargetRelation _relation = UnitTargetRelation.Allies;

        [Tooltip("Какие типы юнитов участвуют в выборе")]
        [SerializeField] private UnitTargetKind _unitKind = UnitTargetKind.Units;

        [Tooltip("Включать владельца пассивки в пул целей")]
        [SerializeField] private bool _includeOwner = true;

        [Tooltip("Как выбрать цели из подходящего пула")]
        [SerializeField] private UnitTargetSelectionMode _selectionMode = UnitTargetSelectionMode.All;

        [Tooltip("Дополнительные фильтры целей")]
        [SerializeField] private UnitTargetFilter[] _filters;


        public UnitTargetingDefinition ToDefinition()
        {
            return new UnitTargetingDefinition(_scope, _relation, _unitKind, _includeOwner, _selectionMode,
                _filters ?? Array.Empty<UnitTargetFilter>());
        }
    }
}