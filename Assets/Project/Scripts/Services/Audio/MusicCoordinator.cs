using System;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.Audio.AudioSystem;
using R3;
using Scripts.Services.Audio.AudioSystem.Configs;
using VContainer.Unity;

namespace Project.Scripts.Services.Audio
{
    public class MusicCoordinator : IStartable, IDisposable
    {
        private const float CrossfadeDuration = 0.5f;

        
        private readonly AudioService _audioService;
        private readonly IAppStateMachine _appStateMachine;
        private readonly CompositeDisposable _disposables = new();
        private string _currentGroup;


        public MusicCoordinator(AudioService audioService, IAppStateMachine appStateMachine)
        {
            _audioService = audioService;
            _appStateMachine = appStateMachine;
        }

        public void Start()
        {
            _appStateMachine.State
                .Subscribe(OnAppStateChanged)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            if (null != _currentGroup)
            {
                _audioService.StopGroup(_currentGroup);
                _currentGroup = null;
            }
        }


        private void OnAppStateChanged(AppState state)
        {
            var targetGroup = ResolveGroupForState(state);
            if (targetGroup == _currentGroup)
                return;

            if (null != _currentGroup)
                _audioService.StopGroup(_currentGroup, CrossfadeDuration);

            if (null != targetGroup)
                _audioService.PlayGroup(targetGroup, fade: CrossfadeDuration);

            _currentGroup = targetGroup;
        }

        private static string ResolveGroupForState(AppState state)
        {
            switch (state)
            {
                case AppState.Gameplay:
                    return AudioTags.Group_MainMusic;
                case AppState.Boot:
                case AppState.Lobby:
                case AppState.LoadingGameplay:
                default:
                    return null;
            }
        }
    }
}