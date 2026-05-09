using System;
using Project.Scripts.Shared.Abilities;
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
        [Tooltip("Что пассивка применяет при срабатывании")]
        [SerializeField] private AbilityEffectEntryConfig[] _abilityEffectEntries;

        [Tooltip("Может ли пассивная способность активироваться повторно, пока ее эффект уже активен")]
        [SerializeField] private bool _canActivateWhileActive;

        [Tooltip("Максимум активаций за бой. Ноль означает без ограничений")]
        [SerializeField] private int _maxActivations;

        public HeroPassiveDefinition ToDefinition()
        {
            return new HeroPassiveDefinition(_displayName, ToActivationConditionGroupDefinition(),
                ToAbilityEffectEntryDefinitions(), _canActivateWhileActive, _maxActivations);
        }

        public PassiveAbilityDefinition ToPassiveAbilityDefinition()
        {
            return new PassiveAbilityDefinition(_displayName, ToActivationConditionGroupDefinition(),
                ToAbilityEffectEntryDefinitions(), _canActivateWhileActive, _maxActivations);
        }

        private ActivationConditionGroupDefinition ToActivationConditionGroupDefinition()
        {
            return null != _activationConditionGroup ? _activationConditionGroup.ToDefinition() : default;
        }

        private AbilityEffectEntryDefinition[] ToAbilityEffectEntryDefinitions()
        {
            if (null == _abilityEffectEntries || _abilityEffectEntries.Length == 0)
                return Array.Empty<AbilityEffectEntryDefinition>();

            var result = new AbilityEffectEntryDefinition[_abilityEffectEntries.Length];
            for (var i = 0; i < _abilityEffectEntries.Length; i++)
                result[i] = null != _abilityEffectEntries[i] ? _abilityEffectEntries[i].ToDefinition() : default;

            return result;
        }
    }
    
    
    [Serializable]
    public class ActivationConditionConfig
    {
        [Tooltip("Условие активации: AbilityActivated = владелец активировал способность; UnitActivationsInTimeWindow = сторона владельца активировала героев или аватара за WindowSeconds; EnemyHeroDefeatsInTimeWindow = герои противника умирали за WindowSeconds; MatchEnergyCollected = сторона владельца набрала новую энергию за текущую Match phase; MatchesCollected = сторона владельца собрала реальные матчи за текущую Match phase; SlotKindMatchesCollected = сторона владельца собрала матчи цвета ячейки героя за текущую Match phase; SlotKindMatchesInTimeWindow = сторона владельца собрала матчи цвета ячейки героя за WindowSeconds; LineRuneUsed/BombUsed/StormUsed = сторона владельца использовала соответствующий спецтайл за бой")]
        [SerializeField] private ActivationConditionKind _kind = ActivationConditionKind.AbilityActivated;

        [Tooltip("Кто должен вызвать условие: Owner для AbilityActivated; OwnerSide для UnitActivationsInTimeWindow, MatchEnergyCollected, MatchesCollected, LineRuneUsed, BombUsed и StormUsed; OwnerSlotKind для SlotKindMatchesCollected и SlotKindMatchesInTimeWindow; OpponentSide для EnemyHeroDefeatsInTimeWindow")]
        [SerializeField] private ActivationConditionSubject _subject = ActivationConditionSubject.Owner;

        [Tooltip("Порог условия: для AbilityActivated = количество активаций; для UnitActivationsInTimeWindow = сколько героев/аватаров нужно активировать в окне; для EnemyHeroDefeatsInTimeWindow = сколько героев противника должно умереть в окне; для MatchEnergyCollected = сколько новой энергии реально добавилось в общий пул; для MatchesCollected = количество MatchResult за текущую Match phase; для SlotKindMatchesCollected = количество MatchResult цвета ячейки героя за текущую Match phase; для SlotKindMatchesInTimeWindow = сколько MatchResult цвета ячейки героя нужно собрать в окне; для LineRuneUsed/BombUsed/StormUsed = количество срабатываний спецтайла за бой")]
        [SerializeField] private int _requiredCount = 1;

        [Tooltip("Временное окно в секундах для условий, которым нужно значение времени. Для InTimeWindow условий должно быть больше 0, иначе условие считается невалидным")]
        [SerializeField] private float _windowSeconds;


        public ActivationConditionDefinition ToDefinition()
        {
            return new ActivationConditionDefinition(_kind, NormalizeSubject(_kind, _subject), _requiredCount,
                _windowSeconds);
        }

        private static ActivationConditionSubject NormalizeSubject(ActivationConditionKind kind,
            ActivationConditionSubject subject)
        {
            if (kind is ActivationConditionKind.SlotKindMatchesCollected
                or ActivationConditionKind.SlotKindMatchesInTimeWindow)
                return ActivationConditionSubject.OwnerSlotKind;

            if (kind == ActivationConditionKind.EnemyHeroDefeatsInTimeWindow)
                return ActivationConditionSubject.OpponentSide;

            return kind is ActivationConditionKind.UnitActivationsInTimeWindow
                    or ActivationConditionKind.MatchEnergyCollected
                    or ActivationConditionKind.MatchesCollected
                    or ActivationConditionKind.LineRuneUsed
                    or ActivationConditionKind.BombUsed
                    or ActivationConditionKind.StormUsed
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
            if (null == _conditions || _conditions.Length == 0)
                return Array.Empty<ActivationConditionDefinition>();

            var result = new ActivationConditionDefinition[_conditions.Length];
            for (var i = 0; i < _conditions.Length; i++)
                result[i] = null != _conditions[i] ? _conditions[i].ToDefinition() : default;

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
        [Tooltip("Что меняет баф: ModifyAbilityPower = силу способности героя, ModifyActivationEnergyCost = стоимость активации, ModifyActivationCooldown = длительность cooldown активации героя, ModifyMatchEnergyBySlotKind = энергию от тайлов цвета слота владельца, ModifySpecialTileActivationEnergy = энергию от непосредственной активации спецтайлов, ModifyBombRadius = радиус действия бомб стороны цели, RepeatAbilityApplication = количество дополнительных применений способности героя к той же цели, NextAttackDamage = урон следующей атаки цели, ApplyAbilityToAdditionalTargets = применить способность героя к дополнительным подходящим целям, ModifyLineRuneThickness = дополнительные соседние ряды/столбцы при активации LineRune, ResurrectOnDeath = воскрешает героя-цель на Value HP при попытке умереть. В Burndown игнорируется. Рекомендуется использовать с MaxActivations=1 на пассивке, чтобы способность стала недоступной после воскрешения")]
        [SerializeField] private BuffKind _kind;

        [Tooltip("Как именно меняется числовой параметр")]
        [SerializeField] private BuffModifierOperation _operation;

        [Tooltip("Значение бафа")]
        [SerializeField] private float _value;

        [Tooltip("Когда баф снимается: Battle = до конца боя, NextAttack = после следующей атаки цели, NextActivation = после следующей активации цели, UntilEndOfNextMainPhase = до конца следующей Match/Hero фазы")]
        [SerializeField] private BuffLifetimeKind _lifetimeKind = BuffLifetimeKind.Battle;

        [Tooltip("Stack = повторное наложение усиливает баф. IgnoreNew = новый такой же баф игнорируется, пока старый активен")]
        [SerializeField] private BuffStackingMode _stackingMode = BuffStackingMode.Stack;


        public BuffDefinition ToDefinition()
        {
            return new BuffDefinition(_kind, _operation, _value, _lifetimeKind, _stackingMode);
        }
    }
}