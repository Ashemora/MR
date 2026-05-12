using Project.Scripts.Shared.Abilities;
using UnityEngine;
using Project.Scripts.Configs.Battle.Abilities;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Configs.Battle.Units
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Configs/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Tooltip("Максимальные HP этого героя. Ноль означает, что герой бессмертен (не может получать урон)")]
        [SerializeField] private int _maxHP = 50;

        [Tooltip("Отображаемое имя героя, для будущих UI-ярлыков")]
        [SerializeField] private string _displayName;

        [Tooltip("Спрайт портрета в слоте героя (null = пустая рамка)")]
        [SerializeField] private Sprite _portrait;

        [Tooltip("Активная способность героя")]
        [SerializeField] private ActiveAbilityConfig _activeAbility;

        [Tooltip("Пассивные способности героя. Пустой список означает, что пассивных способностей нет")]
        [SerializeField] private HeroPassiveConfig[] _passiveAbilities;


        public UnitActionType AbilityType => ResolveAbilityType();
        public int AbilityPower => ToActiveAbilityDefinition().DirectAction.Value;
        public int MaxHP => _maxHP;
        public Sprite Portrait => _portrait;
        public int ActivationEnergyCost => _activeAbility?.ActivationEnergyCost ?? 0;
        public float ActivationCooldownSeconds => _activeAbility?.ActivationCooldownSeconds ?? 0f;
        public HeroPassiveConfig[] PassiveAbilities => _passiveAbilities;


        public ActiveAbilityDefinition ToActiveAbilityDefinition()
        {
            return null != _activeAbility ? _activeAbility.ToDefinition() : default;
        }

        private UnitActionType ResolveAbilityType()
        {
            return UnitActionTypeMapping.FromActiveAbility(ToActiveAbilityDefinition());
        }
    }
}