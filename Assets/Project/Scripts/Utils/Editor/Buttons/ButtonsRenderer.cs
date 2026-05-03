#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Project.Scripts.Utils.Buttons;
using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Utils.Editor.Buttons
{
    public static class ButtonsRenderer
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;


        public static void DrawButton(Rect rect, UnityEngine.Object[] targets, string methodName)
        {
            if (targets == null || targets.Length == 0 || string.IsNullOrEmpty(methodName))
                return;

            var anchor = targets[0];
            if (!anchor)
                return;

            var method = FindMethod(anchor.GetType(), methodName);
            if (null == method)
            {
                EditorGUI.HelpBox(rect, $"Button: method '{methodName}' not found or has parameters", MessageType.Error);
                return;
            }

            if (GUI.Button(rect, ToDisplayName(method.Name)))
                InvokeOnAll(targets, method);
        }

        public static void DrawBottomButtons(SerializedObject serializedObject)
        {
            if (null == serializedObject)
                return;

            var targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0 || !targets[0])
                return;

            var type = targets[0].GetType();
            var pinned = CollectPinnedMethodNames(type);
            var methods = CollectButtonMethods(type);

            for (var i = 0; i < methods.Count; i++)
            {
                var method = methods[i];
                if (pinned.Contains(method.Name))
                    continue;

                if (GUILayout.Button(ToDisplayName(method.Name)))
                    InvokeOnAll(targets, method);
            }
        }


        private static MethodInfo FindMethod(Type type, string name)
        {
            var current = type;
            while (null != current && current != typeof(object))
            {
                var method = current.GetMethod(name, MemberFlags | BindingFlags.DeclaredOnly);
                if (null != method && method.GetParameters().Length == 0)
                    return method;
                current = current.BaseType;
            }
            
            return null;
        }

        private static List<MethodInfo> CollectButtonMethods(Type type)
        {
            var result = new List<MethodInfo>();
            var seen = new HashSet<string>();
            var current = type;
            while (null != current && current != typeof(object))
            {
                var methods = current.GetMethods(MemberFlags | BindingFlags.DeclaredOnly);
                for (var i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (m.GetParameters().Length != 0)
                        continue;
                    if (null == m.GetCustomAttribute<ButtonAttribute>())
                        continue;
                    if (false == seen.Add(m.Name))
                        continue;
                    result.Add(m);
                }
                current = current.BaseType;
            }
            
            return result;
        }

        private static HashSet<string> CollectPinnedMethodNames(Type type)
        {
            var result = new HashSet<string>();
            var current = type;
            while (null != current && current != typeof(object))
            {
                var fields = current.GetFields(MemberFlags | BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++)
                {
                    var attr = fields[i].GetCustomAttribute<ButtonAttribute>();
                    if (null != attr && false == string.IsNullOrEmpty(attr.MethodName))
                        result.Add(attr.MethodName);
                }
                current = current.BaseType;
            }
            
            return result;
        }

        private static void InvokeOnAll(UnityEngine.Object[] targets, MethodInfo method)
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (!target)
                    continue;
                
                Undo.RecordObject(target, $"Invoke {method.Name}");
                method.Invoke(target, null);
                EditorUtility.SetDirty(target);
            }
        }

        private static string ToDisplayName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return raw;

            var sb = new StringBuilder(raw.Length + 4);
            sb.Append(raw[0]);
            for (var i = 1; i < raw.Length; i++)
            {
                var c = raw[i];
                if (char.IsUpper(c) && false == char.IsUpper(raw[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            
            return sb.ToString();
        }
    }
}
#endif