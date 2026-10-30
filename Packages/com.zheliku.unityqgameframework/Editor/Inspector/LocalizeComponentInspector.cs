using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 显示本地化绑定的必要配置与自动刷新说明。
    /// </summary>
    [CustomEditor(typeof(LocalizeComponent), true)]
    [CanEditMultipleObjects]
    internal sealed class LocalizeComponentInspector : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制绑定配置并检查命名初值。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }
            serializedObject.Update();
            SerializedProperty key = serializedObject.FindProperty("m_Key");
            if (string.IsNullOrWhiteSpace(key.stringValue))
            {
                EditorGUILayout.HelpBox("Set a translation key, or an asset key for Image.", MessageType.Warning);
            }
            SerializedProperty values = serializedObject.FindProperty("m_InitialFormatValues");
            if (values != null)
            {
                var names = new HashSet<string>();
                for (int index = 0; index < values.arraySize; index++)
                {
                    string name = values.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name").stringValue;
                    if (string.IsNullOrEmpty(name) || !names.Add(name))
                    {
                        EditorGUILayout.HelpBox("Format values need unique names matching the translation, such as Player or Count.", MessageType.Error);
                        break;
                    }
                }
                EditorGUILayout.HelpBox("Initial values can be overridden with SetFormatValues or SetFormatValue. Configure wrapping, alignment, rich text and fallback fonts on the TMP component.", MessageType.Info);
            }
            EditorGUILayout.HelpBox("Updates automatically when enabled and after a successful language change. Disabled objects unsubscribe and restore their original resource references.", MessageType.None);
        }
    }
}
