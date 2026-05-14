#if UNITY_EDITOR
using Project.Scripts.Configs.Battle.Units.Editor;
using UnityEditor;

namespace Project.Scripts.Configs.Battle.Editor
{
    [CustomEditor(typeof(PlayerBattleConfig))]
    public class PlayerBattleConfigEditor : UnityEditor.Editor
    {
        private const string DefaultDeckPath = "_defaultUnitDeck";
        private const string ScriptPath = "m_Script";


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == ScriptPath)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator);
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);

                if (iterator.propertyPath == DefaultDeckPath)
                    UnitDeckPreviewDrawer.Draw(iterator.objectReferenceValue, "Default Deck Preview");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif