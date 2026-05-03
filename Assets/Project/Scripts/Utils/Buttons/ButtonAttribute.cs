using System;
using UnityEngine;

namespace Project.Scripts.Utils.Buttons
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public ButtonPosition Position { get; }


        public ButtonAttribute()
        {
            MethodName = null;
            Position = ButtonPosition.Below;
        }

        public ButtonAttribute(string methodName, ButtonPosition position = ButtonPosition.Below)
        {
            MethodName = methodName;
            Position = position;
        }
    }
}