using System;
using UnityEngine;

namespace Project.Scripts.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SpritePreviewAttribute : PropertyAttribute
    {
        public float Size { get; }


        public SpritePreviewAttribute(float size = 64f)
        {
            Size = size;
        }
    }
}