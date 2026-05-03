using System;
using UnityEngine;

namespace Project.Scripts.Utils.Buttons
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class MrButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public MrButtonPosition Position { get; }


        public MrButtonAttribute()
        {
            MethodName = null;
            Position = MrButtonPosition.Below;
        }

        public MrButtonAttribute(string methodName, MrButtonPosition position = MrButtonPosition.Below)
        {
            MethodName = methodName;
            Position = position;
        }
    }
}