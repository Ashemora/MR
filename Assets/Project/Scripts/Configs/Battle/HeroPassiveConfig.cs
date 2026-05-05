using System;
using Project.Scripts.Shared.Passives;
using UnityEngine;

namespace Project.Scripts.Configs.Battle
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
        [Tooltip("Что пассивка накладывает при срабатывании")]
        [SerializeField] private PassiveEffectEntryConfig[] _effectEntries;

        [Tooltip("Может ли пассивная способность активироваться повторно, пока ее эффект уже активен")]
        [SerializeField] private bool _canActivateWhileActive;

        [Tooltip("Максимум активаций за бой. Ноль означает без ограничений")]
        [SerializeField] private int _maxActivations;

        
        public string Id => _id;
        public string DisplayName => _displayName;
        public ActivationConditionGroupConfig ActivationConditionGroup => _activationConditionGroup;
        public PassiveEffectEntryConfig[] EffectEntries => _effectEntries;
        public bool CanActivateWhileActive => _canActivateWhileActive;
        public int MaxActivations => _maxActivations;


        public HeroPassiveDefinition ToDefinition()
        {
            return new HeroPassiveDefinition(_displayName, ToActivationConditionGroupDefinition(),
                ToEffectEntryDefinitions(), _canActivateWhileActive, _maxActivations);
        }

        private ActivationConditionGroupDefinition ToActivationConditionGroupDefinition()
        {
            return null != _activationConditionGroup
                ? _activationConditionGroup.ToDefinition()
                : default;
        }

        private PassiveEffectEntryDefinition[] ToEffectEntryDefinitions()
        {
            if (null == _effectEntries || _effectEntries.Length == 0)
                return Array.Empty<PassiveEffectEntryDefinition>();

            var result = new PassiveEffectEntryDefinition[_effectEntries.Length];
            for (var i = 0; i < _effectEntries.Length; i++)
                result[i] = null != _effectEntries[i] ? _effectEntries[i].ToDefinition() : default;

            return result;
        }
    }
    
    
    [Serializable]
    public class ActivationConditionConfig
    {
        [Tooltip("Условие активации: AbilityActivated = владелец активировал способность; MatchEnergyCollected = сторона владельца набрала новую энергию за текущую Match phase")]
        [SerializeField] private ActivationConditionKind _kind = ActivationConditionKind.AbilityActivated;

        [Tooltip("Кто должен вызвать условие: Owner для AbilityActivated; OwnerSide для MatchEnergyCollected")]
        [SerializeField] private ActivationConditionSubject _subject = ActivationConditionSubject.Owner;

        [Tooltip("Порог условия: для AbilityActivated = количество активаций; для MatchEnergyCollected = сколько новой энергии реально добавилось в общий пул за текущую Match phase")]
        [SerializeField] private int _requiredCount = 1;


        public ActivationConditionDefinition ToDefinition()
        {
            return new ActivationConditionDefinition(_kind, NormalizeSubject(_kind, _subject), _requiredCount);
        }

        private static ActivationConditionSubject NormalizeSubject(ActivationConditionKind kind,
            ActivationConditionSubject subject)
        {
            return kind == ActivationConditionKind.MatchEnergyCollected
                ? ActivationConditionSubject.OwnerSide
                : subject;
        }
    }

    [Serializable]
    public class ActivationConditionGroupConfig
    {
        [Tooltip("All = должны выполниться все условия. Any = достаточно любого одного условия")]
        [SerializeField] private ActivationConditionGroupOperator _operator = ActivationConditionGroupOperator.All;

        [Tooltip("Список условий активации пассивки")]
        [SerializeField] private ActivationConditionConfig[] _conditions;


        public ActivationConditionGroupDefinition ToDefinition()
        {
            return new ActivationConditionGroupDefinition(_operator, ToConditionDefinitions());
        }

        private ActivationConditionDefinition[] ToConditionDefinitions()
        {
            if (_conditions == null || _conditions.Length == 0)
                return Array.Empty<ActivationConditionDefinition>();

            var result = new ActivationConditionDefinition[_conditions.Length];
            for (var i = 0; i < _conditions.Length; i++)
                result[i] = _conditions[i] != null ? _conditions[i].ToDefinition() : default;

            return result;
        }
    }


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

    [Serializable]
    public class BuffEffectConfig
    {
        [Tooltip("Что меняет баф: ModifyAbilityPower = силу способности героя, ModifyActivationEnergyCost = стоимость активации, ModifyMatchEnergyBySlotKind = энергию от тайлов цвета слота владельца, NextAttackDamage = урон следующей атаки цели")]
        [SerializeField] private BuffKind _kind;

        [Tooltip("Как именно меняется числовой параметр")]
        [SerializeField] private BuffModifierOperation _operation;

        [Tooltip("Значение бафа")]
        [SerializeField] private float _value;

        [Tooltip("Когда баф снимается: Battle = до конца боя, Rounds = через указанное число раундов, NextAttack = после следующей атаки цели")]
        [SerializeField] private BuffLifetimeKind _lifetimeKind = BuffLifetimeKind.Battle;

        [Tooltip("Сколько раундов действует баф, если выбран lifetime Rounds")]
        [SerializeField] private int _durationRounds;

        [Tooltip("Stack = повторное наложение усиливает баф. IgnoreNew = новый такой же баф игнорируется, пока старый активен")]
        [SerializeField] private BuffStackingMode _stackingMode = BuffStackingMode.Stack;


        public BuffDefinition ToDefinition()
        {
            return new BuffDefinition(_kind, _operation, _value, _lifetimeKind, _durationRounds, _stackingMode);
        }
    }

    [Serializable]
    public class PassiveEffectEntryConfig
    {
        [Tooltip("Кого выбирает эта запись эффекта")]
        [SerializeField] private UnitTargetingConfig _targeting;

        [Tooltip("Какой баф накладывается на выбранные цели")]
        [SerializeField] private BuffEffectConfig _buff;


        public PassiveEffectEntryDefinition ToDefinition()
        {
            return new PassiveEffectEntryDefinition(
                null != _targeting ? _targeting.ToDefinition() : default,
                null != _buff ? _buff.ToDefinition() : default);
        }
    }
}