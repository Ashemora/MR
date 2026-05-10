using System;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Layout
{
    [Serializable]
    public struct BattleFieldUnitPose
    {
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private Vector3 _localScale;


        public Vector3 LocalPosition => _localPosition;
        public Vector3 LocalScale => _localScale;
        public static BattleFieldUnitPose Identity => new(Vector3.zero, Vector3.one);


        public BattleFieldUnitPose(Vector3 localPosition, Vector3 localScale)
        {
            _localPosition = localPosition;
            _localScale = localScale;
        }

        public static BattleFieldUnitPose Lerp(BattleFieldUnitPose from, BattleFieldUnitPose to, float t)
        {
            return new BattleFieldUnitPose(
                Vector3.LerpUnclamped(from.LocalPosition, to.LocalPosition, t),
                Vector3.LerpUnclamped(from.LocalScale, to.LocalScale, t));
        }
    }
}