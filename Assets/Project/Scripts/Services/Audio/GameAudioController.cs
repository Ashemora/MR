using System;
using Project.Scripts.Services.Audio.AudioSystem;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.Heroes;
using Scripts.Services.Audio.AudioSystem.Configs;
using UnityEngine;
using R3;

namespace Project.Scripts.Services.Audio
{
    public class GameAudioController : IDisposable
    {
        private readonly AudioService _audioService;
        private readonly CompositeDisposable _disposables = new();
        private int _prevPlayerHp = int.MaxValue;


        public GameAudioController(AudioService audioService, EventBus eventBus, IGameStateService gameStateService)
        {
            _audioService = audioService;

            eventBus.Subscribe<PlayerHPChangedEvent>(e =>
            {
                if (e.Current < _prevPlayerHp)
                    _audioService.Play(AudioTags.Group_Gameplay, AudioTags.Sound_Hit_01);

                _prevPlayerHp = e.Current;
            }).AddTo(_disposables);

            eventBus.Subscribe<AbilityApplicationEvent>(e =>
            {
                var sound = e.ActionType == HeroActionType.HealAlly
                    ? AudioTags.Sound_Heal_01
                    : AudioTags.Sound_Laser_01;
                _audioService.Play(AudioTags.Group_Gameplay, sound);
            }).AddTo(_disposables);

            eventBus.Subscribe<HeroActivatedEvent>(e =>
            {
                var sound = e.ActionType == HeroActionType.HealAlly
                    ? AudioTags.Sound_Heal_01
                    : AudioTags.Sound_Laser_01;
                _audioService.Play(AudioTags.Group_Gameplay, sound);
            }).AddTo(_disposables);

            eventBus.Subscribe<MatchPlayedEvent>(e =>
            {
                var pitch = Mathf.Clamp(1f + e.CascadeIndex * 0.1f, 1f, 2f);
                _audioService.Play(AudioTags.Group_Gameplay, AudioTags.Sound_Match_01, pitch: pitch);
            }).AddTo(_disposables);

            eventBus.Subscribe<BombActivatedEvent>(_ =>
            {
                _audioService.Play(AudioTags.Group_Gameplay, AudioTags.Sound_Bomb_01);
            }).AddTo(_disposables);

            gameStateService.State.Subscribe(state =>
            {
                if (state == GameState.Win)
                    _audioService.Play(AudioTags.Group_Gameplay, AudioTags.Sound_Fanfares_01);
                else if (state == GameState.Lose)
                    _audioService.Play(AudioTags.Group_Gameplay, AudioTags.Sound_Lose_01);
            }).AddTo(_disposables);
        }

        public void StartMusic()
        {
            _audioService.PlayGroup(AudioTags.Group_MainMusic);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}