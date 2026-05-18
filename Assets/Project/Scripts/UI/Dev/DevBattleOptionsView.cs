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

        [Header("Player section")]
        [SerializeField] private Button _playerModeButton;
        [SerializeField] private TMP_Text _playerModeButtonText;
        [SerializeField] private GameObject _playerDeckRoot;
        [SerializeField] private TMP_Text _playerDeckSelectedText;
        [SerializeField] private RectTransform _playerDeckListContent;
        [SerializeField] private GameObject _playerSeedRoot;
        [SerializeField] private TMP_InputField _playerSeedInput;

        [Header("Opponent section")]
        [SerializeField] private Button _opponentModeButton;
        [SerializeField] private TMP_Text _opponentModeButtonText;
        [SerializeField] private GameObject _opponentDeckRoot;
        [SerializeField] private TMP_Text _opponentDeckSelectedText;
        [SerializeField] private RectTransform _opponentDeckListContent;
        [SerializeField] private GameObject _opponentSeedRoot;
        [SerializeField] private TMP_InputField _opponentSeedInput;

        [Header("Bot strength")]
        [SerializeField] private Button _strengthButton;
        [SerializeField] private TMP_Text _strengthButtonText;

        [Header("Closing")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _blockerButton;

        [Header("Deck list item prefab")]
        [SerializeField] private DevDeckListItemView _deckListItemPrefab;


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;


        private Sequence _openSequence;
        private Sequence _closeSequence;
        private DevDeckListItemView[] _playerDeckItems;
        private DevDeckListItemView[] _opponentDeckItems;


        protected override UniTask OnBindViewModel()
        {
            DisableLegacyDropdowns();
            BuildDeckLists();
            BindModeButtons();
            BindStrengthButton();
            BindSeedInputs();

            if (_closeButton)
                _closeButton.onClick.AddListener(ViewModel.RequestClose);
            if (_blockerButton)
                _blockerButton.onClick.AddListener(ViewModel.RequestClose);

            ViewModel.PlayerModeIndex
                .Subscribe(modeIndex =>
                {
                    UpdateSideVisibility(modeIndex, _playerDeckRoot, _playerSeedRoot);
                    UpdateModeLabel(_playerModeButtonText, modeIndex);
                })
                .AddTo(Disposables);
            ViewModel.OpponentModeIndex
                .Subscribe(modeIndex =>
                {
                    UpdateSideVisibility(modeIndex, _opponentDeckRoot, _opponentSeedRoot);
                    UpdateModeLabel(_opponentModeButtonText, modeIndex);
                })
                .AddTo(Disposables);
            ViewModel.PlayerDeckIndex
                .Subscribe(index => RefreshDeckSelection(_playerDeckItems, _playerDeckSelectedText, index))
                .AddTo(Disposables);
            ViewModel.OpponentDeckIndex
                .Subscribe(index => RefreshDeckSelection(_opponentDeckItems, _opponentDeckSelectedText, index))
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

            if (_playerModeButton)
                _playerModeButton.onClick.RemoveListener(CyclePlayerMode);
            if (_opponentModeButton)
                _opponentModeButton.onClick.RemoveListener(CycleOpponentMode);
            if (_strengthButton)
                _strengthButton.onClick.RemoveListener(CycleStrength);
            if (_playerSeedInput)
                _playerSeedInput.onValueChanged.RemoveListener(ViewModel.SetPlayerSeedText);
            if (_opponentSeedInput)
                _opponentSeedInput.onValueChanged.RemoveListener(ViewModel.SetOpponentSeedText);
            if (_closeButton)
                _closeButton.onClick.RemoveListener(ViewModel.RequestClose);
            if (_blockerButton)
                _blockerButton.onClick.RemoveListener(ViewModel.RequestClose);
        }


        private void BindModeButtons()
        {
            if (_playerModeButton)
            {
                _playerModeButton.onClick.AddListener(CyclePlayerMode);
                UpdateModeLabel(_playerModeButtonText, ViewModel.PlayerModeIndex.CurrentValue);
            }
            if (_opponentModeButton)
            {
                _opponentModeButton.onClick.AddListener(CycleOpponentMode);
                UpdateModeLabel(_opponentModeButtonText, ViewModel.OpponentModeIndex.CurrentValue);
            }
        }

        private void BindStrengthButton()
        {
            if (!_strengthButton)
                return;

            _strengthButton.interactable = ViewModel.StrengthCount > 0;
            _strengthButton.onClick.AddListener(CycleStrength);
            UpdateStrengthLabel();
        }

        private void BindSeedInputs()
        {
            if (_playerSeedInput)
            {
                _playerSeedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _playerSeedInput.SetTextWithoutNotify(ViewModel.PlayerSeedText.CurrentValue);
                _playerSeedInput.onValueChanged.AddListener(ViewModel.SetPlayerSeedText);
            }
            if (_opponentSeedInput)
            {
                _opponentSeedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _opponentSeedInput.SetTextWithoutNotify(ViewModel.OpponentSeedText.CurrentValue);
                _opponentSeedInput.onValueChanged.AddListener(ViewModel.SetOpponentSeedText);
            }
        }

        private void BuildDeckLists()
        {
            _playerDeckItems = SpawnDeckList(_playerDeckListContent, ViewModel.SetPlayerDeckIndex);
            _opponentDeckItems = SpawnDeckList(_opponentDeckListContent, ViewModel.SetOpponentDeckIndex);
            RefreshDeckSelection(_playerDeckItems, _playerDeckSelectedText, ViewModel.PlayerDeckIndex.CurrentValue);
            RefreshDeckSelection(_opponentDeckItems, _opponentDeckSelectedText, ViewModel.OpponentDeckIndex.CurrentValue);
        }

        private DevDeckListItemView[] SpawnDeckList(RectTransform content, System.Action<int> onSelect)
        {
            if (!content || !_deckListItemPrefab)
                return System.Array.Empty<DevDeckListItemView>();

            var count = ViewModel.DeckCount;
            var items = new DevDeckListItemView[count];
            for (var i = 0; i < count; i++)
            {
                var item = Instantiate(_deckListItemPrefab, content);
                item.Bind(i, ViewModel.GetDeckDisplayName(i), onSelect);
                items[i] = item;
            }

            return items;
        }

        private void RefreshDeckSelection(DevDeckListItemView[] items, TMP_Text selectedLabel, int selectedIndex)
        {
            if (null != items)
                for (var i = 0; i < items.Length; i++)
                    if (items[i])
                        items[i].SetSelected(i == selectedIndex);

            if (selectedLabel)
                selectedLabel.text = ViewModel.DeckCount > 0 ? ViewModel.GetDeckDisplayName(selectedIndex) : "-";
        }

        private static void UpdateSideVisibility(int modeIndex, GameObject deckRoot, GameObject seedRoot)
        {
            var isRandom = modeIndex == 1;
            if (deckRoot)
                deckRoot.SetActive(false == isRandom);
            if (seedRoot)
                seedRoot.SetActive(isRandom);
        }

        private void CyclePlayerMode()
        {
            ViewModel.SetPlayerModeIndex(ViewModel.PlayerModeIndex.CurrentValue == 0 ? 1 : 0);
        }

        private void CycleOpponentMode()
        {
            ViewModel.SetOpponentModeIndex(ViewModel.OpponentModeIndex.CurrentValue == 0 ? 1 : 0);
        }

        private void CycleStrength()
        {
            if (ViewModel.StrengthCount <= 0)
                return;

            ViewModel.SetStrengthIndex((ViewModel.StrengthIndex.CurrentValue + 1) % ViewModel.StrengthCount);
        }

        private static void UpdateModeLabel(TMP_Text label, int modeIndex)
        {
            if (!label)
                return;

            label.text = modeIndex == 0 ? "PickDeck" : "Random";
        }

        private void UpdateStrengthLabel()
        {
            if (!_strengthButtonText)
                return;

            var index = ViewModel.StrengthIndex.CurrentValue;
            _strengthButtonText.text = ViewModel.StrengthCount > 0 ? ViewModel.GetStrengthDisplayName(index) : "-";
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