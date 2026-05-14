#if UNITY_EDITOR
using Project.Scripts.Configs.Battle.Units.Editor;
using UnityEditor;

namespace Project.Scripts.Configs.Levels.Editor
{
    [CustomEditor(typeof(LevelConfig))]
    public class LevelConfigEditor : UnityEditor.Editor
    {
        private const string OpponentDeckPath = "_opponentUnitDeck";
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

                if (iterator.propertyPath == OpponentDeckPath)
                    UnitDeckPreviewDrawer.Draw(iterator.objectReferenceValue, "Opponent Deck Preview");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif