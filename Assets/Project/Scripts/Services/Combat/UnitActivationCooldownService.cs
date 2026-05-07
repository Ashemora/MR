using System;
using Project.Scripts.Configs.Battle;
using Project.Scripts.Configs.Levels;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Rules;

namespace Project.Scripts.Services.Combat
{
    public class UnitActivationCooldownService : IUnitActivationCooldownService, IDisposable
    {
        private const int SlotCount = 4;
        private const float MinPublishDelta = 0.01f;


        private readonly EventBus _eventBus;
        private readonly IHeroCooldownModifierService _heroCooldownModifierService;
        private readonly float _minUnitActivationCooldownSeconds;
        private readonly float[] _playerHeroDurations = new float[SlotCount];
        private readonly float[] _enemyHeroDurations = new float[SlotCount];
        private readonly float[] _playerHeroActiveDurations = new float[SlotCount];
        private readonly float[] _enemyHeroActiveDurations = new float[SlotCount];
        private readonly float[] _playerHeroRemaining = new float[SlotCount];
        private readonly float[] _enemyHeroRemaining = new float[SlotCount];
        private readonly float[] _playerHeroLastPublished = new float[SlotCount];
        private readonly float[] _enemyHeroLastPublished = new float[SlotCount];
        private readonly float _playerAvatarDuration;
        private readonly float _enemyAvatarDuration;
        private float _playerAvatarActiveDuration;
        private float _enemyAvatarActiveDuration;
        private float _playerAvatarRemaining;
        private float _enemyAvatarRemaining;
        private float _playerAvatarLastPublished = -1f;
        private float _enemyAvatarLastPublished = -1f;
        private IDisposable _heroDefeatedSubscription;
        private IDisposable _playerDefeatedSubscription;
        private IDisposable _enemyDefeatedSubscription;


        public UnitActivationCooldownService(EventBus eventBus, LevelConfig levelConfig, BattleFlowConfig battleFlowConfig,
            IHeroCooldownModifierService heroCooldownModifierService)
        {
            _eventBus = eventBus;
            _heroCooldownModifierService = heroCooldownModifierService;
            _minUnitActivationCooldownSeconds = battleFlowConfig.MinUnitActivationCooldownSeconds;
            FillHeroDurations(_playerHeroDurations, levelConfig.PlayerHeroes);
            FillHeroDurations(_enemyHeroDurations, levelConfig.EnemyHeroes);
            _playerAvatarDuration = levelConfig.PlayerAvatarConfig ? levelConfig.PlayerAvatarConfig.ActivationCooldownSeconds : 0f;
            _enemyAvatarDuration = levelConfig.EnemyAvatarConfig ? levelConfig.EnemyAvatarConfig.ActivationCooldownSeconds : 0f;

            _heroDefeatedSubscription = _eventBus.Subscribe<HeroDefeatedEvent>(OnHeroDefeated);
            _playerDefeatedSubscription = _eventBus.Subscribe<PlayerDefeatedEvent>(_ => ResetAvatarCooldown(BattleSide.Player));
            _enemyDefeatedSubscription = _eventBus.Subscribe<EnemyDefeatedEvent>(_ => ResetAvatarCooldown(BattleSide.Enemy));
        }


        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            TickHeroSide(BattleSide.Player, _playerHeroRemaining, _playerHeroActiveDurations,
                _playerHeroLastPublished, deltaTime);
            TickHeroSide(BattleSide.Enemy, _enemyHeroRemaining, _enemyHeroActiveDurations,
                _enemyHeroLastPublished, deltaTime);
            TickAvatar(BattleSide.Player, _playerAvatarActiveDuration, ref _playerAvatarRemaining,
                ref _playerAvatarLastPublished, deltaTime);
            TickAvatar(BattleSide.Enemy, _enemyAvatarActiveDuration, ref _enemyAvatarRemaining,
                ref _enemyAvatarLastPublished, deltaTime);
        }

        public bool IsHeroOnCooldown(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return false;

            return GetHeroRemaining(side)[slotIndex] > 0f;
        }

        public bool IsAvatarOnCooldown(BattleSide side)
        {
            return side == BattleSide.Player ? _playerAvatarRemaining > 0f : _enemyAvatarRemaining > 0f;
        }

        public void StartHeroCooldown(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return;

            var durations = GetHeroDurations(side);
            var activeDurations = GetHeroActiveDurations(side);
            var remaining = GetHeroRemaining(side);
            var duration = GetModifiedHeroDuration(side, slotIndex, durations[slotIndex]);
            if (duration <= 0f)
                return;

            activeDurations[slotIndex] = duration;
            remaining[slotIndex] = duration;
            PublishHeroCooldown(side, slotIndex, duration, duration);
        }

