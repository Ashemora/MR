using System;
using DG.Tweening;
using Project.Scripts.Configs.Battle.Visuals;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.FX
{
    public class EnergyTransferView : MonoBehaviour
    {
        private const float FullWaveRotation = Mathf.PI * 2f;
        

        [Tooltip("Рендереры, которые окрашиваются при воспроизведении эффекта")]
        [SerializeField] private Renderer[] _renderers;

        [Tooltip("Если включено, альфа TrailRenderer берётся из его градиента; если выключено - используется альфа пришедшего цвета")]
        [SerializeField] private bool _trailPreserveGradientAlpha = true;


        private static readonly int ColorShaderId = Shader.PropertyToID("_Color");
        private Tween _flightTween;
        private MaterialPropertyBlock _mpb;


        private void OnDestroy()
        {
            Kill();
        }


        public void Play(Vector3 from, Vector3 to, Color color, BattleAnimationConfig config, Action onComplete)
        {
            Kill();

            SetTrailEmission(false);
            transform.position = from;
            ClearTrails();
            ApplyColor(color);
            SetTrailEmission(true);

            var delta = to - from;
            var direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.up;
            var side = Vector3.Cross(direction, Vector3.forward);
            if (side.sqrMagnitude <= 0.0001f)
                side = Vector3.right;
            else
                side.Normalize();

            var duration = config ? config.EnergyTransferDuration : 0.45f;
            var amplitude = config ? config.EnergyTransferWaveAmplitude : 0.22f;
            var ease = config ? config.EnergyTransferFlightEase : Ease.InOutSine;
            var progress = 0f;

            _flightTween = DOTween.To(() => progress, x =>
                {
                    progress = x;
                    var basePosition = Vector3.LerpUnclamped(from, to, progress);
                    var waveOffset = Mathf.Sin(progress * FullWaveRotation) * amplitude;
                    transform.position = basePosition + side * waveOffset;
                },
                1f,
                duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    _flightTween = null;
                    transform.position = to;
                    onComplete?.Invoke();
                });
        }

        private void ApplyColor(Color color)
        {
            if (null == _renderers || _renderers.Length == 0)
                return;

            _mpb ??= new MaterialPropertyBlock();
            _mpb.SetColor(ColorShaderId, color);

            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (!r)
                    continue;

                if (r is TrailRenderer trail)
                {
                    float startAlpha, endAlpha;
                    if (_trailPreserveGradientAlpha)
                    {
                        var gradient = trail.colorGradient;
                        startAlpha = gradient.Evaluate(0f).a;
                        endAlpha = gradient.Evaluate(1f).a;
                    }
                    else
                    {
                        startAlpha = color.a;
                        endAlpha = color.a;
                    }
                    trail.startColor = new Color(color.r, color.g, color.b, startAlpha);
                    trail.endColor = new Color(color.r, color.g, color.b, endAlpha);
                }
                else
                    r.SetPropertyBlock(_mpb);
            }
        }

        public void Kill()
        {
            _flightTween?.Kill();
            _flightTween = null;
            SetTrailEmission(false);
            ClearTrails();
        }

        private void SetTrailEmission(bool isEmitting)
        {
            if (null == _renderers || _renderers.Length == 0)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] is TrailRenderer trail)
                    trail.emitting = isEmitting;
            }
        }

        private void ClearTrails()
        {
            if (null == _renderers || _renderers.Length == 0)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] is TrailRenderer trail)
                    trail.Clear();
            }
        }
    }
}