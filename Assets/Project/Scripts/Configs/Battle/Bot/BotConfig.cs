using Project.Scripts.Shared.Bot;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Bot
{
    [CreateAssetMenu(fileName = "BotConfig", menuName = "Configs/Bot Config")]
    public class BotConfig : ScriptableObject
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

        [Header("Hero Activation")]
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

        
        public BotSettings ToSettings()
        {
            return new BotSettings(_minAvatarActivationDelay, _maxAvatarActivationDelay);
        }
    }
}