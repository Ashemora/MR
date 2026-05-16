using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Services.UISystem;
using Project.Scripts.Services.UISystem.Components;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Lobby.Options
{
    public class OptionsView : BaseView<OptionsViewModel>
    {
        private const float OpenDuration = 0.32f;
        private const float CloseDuration = 0.2f;
        private const float StartScale = 0.7f;
        private const float EndScale = 1f;


        [Tooltip("Корневой RectTransform центральной панели - к нему применяется анимация масштаба")]
        [SerializeField] private RectTransform _panel;

        [Tooltip("CanvasGroup полупрозрачного фона - анимируется альфа на открытии и закрытии")]
        [SerializeField] private CanvasGroup _blockerCanvasGroup;

        [Tooltip("Кнопка-фон, перехватывающая клики вне панели - закрывает окно")]
        [SerializeField] private Button _blockerButton;

        [Tooltip("Кнопка закрытия окна опций")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Слайдер громкости музыки (0..1)")]
        [SerializeField] private Slider _musicSlider;

        [Tooltip("Слайдер громкости SFX (0..1)")]
        [SerializeField] private Slider _sfxSlider;

        [Tooltip("Toggle включения музыки. On = звук играет, Off = mute")]
        [SerializeField] private IconToggleView _musicToggle;

        [Tooltip("Toggle включения SFX. On = звук играет, Off = mute")]
        [SerializeField] private IconToggleView _sfxToggle;


        protected override bool EnablePumpAnimation => false;
        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;


        private Sequence _openSequence;
        private Sequence _closeSequence;


        protected override UniTask OnBindViewModel()
        {
            _musicSlider.minValue = 0f;
            _musicSlider.maxValue = 1f;
            _sfxSlider.minValue = 0f;
            _sfxSlider.maxValue = 1f;

            _musicToggle.SetIsOnWithoutNotify(false == ViewModel.MusicMuted.CurrentValue);
            _sfxToggle.SetIsOnWithoutNotify(false == ViewModel.SfxMuted.CurrentValue);
            ApplyMusicSliderMutedState(ViewModel.MusicMuted.CurrentValue);
            ApplySfxSliderMutedState(ViewModel.SfxMuted.CurrentValue);

            ViewModel.MusicVolume
                .Subscribe(value =>
                {
                    if (false == ViewModel.MusicMuted.CurrentValue)
                        _musicSlider.SetValueWithoutNotify(value);
                })
                .AddTo(Disposables);
            ViewModel.SfxVolume
                .Subscribe(value =>
                {
                    if (false == ViewModel.SfxMuted.CurrentValue)
                        _sfxSlider.SetValueWithoutNotify(value);
                })
                .AddTo(Disposables);
            ViewModel.MusicMuted
                .Subscribe(muted =>
                {
                    _musicToggle.SetIsOnWithoutNotify(false == muted);
                    ApplyMusicSliderMutedState(muted);
                })
                .AddTo(Disposables);
            ViewModel.SfxMuted
                .Subscribe(muted =>
                {
                    _sfxToggle.SetIsOnWithoutNotify(false == muted);
                    ApplySfxSliderMutedState(muted);
                })
                .AddTo(Disposables);

            _musicSlider.onValueChanged.AddListener(ViewModel.SetMusicVolume);
            _sfxSlider.onValueChanged.AddListener(ViewModel.SetSfxVolume);

            _musicToggle.ValueChanged
                .Subscribe(isOn => ViewModel.SetMusicMuted(false == isOn))
                .AddTo(Disposables);
            _sfxToggle.ValueChanged
                .Subscribe(isOn => ViewModel.SetSfxMuted(false == isOn))
                .AddTo(Disposables);

            _closeButton.onClick.AddListener(ViewModel.RequestClose);
            if (_blockerButton)
                _blockerButton.onClick.AddListener(ViewModel.RequestClose);

            return UniTask.CompletedTask;
        }

        protected override async UniTask OnShow()
        {
            KillTweens();

            if (_panel)
                _panel.localScale = Vector3.one * StartScale;
            if (_blockerCanvasGroup)
                _blockerCanvasGroup.alpha = 0f;

            _openSequence = DOTween.Sequence();
            if (_blockerCanvasGroup)
                _openSequence.Join(_blockerCanvasGroup.DOFade(1f, OpenDuration * 0.8f));
            if (_panel)
                _openSequence.Join(_panel.DOScale(EndScale, OpenDuration).SetEase(Ease.OutBack));

            await _openSequence.AsyncWaitForCompletion();
        }

        protected override async UniTask OnHide()
        {
            KillTweens();

            _closeSequence = DOTween.Sequence();
            if (_blockerCanvasGroup)
                _closeSequence.Join(_blockerCanvasGroup.DOFade(0f, CloseDuration));
            if (_panel)
                _closeSequence.Join(_panel.DOScale(StartScale, CloseDuration).SetEase(Ease.InBack));

            await _closeSequence.AsyncWaitForCompletion();
        }

        protected override void OnClose()
        {
            KillTweens();

            _closeButton.onClick.RemoveAllListeners();
            if (_blockerButton)
                _blockerButton.onClick.RemoveAllListeners();

            _musicSlider.onValueChanged.RemoveAllListeners();
            _sfxSlider.onValueChanged.RemoveAllListeners();
        }


        private void KillTweens()
        {
            _openSequence?.Kill();
            _openSequence = null;
            _closeSequence?.Kill();
            _closeSequence = null;
        }

        private void ApplyMusicSliderMutedState(bool muted)
        {
            _musicSlider.interactable = false == muted;
            _musicSlider.SetValueWithoutNotify(muted ? 0f : ViewModel.MusicVolume.CurrentValue);
        }

        private void ApplySfxSliderMutedState(bool muted)
        {
            _sfxSlider.interactable = false == muted;
            _sfxSlider.SetValueWithoutNotify(muted ? 0f : ViewModel.SfxVolume.CurrentValue);
        }
    }
}