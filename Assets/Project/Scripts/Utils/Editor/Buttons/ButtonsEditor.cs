#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Utils.Editor.Buttons
{
    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    public class ScriptableObjectButtonsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ButtonsRenderer.DrawBottomButtons(serializedObject);
        }
    }


    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class MonoBehaviourButtonsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ButtonsRenderer.DrawBottomButtons(serializedObject);
        }
    }
}
#endif