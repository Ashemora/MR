#if UNITY_EDITOR
using System;
using System.Reflection;
using Project.Scripts.Configs.Board;
using Project.Scripts.Constants;
using Project.Scripts.Services.BoardEdit;
using Project.Scripts.Shared.Tiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.Scripts.Utils.Editor
{
    public class GDToolsWindow : EditorWindow
    {
        private TileSetConfig _tileSetConfig;


        [MenuItem("Tools/GD Tools")]
        public static void Open()
        {
            GetWindow<GDToolsWindow>("GD Tools");
        }


        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button(new GUIContent("Toggle Obj", "Toggle active state of selected objects")))
                ToggleObject();
            if (GUILayout.Button(new GUIContent("Toggle Gzms", "Toggle Gizmos visibility in the Scene view")))
                ToggleGizmos();
            if (GUILayout.Button(new GUIContent("Toggle Snd", "Toggle audio master mute")))
                ToggleAudio();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Board Edit", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var activeScene = EditorSceneManager.GetActiveScene();
            var isGameplayActive = Application.isPlaying && activeScene.name == SceneNames.GamePlay;
            EditorGUI.BeginDisabledGroup(false == isGameplayActive);

            EditorGUILayout.BeginHorizontal();

            var size = EditorGUIUtility.singleLineHeight;
            var sprite = GetSpriteForKind(BoardEditMode.SelectedKind);
            var iconRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (sprite)
            {
                var uv = new Rect(
                    sprite.rect.x / sprite.texture.width,
                    sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width,
                    sprite.rect.height / sprite.texture.height);
                var prevColor = GUI.color;
                if (false == GUI.enabled)
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUI.DrawTextureWithTexCoords(iconRect, sprite.texture, uv);
                GUI.color = prevColor;
            }

            BoardEditMode.SelectedKind = (TileKind)EditorGUILayout.EnumPopup(BoardEditMode.SelectedKind);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            var buttonLabel = BoardEditMode.IsActive ? "Stop Edit" : "Start Edit";
            var buttonColor = BoardEditMode.IsActive ? new Color(1f, 0.45f, 0.45f) : new Color(0.45f, 1f, 0.6f);

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonLabel, GUILayout.Height(30)))
                BoardEditMode.SetActive(false == BoardEditMode.IsActive);
            GUI.backgroundColor = prev;

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Copy Console"))
                CopyConsole();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private Sprite GetSpriteForKind(TileKind kind)
        {
            if (!_tileSetConfig)
            {
                var guids = AssetDatabase.FindAssets("t:TileSetConfig");
                if (guids.Length == 0)
                    return null;

                _tileSetConfig = AssetDatabase.LoadAssetAtPath<TileSetConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (!_tileSetConfig)
                return null;

            var regularTiles = _tileSetConfig.RegularTiles;
            if (regularTiles != null)
                for (var i = 0; i < regularTiles.Length; i++)
                    if (regularTiles[i].Kind == kind)
                        return regularTiles[i].Sprite;

            var specialTiles = _tileSetConfig.SpecialTiles;
            if (specialTiles != null)
                for (var i = 0; i < specialTiles.Length; i++)
                    if (specialTiles[i].Kind == kind)
                        return specialTiles[i].Sprite;

            return null;
        }

        private void ToggleObject()
        {
            var selectedObjects = Selection.gameObjects;
            for (var i = 0; i < selectedObjects.Length; i++)
            {
                var obj = selectedObjects[i];
                if (obj)
                {
                    Undo.RecordObject(obj, "Toggle Active State");
                    obj.SetActive(!obj.activeSelf);
                    if (PrefabUtility.IsPartOfPrefabInstance(obj) || PrefabUtility.IsPartOfPrefabAsset(obj))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            }
        }
        
        private void ToggleGizmos()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (!sceneView)
            {
                Debug.LogWarning("No active Scene view found");
                return;
            }

            sceneView.drawGizmos = !sceneView.drawGizmos;
            sceneView.Repaint();
        }

        private void ToggleAudio()
        {
            EditorUtility.audioMasterMute = !EditorUtility.audioMasterMute;
        }

        private void CopyConsole()
        {
            try
            {
                var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                if (null == logEntries)
                {
                    Debug.LogError("Failed to access console types");
                    return;
                }

                var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var startGettingEntriesMethod = logEntries.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var getEntryInternalMethod = logEntries.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var endGettingEntriesMethod = logEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                var count = (int)getCountMethod.Invoke(null, null);
                var sb = new System.Text.StringBuilder();
                startGettingEntriesMethod.Invoke(null, null);
                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
                var logEntry = Activator.CreateInstance(logEntryType);

                for (var i = 0; i < count; i++)
                {
                    getEntryInternalMethod.Invoke(null, new[] { i, logEntry });
                    var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                    var message = (string)messageField.GetValue(logEntry);
                    var header = message.Split('\n')[0];
                    sb.AppendLine(header);
                }

                endGettingEntriesMethod.Invoke(null, null);
                GUIUtility.systemCopyBuffer = sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while copying console:{ex.Message}");
            }
        }
    }
}
#endif