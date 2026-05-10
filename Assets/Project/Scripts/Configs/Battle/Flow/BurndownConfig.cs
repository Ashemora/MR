using DG.Tweening;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Flow
{
    [CreateAssetMenu(fileName = "BurndownConfig", menuName = "Configs/Battle/Burndown Config")]
    public class BurndownConfig : ScriptableObject
    {
        [Tooltip("Фиксированный урон в секунду, применяемый к каждому герою во время овертайма")]
        [SerializeField] private int _heroDrainPerSecond = 200;

        [Tooltip("Фиксированный урон в секунду, применяемый к открытому аватару во время овертайма")]
        [SerializeField] private int _avatarDrainPerSecond = 150;

        [Tooltip("Интервал в секундах между тиками урона при овертайме")]
        [SerializeField] private float _drainTickInterval = 0.1f;

        [Tooltip("Длительность анимации схлопывания всех тайлов при старте овертайма")]
        [SerializeField] private float _collapseAllDuration = 0.6f;

        [Tooltip("Кривая ослабления для схлопывания тайлов при овертайме")]
        [SerializeField] private Ease _collapseAllEase = Ease.InBack;


        public int HeroDrainPerSecond => _heroDrainPerSecond;
        public int AvatarDrainPerSecond => _avatarDrainPerSecond;
        public float DrainTickInterval => _drainTickInterval;
        public float CollapseAllDuration => _collapseAllDuration;
        public Ease CollapseAllEase => _collapseAllEase;
    }
}