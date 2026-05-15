using R3;

namespace Project.Scripts.Services.Audio.Settings
{
    public interface IAudioSettingsService
    {
        ReadOnlyReactiveProperty<float> MusicVolume { get; }
        ReadOnlyReactiveProperty<float> SfxVolume { get; }
        ReadOnlyReactiveProperty<bool> MusicMuted { get; }
        ReadOnlyReactiveProperty<bool> SfxMuted { get; }

        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
        void SetMusicMuted(bool muted);
        void SetSfxMuted(bool muted);
        void ToggleMusicMuted();
        void ToggleSfxMuted();
    }
}