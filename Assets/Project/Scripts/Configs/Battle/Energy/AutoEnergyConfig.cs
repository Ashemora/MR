using UnityEngine;

namespace Project.Scripts.Configs.Battle.Energy
{
    [CreateAssetMenu(fileName = "AutoEnergyConfig", menuName = "Configs/Battle/Auto Energy Config")]
    public class AutoEnergyConfig : ScriptableObject
    {
        [Tooltip("Интервал между тиками автозаполнения энергии (в секундах)")]
        [SerializeField] private float _tickInterval = 3f;

        [Tooltip("Количество энергии, начисляемое каждому герою и аватару за один тик")]
        [SerializeField] private float _energyPerTick = 1f;


        public float TickInterval => _tickInterval;
        public float EnergyPerTick => _energyPerTick;
    }
}