#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Units.Editor
{
    public static class UnitDeckPreviewDrawer
    {
        public static void Draw(Object deck, string header)
        {
            if (null == deck)
                return;

            var deckObject = new SerializedObject(deck);
            var avatarProperty = deckObject.FindProperty("_avatarConfig");
            var heroesProperty = deckObject.FindProperty("_heroes");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                if (null != avatarProperty)
                    EditorGUILayout.PropertyField(avatarProperty, new GUIContent("Avatar"), true);
                if (null != heroesProperty)
                    EditorGUILayout.PropertyField(heroesProperty, new GUIContent("Heroes"), true);
            }
        }
    }
}
#endif