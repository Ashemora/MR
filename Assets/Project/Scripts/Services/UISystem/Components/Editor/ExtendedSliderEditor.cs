#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

namespace Project.Scripts.Services.UISystem.Components.Editor
{
    [CustomEditor(typeof(ExtendedSlider))]
    [CanEditMultipleObjects]
    public class ExtendedSliderEditor : SliderEditor
    {
        private SerializedProperty _additionalTargetGraphics;


        protected override void OnEnable()
        {
            base.OnEnable();
            _additionalTargetGraphics = serializedObject.FindProperty("_additionalTargetGraphics");
        }
        

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_additionalTargetGraphics);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif