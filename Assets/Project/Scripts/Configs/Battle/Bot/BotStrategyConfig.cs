using Project.Scripts.Shared.Bot;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Bot
{
    [CreateAssetMenu(fileName = "BotStrategyConfig", menuName = "Configs/Bot Strategy Config")]
    public class BotStrategyConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Имя стратегии, отображаемое в DEV-инструментах")]
        [SerializeField] private string _displayName = "Balanced";

        [Header("Action Preferences")]
        [Tooltip("Базовая склонность выбирать действия с уроном")]
        [SerializeField] private float _damagePreference = 1f;

        [Tooltip("Базовая склонность выбирать лечение")]
        [SerializeField] private float _healPreference = 1f;

        [Tooltip("Базовая склонность выбирать воскрешение")]
        [SerializeField] private float _resurrectPreference = 1f;

        [Tooltip("Базовая склонность выбирать поддержку и баффы")]
        [SerializeField] private float _supportPreference = 1f;

        [Header("Tactical Weights")]
        [Tooltip("Насколько стратегия ценит добивание вражеских юнитов")]
        [SerializeField] private float _finishEnemyWeight = 1f;

        [Tooltip("Насколько стратегия ценит уничтожение защитной группы аватара")]
        [SerializeField] private float _breakDefenseWeight = 1f;

        [Tooltip("Насколько стратегия давит по открытому аватару противника")]
        [SerializeField] private float _attackExposedAvatarWeight = 1f;

        [Tooltip("Насколько стратегия ценит защиту собственного аватара")]
        [SerializeField] private float _protectOwnAvatarWeight = 1f;

        [Tooltip("Насколько стратегия ценит лечение сильно раненых союзников")]
        [SerializeField] private float _healLowHpAllyWeight = 1f;

        [Tooltip("Насколько стратегия ценит возвращение павших союзников")]
        [SerializeField] private float _resurrectAllyWeight = 1f;

        [Header("Waste Avoidance")]
        [Tooltip("Штраф за лечение сверх недостающего HP")]
        [SerializeField] private float _avoidOverhealWeight = 1f;

        [Tooltip("Штраф за урон сверх оставшегося HP цели")]
        [SerializeField] private float _avoidOverkillWeight = 0.5f;

        [Tooltip("Штраф за лечение союзника, у которого висит щит (щит уже защищает от урона - лечение менее срочно)")]
        [SerializeField] private float _healThroughShieldPenaltyWeight = 1f;


        public string DisplayName => _displayName;


        public BotUtilityProfile ToProfile()
        {
            return new BotUtilityProfile(_damagePreference, _healPreference, _resurrectPreference,
                _supportPreference, _finishEnemyWeight, _breakDefenseWeight, _attackExposedAvatarWeight,
                _protectOwnAvatarWeight, _healLowHpAllyWeight, _resurrectAllyWeight, _avoidOverhealWeight,
                _avoidOverkillWeight, _healThroughShieldPenaltyWeight);
        }
    }
}