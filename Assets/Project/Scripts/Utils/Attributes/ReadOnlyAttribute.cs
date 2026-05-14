using System;
using UnityEngine;

namespace Project.Scripts.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}