using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.Audio.AudioSystem.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scripts.Services.Audio.AudioSystem
{
    public class AudioService : IDisposable
    {
        public Action<string, string> SoundPlayStarted;
        public Action<string, string> SoundPlayStopped;
        
        
        private readonly AudioMusicConfig _audioMusicConfig;
        private readonly AudioSFXConfig _audioSFXConfig;
        private readonly AudioManager _audioManager;
        private readonly Dictionary<string, AudioSource> _activeSources = new();
        private readonly Dictionary<string, CancellationTokenSource> _groupCancellationTokens = new();
        private readonly Dictionary<string, CancellationTokenSource> _groupFadeOutTokens = new();
        private readonly Dictionary<string, CancellationTokenSource> _sfxCancellationTokens = new();
        private readonly Dictionary<string, bool> _groupPaused = new();
        private readonly Dictionary<string, string> _currentPlayingGroups = new();
        private readonly Dictionary<string, List<AudioSource>> _dynamicGroupSources = new();
        
        
        public AudioService(AudioMusicConfig audioMusicConfig, AudioSFXConfig audioSFXConfig,
            AudioManager audioManager)
        {
            _audioMusicConfig = audioMusicConfig;
            _audioSFXConfig = audioSFXConfig;
            _audioManager = audioManager;
        }

        public void Dispose()
        {
            foreach (var cts in _groupCancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _groupCancellationTokens.Clear();

            foreach (var cts in _groupFadeOutTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _groupFadeOutTokens.Clear();

            foreach (var cts in _sfxCancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _sfxCancellationTokens.Clear();

            foreach (var sources in _dynamicGroupSources.Values)
                foreach (var source in sources)
                    UnityEngine.Object.Destroy(source);
            _dynamicGroupSources.Clear();
        }
        
        public void PlayGroup(string groupTag, bool shuffle = false, float fade = 0f)
        {
            CancelFadeOut(groupTag);
            StopGroup(groupTag);
            var group = FindGroupConfig(groupTag);
            if (null == group)
            {
                Debug.LogError($"AudioGroupConfig with tag '{groupTag}' not found.");
                return;
            }
            
            var clips = new List<AudioClipData>(group.Clips);
            if (clips.Count == 0)
            {
                Debug.LogError($"AudioGroupConfig '{groupTag}' has no clips.");
                return;
            }
            
            if (shuffle) 
                clips = Shuffle(clips);
            
            var cts = new CancellationTokenSource();
            _groupCancellationTokens[groupTag] = cts;
            _groupPaused[groupTag] = false;
            
            PlayGroupSequentially(clips, group.Loop, shuffle, groupTag, fade, cts.Token).Forget();
        }

        private async UniTaskVoid PlayGroupSequentially(List<AudioClipData> clips, bool loop, bool shuffle, string groupTag, float fade, CancellationToken token)
        {
            if (fade <= 0f)
            {
                do
                {
                    for (var i = 0; i < clips.Count; i++)
                    {
                        if (token.IsCancellationRequested) 
                            return;
                        
                        var clipData = clips[i];
                        
                        if (null == clipData || !clipData.Clip)
                        {
                            Debug.LogError($"Clip is NULL in group '{groupTag}' at index {i}.");
                            continue;
                        }
                        
                        var source = _audioManager.GetBGMSource();
                        source.clip = clipData.Clip;
                        source.volume = clipData.DefaultVolume;
                        source.Play();
                        SoundPlayStarted?.Invoke(groupTag, clipData.Tag);
                        
                        _activeSources[groupTag] = source;
                        _currentPlayingGroups[groupTag] = clipData.Tag;
                        await UniTask.Delay(TimeSpan.FromSeconds(source.clip.length), cancellationToken: token);
                    }
                    
                    if (shuffle) 
                        clips = Shuffle(clips);
                    
                } while (loop && false == token.IsCancellationRequested);
                
                return;
            }
            var source1 = CreateBGMSource(groupTag);
            var source2 = CreateBGMSource(groupTag);
            var currentSource = source1;
            var nextSource = source2;
            var currentTargetVolume = 0f;
            
            do
            {
                var currentClips = shuffle ? Shuffle(clips) : clips;
                var firstClip = currentClips[0];
                
                if (null == firstClip || !firstClip.Clip)
                {
                    Debug.LogError($"Clip is NULL in group '{groupTag}' at index 0.");
                    return;
                }
                
                currentSource.clip = firstClip.Clip;
                currentTargetVolume = firstClip.DefaultVolume;
                currentSource.volume = 0f;
                currentSource.Play();
                SoundPlayStarted?.Invoke(groupTag, firstClip.Tag);
                _activeSources[groupTag] = currentSource;
                _currentPlayingGroups[groupTag] = firstClip.Tag;

                var fadeInStart = Time.time;
                while (Time.time - fadeInStart < fade)
                {
                    var t = (Time.time - fadeInStart) / fade;
                    currentSource.volume = Mathf.Lerp(0f, currentTargetVolume, t);
                    await UniTask.Yield(PlayerLoopTiming.Update);

                    if (token.IsCancellationRequested)
                        return;
                }
                currentSource.volume = currentTargetVolume;
                
                for (var i = 1; i < currentClips.Count; i++)
                {
                    if (token.IsCancellationRequested)
                        return;
                    
                    var waitTime = Mathf.Max(currentSource.clip.length - fade, 0f);
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);
                    var clipData = currentClips[i];
                    
                    if (null == clipData || clipData.Clip == false)
                    {
                        Debug.LogError($"Clip is NULL in group '{groupTag}' at index {i}.");
                        continue;
                    }
                    
                    nextSource.clip = clipData.Clip;
                    var nextTargetVolume = clipData.DefaultVolume;
                    nextSource.volume = 0f;
                    nextSource.Play();
                    SoundPlayStarted?.Invoke(groupTag, clipData.Tag);
                    var startTime = Time.time;
                    
                    while (Time.time - startTime < fade)
                    {
                        var t = (Time.time - startTime) / fade;
                        currentSource.volume = Mathf.Lerp(currentTargetVolume, 0f, t);
                        nextSource.volume = Mathf.Lerp(0f, nextTargetVolume, t);
                        await UniTask.Yield(PlayerLoopTiming.Update);
                        
                        if (token.IsCancellationRequested)
                            return;
                    }
                    
                    currentSource.volume = 0f;
                    nextSource.volume = nextTargetVolume;
                    currentSource.Stop();
                    (currentSource, nextSource) = (nextSource, currentSource);
                    currentTargetVolume = nextTargetVolume;
                    _activeSources[groupTag] = currentSource;
                }
                
                if (loop)
                {
                    var firstCycleClip = currentClips[0];
                    if (null == firstCycleClip || !firstCycleClip.Clip)
                    {
                        Debug.LogError($"Clip is NULL in group '{groupTag}' at index 0 for loop.");
                        return;
                    }
                    
                    var waitTime = Mathf.Max(currentSource.clip.length - fade, 0f);
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);
                    nextSource.clip = firstCycleClip.Clip;
                    var nextTargetVolume = firstCycleClip.DefaultVolume;
                    nextSource.volume = 0f;
                    nextSource.Play();
                    SoundPlayStarted?.Invoke(groupTag, firstCycleClip.Tag);
                    var startTime = Time.time;
                    
                    while (Time.time - startTime < fade)
                    {
                        var t = (Time.time - startTime) / fade;
                        currentSource.volume = Mathf.Lerp(currentTargetVolume, 0f, t);
                        nextSource.volume = Mathf.Lerp(0f, nextTargetVolume, t);
                        await UniTask.Yield(PlayerLoopTiming.Update);
                        
                        if (token.IsCancellationRequested)
                            return;
                    }
                    currentSource.volume = 0f;
                    nextSource.volume = nextTargetVolume;
                    currentSource.Stop();
                    (currentSource, nextSource) = (nextSource, currentSource);
                    currentTargetVolume = nextTargetVolume;
                    _activeSources[groupTag] = currentSource;
                }
            } while (loop && !token.IsCancellationRequested);
        }

        private AudioSource CreateBGMSource(string groupTag)
        {
            var newSource = _audioManager.gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            newSource.outputAudioMixerGroup = _audioManager.MusicMixerGroup;
            if (!_dynamicGroupSources.ContainsKey(groupTag))
                _dynamicGroupSources[groupTag] = new List<AudioSource>();
            _dynamicGroupSources[groupTag].Add(newSource);
            return newSource;
        }

        public void Play(string groupTag, string soundTag, float pitch = 1f, bool interrupt = false)
        {
            if (interrupt && _activeSources.ContainsKey(soundTag)) 
                Stop(groupTag, soundTag);

            var group = FindGroupConfig(groupTag);
            if (null == group)
            {
                Debug.LogError($"AudioGroupConfig with tag '{groupTag}' not found.");
                return;
            }
            
            var clipData = FindClipConfig(group, soundTag);
            if (null == clipData)
            {
                Debug.LogError($"Clip with tag '{soundTag}' not found in group '{groupTag}'.");
                return;
            }
            
            var cts = new CancellationTokenSource();
            _sfxCancellationTokens[soundTag] = cts;
            var source = _audioManager.GetSFXSource();
            source.clip = clipData.Clip;
            source.volume = clipData.DefaultVolume;
            source.pitch = pitch;
            source.Play();
            SoundPlayStarted?.Invoke(groupTag, soundTag);
            _activeSources[soundTag] = source;

            PlayAndResetPitch(source, clipData.Clip.length, groupTag, soundTag, cts.Token).Forget();
        }

        private async UniTaskVoid PlayAndResetPitch(AudioSource source, float duration, string groupTag, string soundTag, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
                if (!token.IsCancellationRequested && source)
                {
                    source.pitch = 1f;
                    _activeSources.Remove(soundTag);
                    _sfxCancellationTokens.Remove(soundTag);
                    SoundPlayStopped?.Invoke(groupTag, soundTag);
                }
            }
            catch (OperationCanceledException)
            {
                if (source)
                    source.pitch = 1f;
            }
        }

        public void StopGroup(string groupTag, float fade = 0f)
        {
            if (_groupCancellationTokens.TryGetValue(groupTag, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _groupCancellationTokens.Remove(groupTag);
            }

            if (fade <= 0f)
            {
                CancelFadeOut(groupTag);
                FinalizeGroupStop(groupTag);
                return;
            }

            CancelFadeOut(groupTag);
            var fadeCts = new CancellationTokenSource();
            _groupFadeOutTokens[groupTag] = fadeCts;
            FadeOutAndFinalize(groupTag, fade, fadeCts.Token).Forget();
        }

        private async UniTaskVoid FadeOutAndFinalize(string groupTag, float fade, CancellationToken token)
        {
            var sources = new List<AudioSource>();
            if (_activeSources.TryGetValue(groupTag, out var activeSource) && activeSource)
                sources.Add(activeSource);
            if (_dynamicGroupSources.TryGetValue(groupTag, out var dynamicSources))
            {
                for (var i = 0; i < dynamicSources.Count; i++)
                {
                    var s = dynamicSources[i];
                    if (s && false == sources.Contains(s))
                        sources.Add(s);
                }
            }

            if (sources.Count == 0)
            {
                FinalizeGroupStop(groupTag);
                _groupFadeOutTokens.Remove(groupTag);
                return;
            }

            var startVolumes = new float[sources.Count];
            for (var i = 0; i < sources.Count; i++)
                startVolumes[i] = sources[i].volume;

            var startTime = Time.time;
            try
            {
                while (Time.time - startTime < fade)
                {
                    var t = (Time.time - startTime) / fade;
                    for (var i = 0; i < sources.Count; i++)
                    {
                        if (sources[i])
                            sources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i])
                    sources[i].volume = 0f;
            }

            FinalizeGroupStop(groupTag);
            if (_groupFadeOutTokens.TryGetValue(groupTag, out var stored) && stored != null)
            {
                stored.Dispose();
                _groupFadeOutTokens.Remove(groupTag);
            }
        }

        private void FinalizeGroupStop(string groupTag)
        {
            if (_dynamicGroupSources.TryGetValue(groupTag, out var sources))
            {
                for (var i = 0; i < sources.Count; i++)
                    UnityEngine.Object.Destroy(sources[i]);
                _dynamicGroupSources.Remove(groupTag);
            }
            if (_activeSources.TryGetValue(groupTag, out var source))
            {
                if (source)
                {
                    source.Stop();
                    source.pitch = 1f;
                }

                _activeSources.Remove(groupTag);
                _currentPlayingGroups.Remove(groupTag);
                SoundPlayStopped?.Invoke(groupTag, "");
            }
        }

        private void CancelFadeOut(string groupTag)
        {
            if (false == _groupFadeOutTokens.TryGetValue(groupTag, out var cts))
                return;

            cts.Cancel();
            cts.Dispose();
            _groupFadeOutTokens.Remove(groupTag);
        }

        public void Stop(string groupTag, string soundTag)
        {
            if (_currentPlayingGroups.TryGetValue(groupTag, out var playingSound) && playingSound == soundTag)
                StopGroup(groupTag);
            else if (_sfxCancellationTokens.TryGetValue(soundTag, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _sfxCancellationTokens.Remove(soundTag);
            }
            if (_activeSources.TryGetValue(soundTag, out var source))
            {
                if (source)
                {
                    source.Stop();
                    source.pitch = 1f;
                }
                
                _activeSources.Remove(soundTag);
                SoundPlayStopped?.Invoke(groupTag, soundTag);
            }
        }

        public void PauseGroup(string groupTag)
        {
            if (_activeSources.TryGetValue(groupTag, out var source))
            {
                source.Pause();
                _groupPaused[groupTag] = true;
                Debug.Log($"Paused group: {groupTag}");
            }
        }

        public void Pause(string groupTag, string soundTag)
        {
            if (_activeSources.TryGetValue(soundTag, out var source))
            {
                source.Pause();
                Debug.Log($"Paused sound: {soundTag}");
            }
        }

        public void ResumeGroup(string groupTag)
        {
            if (_groupPaused.TryGetValue(groupTag, out var isPaused) && isPaused)
            {
                if (_activeSources.TryGetValue(groupTag, out var source))
                {
                    source.UnPause();
                    _groupPaused[groupTag] = false;
                    Debug.Log($"Resumed group: {groupTag}");
                }
            }
        }

        public void Resume(string groupTag, string soundTag)
        {
            if (_activeSources.TryGetValue(soundTag, out var source))
            {
                source.UnPause();
                Debug.Log($"Resumed sound: {soundTag}");
            }
        }

        private AudioGroupData FindGroupConfig(string groupTag)
        {
            for (var i = 0; i < _audioMusicConfig.Groups.Count; i++)
            {
                if (_audioMusicConfig.Groups[i].GroupTag == groupTag)
                    return _audioMusicConfig.Groups[i];
            }
            
            for (var i = 0; i < _audioSFXConfig.Groups.Count; i++)
            {
                if (_audioSFXConfig.Groups[i].GroupTag == groupTag)
                    return _audioSFXConfig.Groups[i];
            }
            
            Debug.LogError($"Group with tag {groupTag} not found.");
            return null;
        }
        
        private AudioClipData FindClipConfig(AudioGroupData group, string tag)
        {
            for (var i = 0; i < group.Clips.Count; i++)
            {
                if (group.Clips[i].Tag == tag)
                    return group.Clips[i];
            }
            
            Debug.LogError($"Clip with tag '{tag}' not found in group '{group.GroupTag}'.");
            return null;
        }
        
        private List<AudioClipData> Shuffle(List<AudioClipData> clips)
        {
            var shuffled = new List<AudioClipData>(clips);
            var count = shuffled.Count;
            
            for (var i = 0; i < count; i++)
            {
                var randomIndex = Random.Range(i, count);
                (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
            }
            
            return shuffled;
        }
    }
}