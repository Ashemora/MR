using Project.Scripts.Shared.BattleFlow;
using UnityEngine;

namespace Project.Scripts.Configs.Battle
{
    [CreateAssetMenu(fileName = "BattleFlowConfig", menuName = "Configs/Battle/Battle Flow Config")]
    public class BattleFlowConfig : ScriptableObject
    {
        [Header("Rounds settings")]
        [Tooltip("Количество полных раундов до перехода к пост-раундовому состоянию")]
        [SerializeField] private int _roundCount = 2;

        [Tooltip("Длительность Match Phase в секундах")]
        [SerializeField] private float _matchPhaseDuration = 30f;

        [Tooltip("Длительность Hero Phase в секундах")]
        [SerializeField] private float _heroPhaseDuration = 30f;
        
        [Tooltip("Правило переноса остатка энергии между раундами")]
        [SerializeField] private EnergyCarryoverMode _energyCarryoverMode = EnergyCarryoverMode.CarryOverBetweenRounds;

        [Tooltip("Максимальный запас энергии для каждой стороны боя")]
        [SerializeField] private int _energyCap = 250;

        [Header("Cooldown settings")]
        [Tooltip("Минимальная длительность кулдауна активируемого юнита после применения всех модификаторов. Ноль отключает нижний предел")]
        [SerializeField] private float _minUnitActivationCooldownSeconds = 0.5f;

        [Header("PrePhase settings")]
        [Tooltip("Длительность подготовительной паузы перед фазой в секундах")]
        [SerializeField] private int _prePhaseDuration = 5;

        [Tooltip("Если включено, во время подготовительной паузы затемняются оба экрана фаз")]
        [SerializeField] private bool _dimCurrentPhaseDuringPrePhase = true;

        [Tooltip("Если включено, перед самой первой фазой боя проигрывается подготовительная пауза. Если выключено - бой стартует сразу, без preroll-паузы (на последующие фазы не влияет)")]
        [SerializeField] private bool _enablePrePhaseOnBattleStart = true;

        [Header("Final countdown settings")]
        [Tooltip("Оставшееся время в секундах, при котором начинается посекундный обратный отсчёт")]
        [SerializeField] private int _countdownThreshold = 5;


        public int RoundCount => _roundCount;
        public float MatchPhaseDuration => _matchPhaseDuration;
        public float HeroPhaseDuration => _heroPhaseDuration;
        public int PrePhaseDuration => _prePhaseDuration;
        public bool DimCurrentPhaseDuringPrePhase => _dimCurrentPhaseDuringPrePhase;
        public bool EnablePrePhaseOnBattleStart => _enablePrePhaseOnBattleStart;
        public int CountdownThreshold => _countdownThreshold;
        public EnergyCarryoverMode EnergyCarryoverMode => _energyCarryoverMode;
        public int EnergyCap => Mathf.Max(1, _energyCap);
        public float MinUnitActivationCooldownSeconds => Mathf.Max(0f, _minUnitActivationCooldownSeconds);
    }
}