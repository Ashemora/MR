using Project.Scripts.Services.Audio.AudioSystem;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Project.Scripts.Services.Audio.Settings
{
    public class AudioSettingsService : IAudioSettingsService, IStartable, System.IDisposable
    {
        private const string MusicVolumeKey = "audio.music.volume";
        private const string SfxVolumeKey = "audio.sfx.volume";
        private const string MusicMutedKey = "audio.music.muted";
        private const string SfxMutedKey = "audio.sfx.muted";
        private const float DefaultVolume = 1f;

        private readonly AudioManager _audioManager;

        private readonly ReactiveProperty<float> _musicVolume;
        private readonly ReactiveProperty<float> _sfxVolume;
        private readonly ReactiveProperty<bool> _musicMuted;
        private readonly ReactiveProperty<bool> _sfxMuted;

        private readonly CompositeDisposable _disposables = new();

        public ReadOnlyReactiveProperty<float> MusicVolume { get; }
        public ReadOnlyReactiveProperty<float> SfxVolume { get; }
        public ReadOnlyReactiveProperty<bool> MusicMuted { get; }
        public ReadOnlyReactiveProperty<bool> SfxMuted { get; }

        public AudioSettingsService(AudioManager audioManager)
        {
            _audioManager = audioManager;

            _musicVolume = new ReactiveProperty<float>(LoadVolume(MusicVolumeKey));
            _sfxVolume = new ReactiveProperty<float>(LoadVolume(SfxVolumeKey));
            _musicMuted = new ReactiveProperty<bool>(LoadBool(MusicMutedKey));
            _sfxMuted = new ReactiveProperty<bool>(LoadBool(SfxMutedKey));

            MusicVolume = _musicVolume.ToReadOnlyReactiveProperty();
            SfxVolume = _sfxVolume.ToReadOnlyReactiveProperty();
            MusicMuted = _musicMuted.ToReadOnlyReactiveProperty();
            SfxMuted = _sfxMuted.ToReadOnlyReactiveProperty();
        }

        public void Start()
        {
            ApplyMusic();
            ApplySfx();

            _musicVolume.Subscribe(_ => ApplyMusic()).AddTo(_disposables);
            _musicMuted.Subscribe(_ => ApplyMusic()).AddTo(_disposables);
            _sfxVolume.Subscribe(_ => ApplySfx()).AddTo(_disposables);
            _sfxMuted.Subscribe(_ => ApplySfx()).AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _musicVolume.Dispose();
            _sfxVolume.Dispose();
            _musicMuted.Dispose();
            _sfxMuted.Dispose();
        }

        public void SetMusicVolume(float volume)
        {
            var clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_musicVolume.Value, clamped))
                return;

            _musicVolume.Value = clamped;
            PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        }

        public void SetSfxVolume(float volume)
        {
            var clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(_sfxVolume.Value, clamped))
                return;

            _sfxVolume.Value = clamped;
            PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
        }

        public void SetMusicMuted(bool muted)
        {
            if (_musicMuted.Value == muted)
                return;

            _musicMuted.Value = muted;
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
        }

        public void SetSfxMuted(bool muted)
        {
            if (_sfxMuted.Value == muted)
                return;

            _sfxMuted.Value = muted;
            PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
        }

        public void ToggleMusicMuted()
        {
            SetMusicMuted(false == _musicMuted.Value);
        }

        public void ToggleSfxMuted()
        {
            SetSfxMuted(false == _sfxMuted.Value);
        }


        private void ApplyMusic()
        {
            if (!_audioManager)
                return;

            _audioManager.SetMusicVolume(_musicMuted.Value ? 0f : _musicVolume.Value);
        }

        private void ApplySfx()
        {
            if (!_audioManager)
                return;

            _audioManager.SetSFXVolume(_sfxMuted.Value ? 0f : _sfxVolume.Value);
        }

        private static float LoadVolume(string key)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
        }

        private static bool LoadBool(string key)
        {
            return PlayerPrefs.GetInt(key, 0) == 1;
        }
    }
}