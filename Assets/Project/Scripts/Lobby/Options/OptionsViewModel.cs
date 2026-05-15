using Project.Scripts.Services.Audio.Settings;
using Project.Scripts.Services.UISystem;
using R3;

namespace Project.Scripts.Lobby.Options
{
    public class OptionsViewModel : BaseViewModel
    {
        private readonly IAudioSettingsService _audioSettings;
        private readonly Subject<Unit> _closeRequested = new();

        public ReadOnlyReactiveProperty<float> MusicVolume => _audioSettings.MusicVolume;
        public ReadOnlyReactiveProperty<float> SfxVolume => _audioSettings.SfxVolume;
        public ReadOnlyReactiveProperty<bool> MusicMuted => _audioSettings.MusicMuted;
        public ReadOnlyReactiveProperty<bool> SfxMuted => _audioSettings.SfxMuted;

        public Observable<Unit> CloseRequested => _closeRequested;

        public OptionsViewModel(IAudioSettingsService audioSettings)
        {
            _audioSettings = audioSettings;
        }


        public void SetMusicVolume(float volume)
        {
            _audioSettings.SetMusicVolume(volume);
        }

        public void SetSfxVolume(float volume)
        {
            _audioSettings.SetSfxVolume(volume);
        }

        public void SetMusicMuted(bool muted)
        {
            _audioSettings.SetMusicMuted(muted);
        }

        public void SetSfxMuted(bool muted)
        {
            _audioSettings.SetSfxMuted(muted);
        }

        public void RequestClose()
        {
            _closeRequested.OnNext(Unit.Default);
        }


        protected override void OnCleanup()
        {
            _closeRequested.Dispose();
        }
    }
}