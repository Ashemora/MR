using System;
using DG.Tweening;
using Project.Scripts.Configs.Battle.Visuals;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI
{
    public class FloatingDamageNumber : MonoBehaviour
    {
        [Tooltip("Всплывающий текст значения урона или лечения")]
        [SerializeField] private TextMeshPro _label;


        private Sequence _sequence;


        private void OnDestroy()
        {
            _sequence?.Kill();
        }


        public void Play(int value, FloatingNumberType type, Transform anchor, BattleAnimationConfig config, Action onDone)
        {
            transform.position = anchor.position;

            _label.text = type switch
            {
                FloatingNumberType.Heal => $"+{value}",
                _ => $"-{value}"
            };

            _label.color = type switch
            {
                FloatingNumberType.Heal => config.HealNumberColor,
                _ => config.DamageNumberColor
            };

            _label.alpha = 1f;

            _sequence?.Kill();
            var startPos = transform.position;

            _sequence = DOTween.Sequence()
                .Append(transform
                    .DOMove(startPos + Vector3.up * config.FloatDamageDistance, config.FloatDamageDuration)
                    .SetEase(config.FloatDamageEase))
                .Join(_label
                    .DOFade(0f, config.FloatDamageDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => onDone?.Invoke());
        }

        public void Kill()
        {
            _sequence?.Kill();
        }
    }
}