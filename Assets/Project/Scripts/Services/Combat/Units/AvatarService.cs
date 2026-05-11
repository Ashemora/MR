using System;
using Project.Scripts.Configs;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleSetup;
using UnityEngine;
using Project.Scripts.Shared.Heroes;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Combat.Units
{
    public class AvatarService : IAvatarService, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly DebugConfig _debugConfig;
        private readonly IAvatarGroupDefenseService _groupDefense;
        private readonly BattleSetup _battleSetup;
        private int _playerHP;
        private int _enemyHP;


        public AvatarService(EventBus eventBus, DebugConfig debugConfig, BattleSetup battleSetup,
            IAvatarGroupDefenseService groupDefense)
        {
            _eventBus = eventBus;
            _debugConfig = debugConfig;
            _groupDefense = groupDefense;
            _battleSetup = battleSetup;
            _playerHP = battleSetup.PlayerAvatar.MaxHP;
            _enemyHP = battleSetup.EnemyAvatar.MaxHP;
        }


        public AvatarState GetAvatar(BattleSide side)
        {
            var setup = GetSetup(side);
            
            return new AvatarState(side, GetCurrentHP(side), setup.MaxHP, setup.IsAssigned);
        }

        public bool IsAlive(BattleSide side)
        {
            return GetAvatar(side).IsAlive;
        }

        public bool IsHpFull(BattleSide side)
        {
            return GetAvatar(side).IsHpFull;
        }

        public void ApplyDamage(BattleSide side, int amount, bool silent = false)
        {
            if (amount <= 0)
                return;

            if (false == _groupDefense.IsExposed(side))
                return;

            if (side == BattleSide.Enemy && _debugConfig.LogCombatDamage)
            {
                var avatar = GetAvatar(side);
                Debug.Log($"[Combat] Damage applied to enemy for {amount} (HP: {avatar.CurrentHP} -> {Math.Max(0, avatar.CurrentHP - amount)}/{avatar.MaxHP})");
            }

            ApplyHealthDelta(side, -amount, silent);
        }

        public void ApplyHeal(BattleSide side, int amount)
        {
            if (amount <= 0)
                return;

            ApplyHealthDelta(side, amount);
        }

        public void ForceApplyDamage(BattleSide side, int amount, bool suppressDefeatedEvent = false)
        {
            if (amount <= 0)
                return;

            ApplyHealthDelta(side, -amount, silent: true, suppressDefeatedEvent);
        }

        public void Dispose()
        {
        }

        private void ApplyHealthDelta(BattleSide side, int delta, bool silent = false,
            bool suppressDefeatedEvent = false)
        {
            var avatar = GetAvatar(side);
            var result = HealthChangeRules.Apply(avatar.CurrentHP, avatar.MaxHP, delta);
            if (false == result.WasChanged)
                return;

            SetCurrentHP(side, result.CurrentHP);
            PublishHPChanged(side, result.CurrentHP, avatar.MaxHP, silent);

            if (result.BecameDefeated && false == suppressDefeatedEvent)
                PublishDefeated(side);
        }

        private BattleUnitSetup GetSetup(BattleSide side)
        {
            return side == BattleSide.Player ? _battleSetup.PlayerAvatar : _battleSetup.EnemyAvatar;
        }

        private int GetCurrentHP(BattleSide side)
        {
            return side == BattleSide.Player ? _playerHP : _enemyHP;
        }

        private void SetCurrentHP(BattleSide side, int currentHP)
        {
            if (side == BattleSide.Player)
            {
                _playerHP = currentHP;
                return;
            }

            _enemyHP = currentHP;
        }

        private void PublishHPChanged(BattleSide side, int currentHP, int maxHP, bool silent)
        {
            if (side == BattleSide.Player)
            {
                _eventBus.Publish(new PlayerHPChangedEvent(currentHP, maxHP, silent));
                return;
            }

            _eventBus.Publish(new EnemyHPChangedEvent(currentHP, maxHP, silent));
        }

        private void PublishDefeated(BattleSide side)
        {
            if (side == BattleSide.Player)
            {
                _eventBus.Publish(new PlayerDefeatedEvent());
                return;
            }

            _eventBus.Publish(new EnemyDefeatedEvent());
        }
    }
}