using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Project.Scripts.Services.Audio.AudioSystem
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer Settings")]
        [SerializeField] public AudioMixer AudioMixer;
        [SerializeField] public AudioMixerGroup MusicMixerGroup;
        [SerializeField] public AudioMixerGroup SFXMixerGroup;


        private AudioSource _bgmSource;
        private List<AudioSource> _sfxSources = new List<AudioSource>();
        private int _currentSFXIndex;
        private int _baseSFXSourcesAmount = 5;


        private void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = false;
            _bgmSource.playOnAwake = false;
            _bgmSource.outputAudioMixerGroup = MusicMixerGroup;
        }

        public AudioSource GetBGMSource()
        {
            return _bgmSource;
        }

        public AudioSource GetSFXSource()
        {
            if (_sfxSources.Count == 0)
                AddSFXSources(_baseSFXSourcesAmount);

            var source = _sfxSources[_currentSFXIndex];
            _currentSFXIndex = (_currentSFXIndex + 1) % _sfxSources.Count;
            return source;
        }

        public void AddSFXSources(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var newSource = gameObject.AddComponent<AudioSource>();
                newSource.playOnAwake = false;
                newSource.outputAudioMixerGroup = SFXMixerGroup;
                _sfxSources.Add(newSource);
            }
        }

        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            var dB = volume > 0 ? 20 * Mathf.Log10(volume) : -80f;
            AudioMixer.SetFloat("MusicVolume", dB);
        }

        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            var dB = volume > 0 ? 20 * Mathf.Log10(volume) : -80f;
            AudioMixer.SetFloat("SFXVolume", dB);
        }

        public float GetMusicVolume()
        {
            float dB;
            if (AudioMixer.GetFloat("MusicVolume", out dB))
            {
                if (dB <= -80f) 
                    return 0f;
                
                return Mathf.Pow(10f, dB / 20f);
            }

            return 1f;
        }

        public float GetSFXVolume()
        {
            float dB;
            if (AudioMixer.GetFloat("SFXVolume", out dB))
            {
                if (dB <= -80f)
                    return 0f;
                
                return Mathf.Pow(10f, dB / 20f);
            }

            return 1f;
        }
    }
}