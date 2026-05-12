using DG.Tweening;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Visuals
{
    [CreateAssetMenu(fileName = "BattleAnimationConfig", menuName = "Configs/Battle Animation Config")]
    public class BattleAnimationConfig : ScriptableObject
    {
        [Tooltip("Цвет вспышки аватара при получении урона")]
        [SerializeField] private Color _hitFlashColor = new(1f, 0.2f, 0.2f, 1f);

        [Tooltip("Общая длительность вспышки попадания в секундах (затухание к цвету + затухание обратно)")]
        [SerializeField] private float _hitFlashDuration = 0.3f;

        [Tooltip("Кривая ослабления для обеих фаз вспышки попадания")]
        [SerializeField] private Ease _hitFlashEase = Ease.InOutQuad;

        [Tooltip("Расстояние в мировых единицах, на которое двигается аватар/герой при отбрасывании")]
        [SerializeField] private float _knockbackDistance = 0.15f;

        [Tooltip("Общая длительность отбрасывания в секундах (отход + возврат)")]
        [SerializeField] private float _knockbackDuration = 0.3f;

        [Tooltip("Кривая ослабления для обеих фаз отбрасывания")]
        [SerializeField] private Ease _knockbackEase = Ease.OutQuad;

        [Header("HP Bar Animation")]
        [Tooltip("Задержка в секундах перед началом опустошения лаг-полосы после получения урона")]
        [SerializeField] private float _hpBarLagDelay = 0.2f;

        [Tooltip("Длительность опустошения лаг-полосы до нового значения HP в секундах")]
        [SerializeField] private float _hpBarLagDuration = 1f;

        [Tooltip("Кривая ослабления для опустошения лаг-полосы")]
        [SerializeField] private Ease _hpBarLagEase = Ease.OutCubic;

        [Tooltip("Длительность плавной анимации HP при лечении в секундах (без тряски, без лаг-полосы)")]
        [SerializeField] private float _hpBarHealDuration = 0.4f;

        [Tooltip("Кривая ослабления для анимации лечения")]
        [SerializeField] private Ease _hpBarHealEase = Ease.OutQuad;

        [Header("Ready Pulse")]
        [Tooltip("Длительность одного полного цикла пульсации в секундах")]
        [SerializeField] private float _readyPulseDuration = 0.6f;

        [Tooltip("Минимальная прозрачность в нижней точке пульсации (0-1)")]
        [SerializeField] private float _readyPulseAlpha = 0.4f;

        [Tooltip("Кривая ослабления для анимации пульсации")]
        [SerializeField] private Ease _readyPulseEase = Ease.InOutSine;

        [Header("Energy Transfer FX")]
        [Tooltip("Длительность полета эффекта передачи энергии в секундах")]
        [SerializeField] private float _energyTransferDuration = 0.45f;

        [Tooltip("Амплитуда боковых колебаний S-кривой эффекта передачи энергии в мировых единицах")]
        [SerializeField] private float _energyTransferWaveAmplitude = 0.22f;

        [Tooltip("Кривая скорости для полета эффекта передачи энергии")]
        [SerializeField] private Ease _energyTransferFlightEase = Ease.InOutSine;

        [Header("Targeting Highlights")]
        [Tooltip("Цвет свечения юнита, от которого начато перетаскивание (источник)")]
        [SerializeField] private Color _sourceHighlightColor = Color.white;

        [Tooltip("Цвет свечения допустимой цели атаки")]
        [SerializeField] private Color _attackTargetColor = new(1f, 0.15f, 0.15f, 1f);

        [Tooltip("Цвет свечения допустимой цели лечения")]
        [SerializeField] private Color _healTargetColor = new(0.15f, 1f, 0.25f, 1f);

        [Tooltip("Цвет свечения мертвого героя, допустимого для воскрешения")]
        [SerializeField] private Color _resurrectTargetColor = new(0.65f, 0.2f, 1f, 1f);

        [Header("Cooldown Visuals")]
        [Tooltip("Переводить портрет в черно-белый режим во время кулдауна способности")]
        [SerializeField] private bool _cooldownGrayscaleEnabled = true;

        [Header("Shield Pulse")]
        [Tooltip("Параметры пульсации щитов при попытке атаковать защищенного аватара")]
        [SerializeField] private ShieldPulseConfig _shieldPulse = new();

        [Header("Floating Numbers")]
        [Tooltip("Цвет метки числа урона")]
        [SerializeField] private Color _damageNumberColor = new(1f, 0.25f, 0.25f, 1f);

        [Tooltip("Цвет метки числа лечения")]
        [SerializeField] private Color _healNumberColor = new(0.25f, 1f, 0.25f, 1f);

        [Tooltip("Расстояние в мировых единицах, на которое число поднимается вверх во время анимации")]
        [SerializeField] private float _floatDamageDistance = 0.75f;

        [Tooltip("Общая длительность анимации всплытия и затухания в секундах")]
        [SerializeField] private float _floatDamageDuration = 1.2f;

        [Tooltip("Кривая ослабления для движения числа вверх")]
        [SerializeField] private Ease _floatDamageEase = Ease.OutCubic;

        
        public Color HitFlashColor => _hitFlashColor;
        public float HitFlashDuration => _hitFlashDuration;
        public Ease HitFlashEase => _hitFlashEase;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;
        public Ease KnockbackEase => _knockbackEase;
        public float HPBarLagDelay => _hpBarLagDelay;
        public float HPBarLagDuration => _hpBarLagDuration;
        public Ease HPBarLagEase => _hpBarLagEase;
        public float HPBarHealDuration => _hpBarHealDuration;
        public Ease HPBarHealEase => _hpBarHealEase;
        public float EnergyTransferDuration => _energyTransferDuration;
        public float EnergyTransferWaveAmplitude => _energyTransferWaveAmplitude;
        public Ease EnergyTransferFlightEase => _energyTransferFlightEase;
        public Color SourceHighlightColor => _sourceHighlightColor;
        public Color AttackTargetColor => _attackTargetColor;
        public Color HealTargetColor => _healTargetColor;
        public Color ResurrectTargetColor => _resurrectTargetColor;
        public Color DamageNumberColor => _damageNumberColor;
        public Color HealNumberColor => _healNumberColor;
        public float FloatDamageDistance => _floatDamageDistance;
        public float FloatDamageDuration => _floatDamageDuration;
        public Ease FloatDamageEase => _floatDamageEase;
        public float ReadyPulseDuration => _readyPulseDuration;
        public float ReadyPulseAlpha => _readyPulseAlpha;
        public Ease ReadyPulseEase => _readyPulseEase;
        public bool CooldownGrayscaleEnabled => _cooldownGrayscaleEnabled;
        public ShieldPulseConfig ShieldPulse => _shieldPulse;
    }
}