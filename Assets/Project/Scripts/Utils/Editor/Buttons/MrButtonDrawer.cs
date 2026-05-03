#if UNITY_EDITOR
using Project.Scripts.Utils.Buttons;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Utils.Editor.Buttons
{
    [CustomPropertyDrawer(typeof(MrButtonAttribute))]
    public class MrButtonDrawer : PropertyDrawer
    {
        private const float ButtonHeight = 22f;
        private const float Spacing = 2f;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
            return fieldHeight + ButtonHeight + Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (MrButtonAttribute)attribute;
            var fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);

            Rect fieldRect;
            Rect buttonRect;

            if (attr.Position == MrButtonPosition.Above)
            {
                buttonRect = new Rect(position.x, position.y, position.width, ButtonHeight);
                fieldRect = new Rect(position.x, buttonRect.yMax + Spacing, position.width, fieldHeight);
            }
            else
            {
                fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);
                buttonRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, ButtonHeight);
            }

            EditorGUI.PropertyField(fieldRect, property, label, true);
            MrButtonsRenderer.DrawButton(buttonRect, property.serializedObject.targetObjects, attr.MethodName);
        }
    }
}
#endif