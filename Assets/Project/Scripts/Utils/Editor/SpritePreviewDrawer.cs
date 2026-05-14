#if UNITY_EDITOR
using Project.Scripts.Utils.Attributes;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public class SpritePreviewDrawer : PropertyDrawer
    {
        private const float PreviewPadding = 4f;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return lineHeight;

            var attr = (SpritePreviewAttribute)attribute;
            return lineHeight + EditorGUIUtility.standardVerticalSpacing + attr.Size;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var fieldRect = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return;

            var attr = (SpritePreviewAttribute)attribute;
            var sprite = property.objectReferenceValue as Sprite;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var previewX = position.x + EditorGUIUtility.labelWidth;
            var previewRect = new Rect(previewX, position.y + lineHeight + spacing, attr.Size, attr.Size);

            GUI.Box(previewRect, GUIContent.none);
            if (!sprite)
                return;

            var texture = sprite.texture;
            if (!texture)
                return;

            var uv = new Rect(
                sprite.rect.x / texture.width,
                sprite.rect.y / texture.height,
                sprite.rect.width / texture.width,
                sprite.rect.height / texture.height);
            var innerRect = new Rect(
                previewRect.x + PreviewPadding,
                previewRect.y + PreviewPadding,
                previewRect.width - PreviewPadding * 2,
                previewRect.height - PreviewPadding * 2);
            GUI.DrawTextureWithTexCoords(innerRect, texture, uv);
        }
    }
}
#endif