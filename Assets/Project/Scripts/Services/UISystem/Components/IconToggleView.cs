using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Services.UISystem.Components
{
    [RequireComponent(typeof(Toggle))]
    public class IconToggleView : MonoBehaviour
    {
        [Tooltip("Toggle, состояние которого визуализируется. Если не задан - берётся с этого же GameObject")]
        [SerializeField] private Toggle _toggle;

        [Tooltip("Image, у которого подменяется sprite при переключении")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Спрайт для включённого состояния (звук играет)")]
        [SerializeField] private Sprite _onSprite;

        [Tooltip("Спрайт для выключенного состояния (mute)")]
        [SerializeField] private Sprite _offSprite;


        private readonly Subject<bool> _valueChanged = new();

        public Observable<bool> ValueChanged => _valueChanged;

        public bool IsOn
        {
            get
            {
                return _toggle && _toggle.isOn;
            }
        }


        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Awake()
        {
            if (!_toggle)
                _toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            if (_toggle)
                _toggle.onValueChanged.AddListener(OnToggleChanged);

            ApplySprite(IsOn);
        }

        private void OnDisable()
        {
            if (_toggle)
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        private void OnDestroy()
        {
            _valueChanged.Dispose();
        }


        public void SetIsOnWithoutNotify(bool isOn)
        {
            if (!_toggle)
                return;

            _toggle.SetIsOnWithoutNotify(isOn);
            ApplySprite(isOn);
        }


        private void OnToggleChanged(bool isOn)
        {
            ApplySprite(isOn);
            _valueChanged.OnNext(isOn);
        }

        private void ApplySprite(bool isOn)
        {
            if (!_iconImage)
                return;

            _iconImage.sprite = isOn ? _onSprite : _offSprite;
        }
    }
}