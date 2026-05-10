using System;
using Project.Scripts.Configs.Battle.Layout;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.BattleSetup;
using Project.Scripts.Shared.GroupDefense;
using Project.Scripts.Shared.Heroes;
using R3;

namespace Project.Scripts.Services.Combat.Units
{
    public class AvatarGroupDefenseService : IAvatarGroupDefenseService, IDisposable
    {
        private const int SlotCount = 4;


        private readonly SlotLayoutConfig _slotLayoutConfig;
        private readonly EventBus _eventBus;
        private readonly bool[] _playerDefending = new bool[SlotCount];
        private readonly bool[] _enemyDefending = new bool[SlotCount];
        private readonly ReactiveProperty<AvatarDefenseSnapshot> _playerDefense;
        private readonly ReactiveProperty<AvatarDefenseSnapshot> _enemyDefense;
        private IDisposable _subscription;


        public ReadOnlyReactiveProperty<AvatarDefenseSnapshot> PlayerDefense => _playerDefense;
        public ReadOnlyReactiveProperty<AvatarDefenseSnapshot> EnemyDefense => _enemyDefense;


        public AvatarGroupDefenseService(BattleSetup battleSetup, SlotLayoutConfig slotLayoutConfig,
            EventBus eventBus)
        {
            _slotLayoutConfig = slotLayoutConfig;
            _eventBus = eventBus;

            InitDefending(_playerDefending, battleSetup, BattleSide.Player);
            InitDefending(_enemyDefending, battleSetup, BattleSide.Enemy);

            _playerDefense = new ReactiveProperty<AvatarDefenseSnapshot>(ComputeSnapshot(_playerDefending));
            _enemyDefense = new ReactiveProperty<AvatarDefenseSnapshot>(ComputeSnapshot(_enemyDefending));

            _subscription = _eventBus.Subscribe<HeroDefeatedEvent>(OnHeroDefeated);
        }


        public bool IsExposed(BattleSide side)
        {
            return side == BattleSide.Player
                ? _playerDefense.Value.IsExposed
                : _enemyDefense.Value.IsExposed;
        }

        public AvatarDefenseSnapshot GetSnapshot(BattleSide side)
        {
            return side == BattleSide.Player ? _playerDefense.Value : _enemyDefense.Value;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _playerDefense.Dispose();
            _enemyDefense.Dispose();
        }


        private void OnHeroDefeated(HeroDefeatedEvent e)
        {
            var defending = e.Side == BattleSide.Player ? _playerDefending : _enemyDefending;
            var property = e.Side == BattleSide.Player ? _playerDefense : _enemyDefense;
            var prev = property.Value;

            if (e.SlotIndex >= 0 && e.SlotIndex < SlotCount)
                defending[e.SlotIndex] = false;

            var next = ComputeSnapshot(defending);
            property.Value = next;

            if (next.IsExposed && false == prev.IsExposed)
            {
                var groupId = (false == prev.IsGroup1Destroyed && next.IsGroup1Destroyed)
                    ? HeroGroupId.Group1
                    : HeroGroupId.Group2;

                _eventBus.Publish(new AvatarExposedEvent(e.Side, groupId));
            }
        }

        private AvatarDefenseSnapshot ComputeSnapshot(bool[] defending)
        {
            var g1 = IsGroupDestroyed(defending, _slotLayoutConfig.Group1SlotIndices);
            var g2 = IsGroupDestroyed(defending, _slotLayoutConfig.Group2SlotIndices);
            return new AvatarDefenseSnapshot(g1, g2);
        }

        private static bool IsGroupDestroyed(bool[] defending, int[] groupIndices)
        {
            for (var i = 0; i < groupIndices.Length; i++)
            {
                var idx = groupIndices[i];
                if (idx >= 0 && idx < SlotCount && defending[idx])
                    return false;
            }

            return true;
        }

        private static void InitDefending(bool[] defending, BattleSetup battleSetup, BattleSide side)
        {
            for (var i = 0; i < SlotCount; i++)
                defending[i] = battleSetup.GetHero(side, i).IsAssigned;
        }
    }
}