        public void StartAvatarCooldown(BattleSide side)
        {
            if (side == BattleSide.Player)
            {
                var duration = GetModifiedAvatarDuration(_playerAvatarDuration);
                if (duration <= 0f)
                    return;

                _playerAvatarRemaining = duration;
                _playerAvatarActiveDuration = duration;
                PublishAvatarCooldown(BattleSide.Player, _playerAvatarRemaining, duration);
                
                return;
            }

            var enemyDuration = GetModifiedAvatarDuration(_enemyAvatarDuration);
            if (enemyDuration <= 0f)
                return;

            _enemyAvatarRemaining = enemyDuration;
            _enemyAvatarActiveDuration = enemyDuration;
            PublishAvatarCooldown(BattleSide.Enemy, _enemyAvatarRemaining, enemyDuration);
        }

        public void Dispose()
        {
            _heroDefeatedSubscription?.Dispose();
            _heroDefeatedSubscription = null;
            _playerDefeatedSubscription?.Dispose();
            _playerDefeatedSubscription = null;
            _enemyDefeatedSubscription?.Dispose();
            _enemyDefeatedSubscription = null;
        }


        private void TickHeroSide(BattleSide side, float[] remaining, float[] durations, float[] lastPublished, float deltaTime)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (remaining[i] <= 0f)
                    continue;

                remaining[i] -= deltaTime;
                if (remaining[i] < 0f)
                    remaining[i] = 0f;

                if (lastPublished[i] < 0f || Abs(lastPublished[i] - remaining[i]) >= MinPublishDelta || remaining[i] <= 0f)
                {
                    lastPublished[i] = remaining[i];
                    PublishHeroCooldown(side, i, remaining[i], durations[i]);
                }
            }
        }

        private void TickAvatar(BattleSide side, float duration, ref float remaining, ref float lastPublished, float deltaTime)
        {
            if (remaining <= 0f)
                return;

            remaining -= deltaTime;
            if (remaining < 0f)
                remaining = 0f;

            if (lastPublished < 0f || Abs(lastPublished - remaining) >= MinPublishDelta || remaining <= 0f)
            {
                lastPublished = remaining;
                PublishAvatarCooldown(side, remaining, duration);
            }
        }

        private void OnHeroDefeated(HeroDefeatedEvent e)
        {
            ResetHeroCooldown(e.Side, e.SlotIndex);
        }

        private void ResetHeroCooldown(BattleSide side, int slotIndex)
        {
            if (slotIndex is < 0 or >= SlotCount)
                return;

            var remaining = GetHeroRemaining(side);
            if (remaining[slotIndex] <= 0f)
                return;

            remaining[slotIndex] = 0f;
            GetHeroLastPublished(side)[slotIndex] = 0f;
            PublishHeroCooldown(side, slotIndex, 0f, GetHeroActiveDurations(side)[slotIndex]);
        }

        private void ResetAvatarCooldown(BattleSide side)
        {
            if (side == BattleSide.Player)
            {
                if (_playerAvatarRemaining <= 0f)
                    return;

                _playerAvatarRemaining = 0f;
                _playerAvatarLastPublished = 0f;
                PublishAvatarCooldown(BattleSide.Player, 0f, _playerAvatarActiveDuration);
                return;
            }

            if (_enemyAvatarRemaining <= 0f)
                return;

            _enemyAvatarRemaining = 0f;
            _enemyAvatarLastPublished = 0f;
            PublishAvatarCooldown(BattleSide.Enemy, 0f, _enemyAvatarActiveDuration);
        }

        private void PublishHeroCooldown(BattleSide side, int slotIndex, float remaining, float duration)
        {
            _eventBus.Publish(new HeroCooldownChangedEvent(side, slotIndex, remaining, duration));
        }

        private void PublishAvatarCooldown(BattleSide side, float remaining, float duration)
        {
            _eventBus.Publish(new AvatarCooldownChangedEvent(side, remaining, duration));
        }

        private float[] GetHeroRemaining(BattleSide side)
        {
            return side == BattleSide.Player ? _playerHeroRemaining : _enemyHeroRemaining;
        }

        private float[] GetHeroDurations(BattleSide side)
        {
            return side == BattleSide.Player ? _playerHeroDurations : _enemyHeroDurations;
        }

        private float[] GetHeroActiveDurations(BattleSide side)
        {
            return side == BattleSide.Player ? _playerHeroActiveDurations : _enemyHeroActiveDurations;
        }

        private float[] GetHeroLastPublished(BattleSide side)
        {
            return side == BattleSide.Player ? _playerHeroLastPublished : _enemyHeroLastPublished;
        }

        private float GetModifiedHeroDuration(BattleSide side, int slotIndex, float baseDuration)
        {
            var modifiedDuration = _heroCooldownModifierService.GetActivationCooldown(side, slotIndex, baseDuration);
            
            return CooldownRules.ApplyMinimumUnitActivationCooldown(modifiedDuration, _minUnitActivationCooldownSeconds);
        }

        private float GetModifiedAvatarDuration(float baseDuration)
        {
            return CooldownRules.ApplyMinimumUnitActivationCooldown(baseDuration, _minUnitActivationCooldownSeconds);
        }

        private static void FillHeroDurations(float[] target, Project.Scripts.Configs.Battle.HeroConfig[] configs)
        {
            for (var i = 0; i < target.Length; i++)
                target[i] = configs != null && i < configs.Length && configs[i] ? configs[i].ActivationCooldownSeconds : 0f;
        }

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}