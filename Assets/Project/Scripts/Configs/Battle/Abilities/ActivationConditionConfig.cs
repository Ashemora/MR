using System;
using UnityEngine;
using Project.Scripts.Shared.ActivationConditions;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class ActivationConditionConfig
    {
        [Tooltip("Условие активации: UnitActivationsInTimeWindow = сторона владельца активировала героев или аватара за WindowSeconds; EnemyHeroDefeatsInTimeWindow = герои противника умирали за WindowSeconds; MatchEnergyCollected = сторона владельца набрала новую энергию за текущую Match phase; MatchesCollected = сторона владельца собрала реальные матчи за текущую Match phase; SlotKindMatchesCollected = сторона владельца собрала матчи цвета ячейки героя за текущую Match phase; SlotKindMatchesInTimeWindow = сторона владельца собрала матчи цвета ячейки героя за WindowSeconds; LineRuneUsed/BombUsed/StormUsed = сторона владельца использовала соответствующий спецтайл за бой")]
        [SerializeField] private ActivationConditionKind _kind = ActivationConditionKind.MatchEnergyCollected;

        [Tooltip("Кто должен вызвать условие: OwnerSide для UnitActivationsInTimeWindow, MatchEnergyCollected, MatchesCollected, LineRuneUsed, BombUsed и StormUsed; OwnerSlotKind для SlotKindMatchesCollected и SlotKindMatchesInTimeWindow; OpponentSide для EnemyHeroDefeatsInTimeWindow")]
        [SerializeField] private ActivationConditionSubject _subject = ActivationConditionSubject.OwnerSide;

        [Tooltip("Порог условия: для UnitActivationsInTimeWindow = сколько героев/аватаров нужно активировать в окне; для EnemyHeroDefeatsInTimeWindow = сколько героев противника должно умереть в окне; для MatchEnergyCollected = сколько новой энергии реально добавилось в общий пул; для MatchesCollected = количество MatchResult за текущую Match phase; для SlotKindMatchesCollected = количество MatchResult цвета ячейки героя за текущую Match phase; для SlotKindMatchesInTimeWindow = сколько MatchResult цвета ячейки героя нужно собрать в окне; для LineRuneUsed/BombUsed/StormUsed = количество срабатываний спецтайла за бой")]
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
}