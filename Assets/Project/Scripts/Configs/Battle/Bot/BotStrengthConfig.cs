using Project.Scripts.Shared.Bot;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Bot
{
    [CreateAssetMenu(fileName = "BotStrengthConfig", menuName = "Configs/Bot Strength Config")]
    public class BotStrengthConfig : ScriptableObject
    {
        [Header("Debug")]
        [Tooltip("Снимите флажок для отключения бота во время записи аналитики; не влияет на регистрацию")]
        [SerializeField] private bool _enabled = true;

        [Header("Identity")]
        [Tooltip("Имя, отображаемое для противника в боевом UI")]
        [SerializeField] private string _opponentName = "Enemy";

        [Header("Hero Activation")]
        [Tooltip("Секунды между проверками готовности вражеских героев к активации")]
        [SerializeField] private float _heroActivationCheckInterval = 0.8f;

        [Tooltip("Минимальное количество секунд, которое бот ждёт после зарядки героя перед активацией (симулирует реакцию человека)")]
        [SerializeField] private float _minHeroActivationDelay = 1.0f;

        [Tooltip("Максимальное количество секунд ожидания бота после зарядки героя перед активацией")]
        [SerializeField] private float _maxHeroActivationDelay = 4.0f;

        [Header("Match Energy Simulation")]
        [Tooltip("Секунды между тиками симуляции матчей, пополняющих общий запас энергии врага")]
        [SerializeField] private float _matchEnergyTickInterval = 2f;

        [Tooltip("Базовое количество энергии за тик симуляции матчей (используется для имитации каскадов)")]
        [SerializeField] private int _baseMatchEnergyPerTick = 6;

        [Tooltip("Вариативность количества энергии (множитель от базовой: 0.5-1.5)")]
        [SerializeField] private float _cascadeVariation = 0.3f;

        [Header("Cascade Simulation")]
        [Tooltip("Вероятность 'хорошего каскада' (уровень 2) за тик")]
        [SerializeField] private float _goodCascadeChance = 0.12f;

        [Tooltip("Вероятность 'отличного каскада' (уровень 3+) за тик")]
        [SerializeField] private float _greatCascadeChance = 0.04f;

        [Tooltip("Множитель энергии для хорошего каскада")]
        [SerializeField] private float _goodCascadeMultiplier = 1.20f;

        [Tooltip("Множитель энергии для отличного каскада")]
        [SerializeField] private float _greatCascadeMultiplier = 1.40f;

        [Tooltip("Минимальное количество секунд ожидания бота перед активацией аватара после готовности")]
        [SerializeField] private float _minAvatarActivationDelay = 0.5f;

        [Tooltip("Максимальное количество секунд ожидания бота перед активацией аватара после готовности")]
        [SerializeField] private float _maxAvatarActivationDelay = 2.0f;

        [Header("Decision Quality")]
        [Tooltip("Сколько лучших вариантов рассматривает бот при выборе действия. Чем больше значение, тем менее стабилен выбор.")]
        [SerializeField] private int _decisionTopCandidateCount = 3;

        [Tooltip("Случайный шум, добавляемый к оценке вариантов Utility AI.")]
        [SerializeField] private float _decisionRandomNoise = 0.1f;

        [Tooltip("Шанс намеренно выбрать не лучший допустимый вариант, имитируя человеческую ошибку.")]
        [Range(0f, 1f)]
        [SerializeField] private float _decisionMistakeChance = 0.05f;

        [Tooltip("Температура weighted-random выбора: выше - больше разброс, ниже - ближе к лучшему варианту.")]
        [SerializeField] private float _decisionTemperature = 1f;

        [Tooltip("Минимальная utility-оценка для активации способности после внедрения общего decision pipeline.")]
        [SerializeField] private float _decisionMinScoreToAct = 0.1f;


        public bool Enabled => _enabled;
        public string OpponentName => _opponentName;
        public float MinHeroActivationDelay => _minHeroActivationDelay;
        public float MaxHeroActivationDelay => _maxHeroActivationDelay;
        public float HeroActivationCheckInterval => _heroActivationCheckInterval;
        public float MatchEnergyTickInterval => _matchEnergyTickInterval;
        public int BaseMatchEnergyPerTick => _baseMatchEnergyPerTick;
        public float CascadeVariation => _cascadeVariation;
        public float GoodCascadeChance => _goodCascadeChance;
        public float GreatCascadeChance => _greatCascadeChance;
        public float GoodCascadeMultiplier => _goodCascadeMultiplier;
        public float GreatCascadeMultiplier => _greatCascadeMultiplier;
        public float MinAvatarActivationDelay => _minAvatarActivationDelay;
        public float MaxAvatarActivationDelay => _maxAvatarActivationDelay;
        public int DecisionTopCandidateCount => _decisionTopCandidateCount;
        public float DecisionRandomNoise => _decisionRandomNoise;
        public float DecisionMistakeChance => _decisionMistakeChance;
        public float DecisionTemperature => _decisionTemperature;
        public float DecisionMinScoreToAct => _decisionMinScoreToAct;


        public BotDecisionQualitySettings ToDecisionQualitySettings()
        {
            return new BotDecisionQualitySettings(_decisionTopCandidateCount, _decisionRandomNoise,
                _decisionMistakeChance, _decisionTemperature, _decisionMinScoreToAct);
        }
    }
}