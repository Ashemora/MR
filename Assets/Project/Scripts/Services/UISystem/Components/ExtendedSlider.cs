using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Services.UISystem.Components
{
    public class ExtendedSlider : Slider
    {
        [Tooltip("Дополнительные Graphic, подкрашиваемые так же, как Target Graphic, при смене состояния (Color Tint)")]
        [SerializeField] private Graphic[] _additionalTargetGraphics;

        
        private Color[] _additionalBaseColors;


        protected override void OnEnable()
        {
            CacheAdditionalBaseColors();
            base.OnEnable();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (Transition.ColorTint != transition)
                return;

            TintAdditionalGraphics(state, instant);
        }


        private void CacheAdditionalBaseColors()
        {
            if (null == _additionalTargetGraphics || 0 == _additionalTargetGraphics.Length)
            {
                _additionalBaseColors = null;
                return;
            }

            if (null == _additionalBaseColors || _additionalBaseColors.Length != _additionalTargetGraphics.Length)
                _additionalBaseColors = new Color[_additionalTargetGraphics.Length];

            for (var i = 0; i < _additionalTargetGraphics.Length; i++)
            {
                var graphic = _additionalTargetGraphics[i];
                _additionalBaseColors[i] = graphic ? graphic.color : Color.white;
            }
        }

        private void TintAdditionalGraphics(SelectionState state, bool instant)
        {
            if (null == _additionalTargetGraphics || 0 == _additionalTargetGraphics.Length)
                return;

            if (null == _additionalBaseColors || _additionalBaseColors.Length != _additionalTargetGraphics.Length)
                CacheAdditionalBaseColors();

            var tintColor = GetTintColorForState(state) * colors.colorMultiplier;
            var duration = instant ? 0f : colors.fadeDuration;

            for (var i = 0; i < _additionalTargetGraphics.Length; i++)
            {
                var graphic = _additionalTargetGraphics[i];
                if (!graphic || graphic == targetGraphic)
                    continue;

                graphic.CrossFadeColor(_additionalBaseColors[i] * tintColor, duration, true, true);
            }
        }

        private Color GetTintColorForState(SelectionState state)
        {
            switch (state)
            {
                case SelectionState.Normal:
                    return colors.normalColor;
                case SelectionState.Highlighted:
                    return colors.highlightedColor;
                case SelectionState.Pressed:
                    return colors.pressedColor;
                case SelectionState.Selected:
                    return colors.selectedColor;
                case SelectionState.Disabled:
                    return colors.disabledColor;
                default:
                    return Color.black;
            }
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (isActiveAndEnabled)
                CacheAdditionalBaseColors();
        }
#endif
    }
}