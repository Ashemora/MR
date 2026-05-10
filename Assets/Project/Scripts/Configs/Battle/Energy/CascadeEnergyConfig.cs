using Project.Scripts.Shared.Energy;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Energy
{
    [CreateAssetMenu(fileName = "CascadeEnergyConfig", menuName = "Configs/Battle/Cascade Energy Config")]
    public class CascadeEnergyConfig : ScriptableObject
    {
        [Header("Cascade Bonus")]
        [Tooltip("Прирост множителя за каждый уровень каскада. Каскад 1 = x1.0, каскад 2 = x(1.0 + шаг) и т.д.")]
        [SerializeField] private float _cascadeMultiplierStep = 0.15f;

        [Header("Multi-Match Bonus")]
        [Tooltip("Множитель энергии, если в одной волне 2 и более совпадений")]
        [SerializeField] private float _multiMatchMultiplier = 1.15f;

        [Header("Shape Bonuses")]
        [Tooltip("Множитель энергии для совпадений L-формы")]
        [SerializeField] private float _lShapeMultiplier = 1.20f;

        [Tooltip("Множитель энергии для совпадений T-формы")]
        [SerializeField] private float _tShapeMultiplier = 1.35f;

        [Header("Special Tile Energy Multipliers")]
        [Tooltip("Множитель энергии от тайлов, уничтоженных взрывом бомбы")]
        [SerializeField] private float _bombEnergyMultiplier = 1f;

        [Tooltip("Множитель энергии от тайлов, уничтоженных линейной руной (H и V)")]
        [SerializeField] private float _lineRuneEnergyMultiplier = 1f;

        [Tooltip("Множитель энергии от тайлов, уничтоженных штормом")]
        [SerializeField] private float _stormEnergyMultiplier = 1f;

        
        public CascadeEnergySettings ToSettings()
        {
            return new CascadeEnergySettings(
                _cascadeMultiplierStep,
                _multiMatchMultiplier,
                _lShapeMultiplier,
                _tShapeMultiplier,
                _bombEnergyMultiplier,
                _lineRuneEnergyMultiplier,
                _stormEnergyMultiplier);
        }
    }
}