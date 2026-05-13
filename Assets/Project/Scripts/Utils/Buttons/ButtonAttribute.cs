using System;
using UnityEngine;

namespace Project.Scripts.Utils.Buttons
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public ButtonPosition Position { get; }
        public bool DrawField { get; }


        public ButtonAttribute()
        {
            MethodName = null;
            Position = ButtonPosition.Below;
            DrawField = true;
        }

        public ButtonAttribute(string methodName, ButtonPosition position = ButtonPosition.Below, bool drawField = true)
        {
            MethodName = methodName;
            Position = position;
            DrawField = drawField;
        }
    }
}