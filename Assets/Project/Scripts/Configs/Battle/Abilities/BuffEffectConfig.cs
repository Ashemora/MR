using System;
using UnityEngine;
using Project.Scripts.Shared.Buffs;

namespace Project.Scripts.Configs.Battle.Abilities
{
    [Serializable]
    public class BuffEffectConfig
    {
        [Tooltip("Что меняет баф: ModifyAbilityPower = силу способности героя, ModifyActivationEnergyCost = стоимость активации, ModifyActivationCooldown = длительность cooldown активации героя, ModifyMatchEnergyBySlotKind = энергию от тайлов цвета слота владельца, ModifySpecialTileActivationEnergy = энергию от непосредственной активации спецтайлов, ModifyBombRadius = радиус действия бомб стороны цели, RepeatAbilityApplication = количество дополнительных применений способности героя к той же цели, NextAttackDamage = урон следующей атаки цели, ApplyAbilityToAdditionalTargets = применить способность героя к дополнительным подходящим целям, ModifyLineRuneThickness = дополнительные соседние ряды/столбцы при активации LineRune, ResurrectOnDeath = воскрешает героя-цель на Value HP при попытке умереть, Stun = запрещает активацию цели на DurationSeconds. В Burndown ResurrectOnDeath игнорируется. Рекомендуется использовать ResurrectOnDeath с MaxActivations=1 на пассивке, чтобы способность стала недоступной после воскрешения")]
        [SerializeField] private BuffKind _kind;

        [Tooltip("Как именно меняется числовой параметр")]
        [SerializeField] private ValueModifierOperation _operation;

        [Tooltip("Значение бафа")]
        [SerializeField] private float _value;

        [Tooltip("Когда баф снимается: Battle = до конца боя, NextAttack = после следующей атаки цели, NextActivation = после следующей активации цели, UntilEndOfNextMainPhase = до конца следующей Match/Hero фазы")]
        [SerializeField] private BuffLifetimeKind _lifetimeKind = BuffLifetimeKind.Battle;

        [Tooltip("Stack = повторное наложение создает отдельный параллельный стак со своим временем действия. IgnoreNew = новый такой же баф игнорируется, пока старый активен")]
        [SerializeField] private BuffStackingMode _stackingMode = BuffStackingMode.Stack;


        public BuffDefinition ToDefinition()
        {
            return new BuffDefinition(_kind, _operation, _value, _lifetimeKind, _stackingMode);
        }
    }
}