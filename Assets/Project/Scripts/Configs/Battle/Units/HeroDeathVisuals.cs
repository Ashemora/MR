using System;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Units
{
    [Serializable]
    public struct HeroDeathVisuals
    {
        [Tooltip("Применять шейдерную заливку портрета при гибели героя.")]
        public bool ApplyDeathFill;

        [Tooltip("Цвет, в который красятся _deathColoredRenderers при гибели героя.")]
        public Color DeathColor;
    }
}