using System;
using Project.Scripts.Configs.Battle.Flow;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.Game;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Timer;
using Random = System.Random;

namespace Project.Scripts.Services.Timer
{
    public class BurndownService : IBurndownService
    {
        public bool IsActive => _isActive;
        
        
        private readonly BurndownConfig _config;
        private readonly IHeroService _heroService;
        private readonly IAvatarService _avatarService;
        private readonly EventBus _eventBus;
        private readonly IGameStateService _gameStateService;
        private readonly Random _random = new();

        private bool _isActive;
        private float _drainAccumulator;
        private BurndownDrainCursor _playerCursor;
        private BurndownDrainCursor _enemyCursor;


        public BurndownService(BurndownConfig config, IHeroService heroService, IAvatarService avatarService,
            EventBus eventBus, IGameStateService gameStateService)
        {
            _config = config;
            _heroService = heroService;
            _avatarService = avatarService;
            _eventBus = eventBus;
            _gameStateService = gameStateService;
        }


        public void Begin()
        {
            if (_isActive)
                return;

            _isActive = true;
            _drainAccumulator = 0f;

            _playerCursor.Initialize(_heroService.GetSlots(BattleSide.Player));
            _enemyCursor.Initialize(_heroService.GetSlots(BattleSide.Enemy));

            _eventBus.Publish(new BurndownDrainTargetChangedEvent(BattleSide.Player, _playerCursor.TargetIndex));
            _eventBus.Publish(new BurndownDrainTargetChangedEvent(BattleSide.Enemy, _enemyCursor.TargetIndex));
        }

        public void Tick(float deltaTime)
        {
            if (false == _isActive)
                return;

            var state = _gameStateService.State.CurrentValue;
            if (state == GameState.Win || state == GameState.Lose)
                return;

            _drainAccumulator += deltaTime;

            if (_drainAccumulator < _config.DrainTickInterval)
                return;

            var ticks = (int)(_drainAccumulator / _config.DrainTickInterval);
            _drainAccumulator -= ticks * _config.DrainTickInterval;

            var heroDamage = (int)Math.Ceiling(_config.HeroDrainPerSecond * _config.DrainTickInterval * ticks);
            var avatarDamage = (int)Math.Ceiling(_config.AvatarDrainPerSecond * _config.DrainTickInterval * ticks);

            var playerHpBefore = _avatarService.GetAvatar(BattleSide.Player).CurrentHP;
            var enemyHpBefore = _avatarService.GetAvatar(BattleSide.Enemy).CurrentHP;

            DrainSide(BattleSide.Player, ref _playerCursor, heroDamage, avatarDamage);
            DrainSide(BattleSide.Enemy, ref _enemyCursor, heroDamage, avatarDamage);

            var playerDied = playerHpBefore > 0 && _avatarService.GetAvatar(BattleSide.Player).CurrentHP <= 0;
            var enemyDied = enemyHpBefore > 0 && _avatarService.GetAvatar(BattleSide.Enemy).CurrentHP <= 0;

            if (playerDied && enemyDied)
                ResolveSimultaneousDeath(playerHpBefore, enemyHpBefore);
            else if (playerDied)
                _eventBus.Publish(new PlayerDefeatedEvent());
            else if (enemyDied)
                _eventBus.Publish(new EnemyDefeatedEvent());
        }


        private void ResolveSimultaneousDeath(int playerHpBefore, int enemyHpBefore)
        {
            bool playerWins;
            if (playerHpBefore != enemyHpBefore)
                playerWins = playerHpBefore > enemyHpBefore;
            else
                playerWins = _random.Next(2) == 0; // TODO: replace with draw system

            if (playerWins)
                _eventBus.Publish(new EnemyDefeatedEvent());
            else
                _eventBus.Publish(new PlayerDefeatedEvent());
        }

        private void DrainSide(BattleSide side, ref BurndownDrainCursor cursor, int heroDamage, int avatarDamage)
        {
            var slots = _heroService.GetSlots(side);

            if (cursor.AdvanceIfDead(slots))
                _eventBus.Publish(new BurndownDrainTargetChangedEvent(side, cursor.TargetIndex));

            if (cursor.IsDrainingAvatar)
            {
                _avatarService.ForceApplyDamage(side, avatarDamage, suppressDefeatedEvent: true);
                return;
            }

            var idx = cursor.TargetIndex;
            _heroService.ApplyDamageToHero(side, idx, heroDamage, silent: true);

            if (false == slots[idx].IsAlive)
            {
                cursor.AdvanceIfDead(slots);
                _eventBus.Publish(new BurndownDrainTargetChangedEvent(side, cursor.TargetIndex));
            }
        }
    }
}