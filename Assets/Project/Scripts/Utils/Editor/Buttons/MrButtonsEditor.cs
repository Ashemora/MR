#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Utils.Editor.Buttons
{
    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    public class MrScriptableObjectButtonsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MrButtonsRenderer.DrawBottomButtons(serializedObject);
        }
    }


    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class MrMonoBehaviourButtonsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MrButtonsRenderer.DrawBottomButtons(serializedObject);
        }
    }
}
#endif