#if DEV
using Project.Scripts.Dev;
using Project.Scripts.Services.UISystem;
using R3;

namespace Project.Scripts.UI.Dev
{
    public class DevBattleOptionsViewModel : BaseViewModel
    {
        public ReactiveProperty<int> PlayerModeIndex { get; } = new();
        public ReactiveProperty<int> PlayerDeckIndex { get; } = new();
        public ReactiveProperty<string> PlayerSeedText { get; } = new(string.Empty);
        public ReactiveProperty<int> OpponentModeIndex { get; } = new();
        public ReactiveProperty<int> OpponentDeckIndex { get; } = new();
        public ReactiveProperty<string> OpponentSeedText { get; } = new(string.Empty);
        public ReactiveProperty<int> StrengthIndex { get; } = new();
        public ReactiveProperty<bool> SkipFillsBotEnergy { get; } = new();
        public Observable<Unit> CloseRequested => _closeRequested;
        public int StrengthCount => _override.StrengthCount;
        public int DeckCount => _override.DeckCount;


        private readonly IDevMatchOverrideService _override;
        private readonly Subject<Unit> _closeRequested = new();


        public DevBattleOptionsViewModel(IDevMatchOverrideService devMatchOverride)
        {
            _override = devMatchOverride;
            PlayerModeIndex.Value = (int)_override.PlayerMode;
            PlayerDeckIndex.Value = _override.PlayerDeckIndex;
            PlayerSeedText.Value = SeedToText(_override.PlayerSeedOverride);
            OpponentModeIndex.Value = (int)_override.OpponentMode;
            OpponentDeckIndex.Value = _override.OpponentDeckIndex;
            OpponentSeedText.Value = SeedToText(_override.OpponentSeedOverride);
            StrengthIndex.Value = _override.StrengthIndex;
            SkipFillsBotEnergy.Value = _override.SkipFillsBotEnergy;

            PlayerModeIndex.AddTo(Disposables);
            PlayerDeckIndex.AddTo(Disposables);
            PlayerSeedText.AddTo(Disposables);
            OpponentModeIndex.AddTo(Disposables);
            OpponentDeckIndex.AddTo(Disposables);
            OpponentSeedText.AddTo(Disposables);
            StrengthIndex.AddTo(Disposables);
            SkipFillsBotEnergy.AddTo(Disposables);
        }


        public string GetStrengthDisplayName(int index)
        {
            return _override.GetStrengthDisplayName(index);
        }

        public string GetDeckDisplayName(int index)
        {
            return _override.GetDeckDisplayName(index);
        }

        public void SetPlayerModeIndex(int index)
        {
            PlayerModeIndex.Value = index;
        }

        public void SetPlayerDeckIndex(int index)
        {
            PlayerDeckIndex.Value = index;
        }

        public void SetPlayerSeedText(string text)
        {
            PlayerSeedText.Value = text ?? string.Empty;
        }

        public void SetOpponentModeIndex(int index)
        {
            OpponentModeIndex.Value = index;
        }

        public void SetOpponentDeckIndex(int index)
        {
            OpponentDeckIndex.Value = index;
        }

        public void SetOpponentSeedText(string text)
        {
            OpponentSeedText.Value = text ?? string.Empty;
        }

        public void SetStrengthIndex(int index)
        {
            StrengthIndex.Value = index;
        }

        public void SetSkipFillsBotEnergy(bool value)
        {
            SkipFillsBotEnergy.Value = value;
        }

        public void RequestClose()
        {
            var playerMode = (DevSideMode)PlayerModeIndex.CurrentValue;
            var opponentMode = (DevSideMode)OpponentModeIndex.CurrentValue;

            _override.SetPlayerMode(playerMode);
            _override.SetPlayerDeckIndex(PlayerDeckIndex.CurrentValue);
            _override.SetPlayerSeedOverride(playerMode == DevSideMode.Random
                ? ParseSeed(PlayerSeedText.CurrentValue)
                : null);
            _override.SetOpponentMode(opponentMode);
            _override.SetOpponentDeckIndex(OpponentDeckIndex.CurrentValue);
            _override.SetOpponentSeedOverride(opponentMode == DevSideMode.Random
                ? ParseSeed(OpponentSeedText.CurrentValue)
                : null);
            _override.SetStrengthIndex(StrengthIndex.CurrentValue);
            _override.SetSkipFillsBotEnergy(SkipFillsBotEnergy.CurrentValue);
            _override.Save();
            _closeRequested.OnNext(Unit.Default);
        }


        protected override void OnCleanup()
        {
            _closeRequested.Dispose();
        }


        private static int? ParseSeed(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return int.TryParse(text, out var seed) ? seed : null;
        }

        private static string SeedToText(int? seed)
        {
            return seed.HasValue ? seed.Value.ToString() : string.Empty;
        }
    }
}
#endif