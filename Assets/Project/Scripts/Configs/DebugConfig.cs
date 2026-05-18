using UnityEngine;

namespace Project.Scripts.Configs
{
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "Configs/Debug Config")]
    public class DebugConfig : ScriptableObject
    {
        [Header("Energy & Abilities")]
        [Tooltip("Логировать детали формулы накопления энергии аватара после каждого матча.")]
        [SerializeField] private bool _logEnergyAccumulation;

        [Header("Board & Match")]
        [Tooltip("Логировать разбивку волн каскада и итоги энергии по видам тайлов после каждого хода.")]
        [SerializeField] private bool _logCascades;

        [Header("Combat")]
        [Tooltip("Логировать урон, нанесённый врагу, включая HP до и после.")]
        [SerializeField] private bool _logCombatDamage;

        [Header("Systems")]
        [Tooltip("Логировать регистрацию, показ и скрытие вью в UIService.")]
        [SerializeField] private bool _logUIEvents;

        [Tooltip("Логировать события жизненного цикла EventBus (например, Dispose).")]
        [SerializeField] private bool _logEventBus;

#if DEV
        [Header("DEV")]
        [Tooltip("Логировать подробности DEV-настроек подбора противника.")]
        [SerializeField] private bool _logDevOpponentOptions;
#endif


        public bool LogEnergyAccumulation => _logEnergyAccumulation;
        public bool LogCascades => _logCascades;
        public bool LogCombatDamage => _logCombatDamage;
        public bool LogUIEvents => _logUIEvents;
        public bool LogEventBus => _logEventBus;
#if DEV
        public bool LogDevOpponentOptions => _logDevOpponentOptions;
#endif
    }
}