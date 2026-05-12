using Project.Scripts.Shared.Abilities;
using UnityEngine;
using Project.Scripts.Configs.Battle.Abilities;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Configs.Battle.Units
{
    [CreateAssetMenu(fileName = "AvatarConfig", menuName = "Configs/Battle/Avatar Config")]
    public class AvatarConfig : ScriptableObject
    {
        [Tooltip("Максимальные HP аватара. Ноль означает бессмертие")]
        [SerializeField] private int _maxHP = 550;

        [Tooltip("Portrait sprite displayed in the avatar slot (null = empty frame)")]
        [SerializeField] private Sprite _portrait;

        [Tooltip("Активная способность аватара")]
        [SerializeField] private ActiveAbilityConfig _activeAbility;


        public int MaxHP => _maxHP;
        public int ActivationEnergyCost => _activeAbility?.ActivationEnergyCost ?? 0;
        public UnitActionType AbilityType => UnitActionTypeMapping.FromActiveAbility(ToActiveAbilityDefinition());
        public int AbilityPower => ToActiveAbilityDefinition().DirectAction.Value;
        public Sprite Portrait => _portrait;
        public float ActivationCooldownSeconds => _activeAbility?.ActivationCooldownSeconds ?? 0f;


        public ActiveAbilityDefinition ToActiveAbilityDefinition()
        {
            return null != _activeAbility ? _activeAbility.ToDefinition() : default;
        }
    }
}