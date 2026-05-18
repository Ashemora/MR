#if DEV
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Services.UISystem;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI.Dev
{
    public class DevBattleOptionsView : BaseView<DevBattleOptionsViewModel>
    {
        private const float OpenDuration = 0.32f;
        private const float CloseDuration = 0.2f;
        private const float StartScale = 0.7f;
        private const float EndScale = 1f;


        [Header("Animation")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _blockerCanvasGroup;

        [Header("Controls")]
        [SerializeField] private Button _modeButton;
        [SerializeField] private TMP_Text _modeButtonText;
        [SerializeField] private GameObject _strengthRoot;
        [SerializeField] private Button _strengthButton;
        [SerializeField] private TMP_Text _strengthButtonText;
        [SerializeField] private TMP_InputField _opponentSeedInput;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _blockerButton;


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;


        private Sequence _openSequence;
        private Sequence _closeSequence;


        protected override UniTask OnBindViewModel()
        {
            DisableLegacyDropdowns();
            BindModeButton();
            BindStrengthButton();
            BindOpponentSeedInput();

            if (_closeButton)
                _closeButton.onClick.AddListener(ViewModel.RequestClose);
            if (_blockerButton)
                _blockerButton.onClick.AddListener(ViewModel.RequestClose);

            ViewModel.ModeIndex
                .Subscribe(modeIndex =>
                {
                    UpdateModeVisibility(modeIndex);
                    UpdateModeLabel();
                })
                .AddTo(Disposables);
            ViewModel.StrengthIndex
                .Subscribe(_ => UpdateStrengthLabel())
                .AddTo(Disposables);

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

            if (_modeButton)
                _modeButton.onClick.RemoveListener(CycleMode);
            if (_strengthButton)
                _strengthButton.onClick.RemoveListener(CycleStrength);
            if (_opponentSeedInput)
                _opponentSeedInput.onValueChanged.RemoveListener(ViewModel.SetOpponentSeedText);
            if (_closeButton)
                _closeButton.onClick.RemoveListener(ViewModel.RequestClose);
            if (_blockerButton)
                _blockerButton.onClick.RemoveListener(ViewModel.RequestClose);
        }


        private void BindModeButton()
        {
            if (!_modeButton)
                return;

            _modeButton.onClick.AddListener(CycleMode);
            UpdateModeLabel();
        }

        private void BindStrengthButton()
        {
            if (!_strengthButton)
                return;

            _strengthButton.interactable = ViewModel.StrengthCount > 0;
            _strengthButton.onClick.AddListener(CycleStrength);
            UpdateStrengthLabel();
        }

        private void BindOpponentSeedInput()
        {
            if (!_opponentSeedInput)
                return;

            _opponentSeedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _opponentSeedInput.SetTextWithoutNotify(ViewModel.OpponentSeedText.CurrentValue);
            _opponentSeedInput.onValueChanged.AddListener(ViewModel.SetOpponentSeedText);
        }

        private void UpdateModeVisibility(int modeIndex)
        {
            var isRandom = modeIndex == 1;

            if (_strengthRoot)
                _strengthRoot.SetActive(isRandom);
            if (_opponentSeedInput)
                _opponentSeedInput.gameObject.SetActive(isRandom);
        }

        private void CycleMode()
        {
            ViewModel.SetModeIndex(ViewModel.ModeIndex.CurrentValue == 0 ? 1 : 0);
        }

        private void CycleStrength()
        {
            if (ViewModel.StrengthCount <= 0)
                return;

            ViewModel.SetStrengthIndex((ViewModel.StrengthIndex.CurrentValue + 1) % ViewModel.StrengthCount);
        }

        private void UpdateModeLabel()
        {
            if (_modeButtonText)
                _modeButtonText.text = ViewModel.ModeIndex.CurrentValue == 0 ? "Config" : "Random";
        }

        private void UpdateStrengthLabel()
        {
            if (!_strengthButtonText)
                return;

            var index = ViewModel.StrengthIndex.CurrentValue;
            var label = ViewModel.StrengthCount > 0 ? ViewModel.GetStrengthDisplayName(index) : "-";
            _strengthButtonText.text = label;
        }

        private void DisableLegacyDropdowns()
        {
            var dropdowns = GetComponentsInChildren<Dropdown>(true);
            for (var i = 0; i < dropdowns.Length; i++)
                dropdowns[i].enabled = false;
        }

        private void KillTweens()
        {
            _openSequence?.Kill();
            _openSequence = null;
            _closeSequence?.Kill();
            _closeSequence = null;
        }
    }
}
#endif