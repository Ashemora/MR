using UnityEngine;

namespace Project.Scripts.Configs.Battle.Flow
{
    [CreateAssetMenu(fileName = "EscalationConfig", menuName = "Configs/Battle/Escalation Config")]
    public class EscalationConfig : ScriptableObject
    {
        [Tooltip("Порог оставшегося времени в секундах, при котором начинается фаза эскалации боя - запускает объявление и игровые модификаторы")]
        [SerializeField] private float _activationThreshold = 30f;

        [Tooltip("Множитель скорости автозаполнения энергии при эскалации (например, 2 = тики идут вдвое быстрее)")]
        [SerializeField] private float _autoEnergyIntervalMultiplier = 2f;

        [Tooltip("Множитель на итоговое количество энергии от каскадов при эскалации (1 = без изменений)")]
        [SerializeField] private float _cascadeEnergyMultiplier = 1f;


        public float ActivationThreshold => _activationThreshold;
        public float AutoEnergyIntervalMultiplier => _autoEnergyIntervalMultiplier;
        public float CascadeEnergyMultiplier => _cascadeEnergyMultiplier;
    }
}