using System;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Layout
{
    [Serializable]
    public struct BattleFieldLayoutSnapshot
    {
        [Min(0.01f)]
        [SerializeField] private float _layoutHeight;
        [SerializeField] private BattleFieldUnitPose _playerAvatar;
        [SerializeField] private BattleFieldUnitPose _enemyAvatar;
        [SerializeField] private BattleFieldUnitPose[] _playerHeroSlots;
        [SerializeField] private BattleFieldUnitPose[] _enemyHeroSlots;
        [SerializeField] private BattleFieldUnitPose[] _groupShields;
        [SerializeField] private BattleFieldUnitPose _playerPanel;
        [SerializeField] private BattleFieldUnitPose _enemyPanel;


        public float LayoutHeight => _layoutHeight;
        public BattleFieldUnitPose PlayerAvatar => _playerAvatar;
        public BattleFieldUnitPose EnemyAvatar => _enemyAvatar;
        public BattleFieldUnitPose[] PlayerHeroSlots => _playerHeroSlots;
        public BattleFieldUnitPose[] EnemyHeroSlots => _enemyHeroSlots;
        public BattleFieldUnitPose[] GroupShields => _groupShields;
        public BattleFieldUnitPose PlayerPanel => _playerPanel;
        public BattleFieldUnitPose EnemyPanel => _enemyPanel;


        public BattleFieldLayoutSnapshot(
            float layoutHeight,
            BattleFieldUnitPose playerAvatar,
            BattleFieldUnitPose enemyAvatar,
            BattleFieldUnitPose[] playerHeroSlots,
            BattleFieldUnitPose[] enemyHeroSlots,
            BattleFieldUnitPose[] groupShields,
            BattleFieldUnitPose playerPanel,
            BattleFieldUnitPose enemyPanel)
        {
            _layoutHeight = Mathf.Max(0.01f, layoutHeight);
            _playerAvatar = playerAvatar;
            _enemyAvatar = enemyAvatar;
            _playerHeroSlots = playerHeroSlots ?? Array.Empty<BattleFieldUnitPose>();
            _enemyHeroSlots = enemyHeroSlots ?? Array.Empty<BattleFieldUnitPose>();
            _groupShields = groupShields ?? Array.Empty<BattleFieldUnitPose>();
            _playerPanel = playerPanel;
            _enemyPanel = enemyPanel;
        }

        public static BattleFieldLayoutSnapshot CreateDefault()
        {
            return new BattleFieldLayoutSnapshot
            {
                _layoutHeight = 4.2f,
                _playerAvatar = BattleFieldUnitPose.Identity,
                _enemyAvatar = BattleFieldUnitPose.Identity,
                _playerHeroSlots = CreatePoseArray(4),
                _enemyHeroSlots = CreatePoseArray(4),
                _groupShields = CreatePoseArray(4),
                _playerPanel = BattleFieldUnitPose.Identity,
                _enemyPanel = BattleFieldUnitPose.Identity
            };
        }

        private static BattleFieldUnitPose[] CreatePoseArray(int count)
        {
            var result = new BattleFieldUnitPose[count];
            for (var i = 0; i < result.Length; i++)
                result[i] = BattleFieldUnitPose.Identity;

            return result;
        }
    }
}