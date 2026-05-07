using DG.Tweening;
using Project.Scripts.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.Battle.Layout
{
    public class EnergyBarView : MonoBehaviour
    {
        [Tooltip("SpriteRenderer фона бара (полная ширина)")]
        [SerializeField] private SpriteRenderer _background;

        [Tooltip("SpriteRenderer заполнения бара (масштабируется пропорционально энергии)")]
        [SerializeField] private SpriteRenderer _fill;

        [Tooltip("Текстовая метка с текущим значением энергии")]
        [SerializeField] private TMP_Text _valueText;

        [Tooltip("Ширина бара в мировых единицах")]
        [SerializeField] private float _barWidth = 2f;

        [Tooltip("Высота бара в мировых единицах")]
        [SerializeField] private float _barHeight = 0.35f;

        [Tooltip("Длительность анимации заполнения в секундах")]
        [SerializeField] private float _animDuration = 0.3f;

        [Tooltip("Кривая плавности анимации заполнения")]
        [SerializeField] private Ease _animEase = Ease.OutQuad;


        public float Height => _barHeight * _layoutScale;
        public float BaseHeight => _barHeight;


        private float _displayedRatio;
        private float _layoutScale = 1f;
        private int _maxValue = 1;
        private int _currentValue;
        private Tween _fillTween;
        private AnimatedIntegerText _valueTextTween;


        private void OnDestroy()
        {
            _fillTween?.Kill();
            _valueTextTween?.Dispose();
        }

        private void Awake()
        {
            ApplyBackground();
            ApplyFill(0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyBackground();
            ApplyFill(_displayedRatio);
        }
#endif


        public void SetMaxValue(int maxValue)
        {
            _maxValue = Mathf.Max(1, maxValue);
            _fillTween?.Kill();
            _displayedRatio = GetRatio(_currentValue);
            ApplyFill(_displayedRatio);
        }

        public void SetValue(int value, bool animate = true)
        {
            _currentValue = value;
            if (_valueText)
            {
                _valueTextTween ??= new AnimatedIntegerText(_valueText);
                if (animate)
                    _valueTextTween.AnimateTo(value, _animDuration, _animEase);
                else
                    _valueTextTween.SetInstant(value);
            }

            var targetRatio = GetRatio(value);
            _fillTween?.Kill();
            if (false == animate)
            {
                _displayedRatio = targetRatio;
                ApplyFill(_displayedRatio);
                return;
            }

            _fillTween = DOTween.To(
                    () => _displayedRatio,
                    r => { _displayedRatio = r; ApplyFill(r); },
                    targetRatio,
                    _animDuration)
                .SetEase(_animEase);
        }

        public void SetWorldCenterY(float worldY)
        {
            var pos = transform.position;
            transform.position = new Vector3(pos.x, worldY, pos.z);
        }

        public void SetLayoutScale(float scale)
        {
            _layoutScale = Mathf.Max(0.01f, scale);
            ApplyBackground();
            ApplyFill(_displayedRatio);
        }

        public void SetTextVisible(bool visible)
        {
            if (_valueText)
                _valueText.enabled = visible;
        }


        private void ApplyBackground()
        {
            if (false == _background || false == _background.sprite)
                return;

            ApplySpriteSize(_background, _barWidth * _layoutScale, _barHeight * _layoutScale);
        }

        private void ApplyFill(float ratio)
        {
            if (false == _fill || false == _fill.sprite)
                return;

            var barWidth = _barWidth * _layoutScale;
            var fillWidth = barWidth * ratio;
            ApplySpriteSize(_fill, fillWidth, _barHeight * _layoutScale);

            var local = _fill.transform.localPosition;
            _fill.transform.localPosition = new Vector3(barWidth * (ratio - 1f) * 0.5f, local.y, local.z);
        }

        private float GetRatio(int value)
        {
            return Mathf.Clamp01(value / (float)_maxValue);
        }

        private static void ApplySpriteSize(SpriteRenderer sr, float worldWidth, float worldHeight)
        {
            var spriteSize = sr.sprite.bounds.size;
            var parentScale = sr.transform.parent ? sr.transform.parent.lossyScale : Vector3.one;
            sr.transform.localScale = new Vector3(
                spriteSize.x > 0f ? worldWidth / (spriteSize.x * parentScale.x) : 1f,
                spriteSize.y > 0f ? worldHeight / (spriteSize.y * parentScale.y) : 1f,
                1f);
        }
    }
}