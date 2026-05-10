using System;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Units
{
    [Serializable]
    public struct AvatarDeathVisuals
    {
        [Tooltip("Применять шейдерную заливку портрета при гибели аватара.")]
        public bool ApplyDeathFill;

        [Tooltip("Цвет, в который красятся _deathColoredRenderers при гибели аватара.")]
        public Color DeathColor;
    }
}