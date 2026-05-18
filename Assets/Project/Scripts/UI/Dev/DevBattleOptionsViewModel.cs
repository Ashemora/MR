#if DEV
using Project.Scripts.Dev;
using Project.Scripts.Services.UISystem;
using R3;

namespace Project.Scripts.UI.Dev
{
    public class DevBattleOptionsViewModel : BaseViewModel
    {
        public ReactiveProperty<int> ModeIndex { get; } = new();
        public ReactiveProperty<int> StrengthIndex { get; } = new();
        public ReactiveProperty<string> OpponentSeedText { get; } = new(string.Empty);
        public Observable<Unit> CloseRequested => _closeRequested;
        public int StrengthCount => _devOpponentOverride.StrengthCount;
        
        
        private readonly IDevOpponentOverrideService _devOpponentOverride;
        private readonly Subject<Unit> _closeRequested = new();


        public DevBattleOptionsViewModel(IDevOpponentOverrideService devOpponentOverride)
        {
            _devOpponentOverride = devOpponentOverride;
            ModeIndex.Value = (int)_devOpponentOverride.Mode;
            StrengthIndex.Value = _devOpponentOverride.StrengthIndex;
            OpponentSeedText.Value = _devOpponentOverride.OpponentSeedOverride.HasValue
                ? _devOpponentOverride.OpponentSeedOverride.Value.ToString()
                : string.Empty;

            ModeIndex.AddTo(Disposables);
            StrengthIndex.AddTo(Disposables);
            OpponentSeedText.AddTo(Disposables);
        }

        public string GetStrengthDisplayName(int index)
        {
            return _devOpponentOverride.GetStrengthDisplayName(index);
        }

        public void SetModeIndex(int index)
        {
            ModeIndex.Value = index;
        }

        public void SetStrengthIndex(int index)
        {
            StrengthIndex.Value = index;
        }

        public void SetOpponentSeedText(string text)
        {
            OpponentSeedText.Value = text ?? string.Empty;
        }

        public void RequestClose()
        {
            var isRandom = ModeIndex.CurrentValue == (int)DevOpponentMode.Random;

            _devOpponentOverride.SetMode((DevOpponentMode)ModeIndex.CurrentValue);
            _devOpponentOverride.SetStrengthIndex(StrengthIndex.CurrentValue);
            _devOpponentOverride.SetOpponentSeedOverride(isRandom
                ? ParseOpponentSeed(OpponentSeedText.CurrentValue)
                : null);
            _devOpponentOverride.Save();
            _closeRequested.OnNext(Unit.Default);
        }


        protected override void OnCleanup()
        {
            _closeRequested.Dispose();
        }
        

        private static int? ParseOpponentSeed(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return int.TryParse(text, out var seed)
                ? seed
                : null;
        }
    }
}
#endif