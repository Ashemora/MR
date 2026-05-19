#if DEV
using UnityEngine;

namespace Project.Scripts.Dev
{
    internal static class DevGameplayButtonCleanup
    {
        public static void DestroyNamedButtons(Transform parent, string buttonName)
        {
            if (!parent || string.IsNullOrEmpty(buttonName))
                return;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name == buttonName)
                    Object.Destroy(child.gameObject);
            }
        }
    }
}
#endif