using Project.Scripts.Shared.Abilities;
using Project.Scripts.Shared.Heroes;
using UnityEngine;
using Project.Scripts.Configs.Battle.Abilities;

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


        public HeroActionType AbilityType => ToHeroActionType(GetPrimaryDirectAction().Kind);
        public int AbilityPower => GetPrimaryDirectAction().Value;
        public int MaxHP => _maxHP;
        public Sprite Portrait => _portrait;
        public int ActivationEnergyCost => _activeAbility?.ActivationEnergyCost ?? 0;
        public float ActivationCooldownSeconds => _activeAbility?.ActivationCooldownSeconds ?? 0f;
        public HeroPassiveConfig[] PassiveAbilities => _passiveAbilities;


        public ActiveAbilityDefinition ToActiveAbilityDefinition()
        {
            return _activeAbility != null ? _activeAbility.ToDefinition() : default;
        }

        private DirectActionDefinition GetPrimaryDirectAction()
        {
            var definition = ToActiveAbilityDefinition();
            var entries = definition.EffectEntries;
            for (var i = 0; i < entries.Count; i++)
            {
                var directActions = entries[i].DirectActions;
                for (var j = 0; j < directActions.Count; j++)
                    if (directActions[j].Kind is DirectActionKind.Damage or DirectActionKind.Heal)
                        return directActions[j];
            }

            return default;
        }

        private static HeroActionType ToHeroActionType(DirectActionKind actionKind)
        {
            return actionKind == DirectActionKind.Heal ? HeroActionType.HealAlly : HeroActionType.DealDamage;
        }
    }
}