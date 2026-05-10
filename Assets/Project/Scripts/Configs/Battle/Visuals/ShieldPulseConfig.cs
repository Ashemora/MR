using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Visuals
{
    [Serializable]
    public class ShieldPulseConfig
    {
        [Tooltip("Множитель масштаба щита в пике пульсации")]
        [SerializeField] private float _scaleMultiplier = 1.08f;

        [Tooltip("Длительность одного удара пульсации (нарастание + спад) в секундах")]
        [SerializeField] private float _duration = 0.25f;

        [Tooltip("Пауза между ударами пульсации в секундах")]
        [SerializeField] private float _interval = 0.65f;

        [Tooltip("Ease для анимации пульсации")]
        [SerializeField] private Ease _ease = Ease.InOutSine;


        public float ScaleMultiplier => _scaleMultiplier;
        public float Duration => _duration;
        public float Interval => _interval;
        public Ease Ease => _ease;
    }
}