//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制资源配置引用和运行状态。
    /// </summary>
    [CustomEditor(typeof(ResourceComponent))]
    internal sealed class ResourceComponentInspector : GameFrameworkInspector
    {
        private bool m_ShowSettings = true;
        private UnityEditor.Editor m_SettingsEditor;

        /// <summary>
        /// 绘制资源组件面板。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            SerializedProperty reference = serializedObject.FindProperty("m_Settings");
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(reference, new GUIContent("Resource Settings", "The component references one ResourceSettings asset. All runtime options are saved in that asset."));
            }
            serializedObject.ApplyModifiedProperties();
            var settings = reference.objectReferenceValue as ResourceSettings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Assign ResourceSettings to use the resource module. Default location: Assets/UnityQGameFramework/Resource.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(target)))
                {
                    if (GUILayout.Button("Use or Create Default Settings"))
                    {
                        reference.objectReferenceValue = ResourceSettingsInspector.GetOrCreateDefaultSettings();
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                return;
            }
            EditorGUILayout.LabelField("Settings Path", AssetDatabase.GetAssetPath(settings), EditorStyles.wordWrappedLabel);
            m_ShowSettings = EditorGUILayout.Foldout(m_ShowSettings, "Edit Resource Settings", true);
            if (m_ShowSettings)
            {
                // 直接绘制引用资产的同一套面板，不在组件上另存设置。
                CreateCachedEditor(settings, typeof(ResourceSettingsInspector), ref m_SettingsEditor);
                m_SettingsEditor.OnInspectorGUI();
            }
            if (EditorApplication.isPlaying)
            {
                var component = (ResourceComponent)target;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Builtin Root", component.ReadOnlyPath, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Download Cache Root", component.ReadWritePath, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Manifest Assets", component.AssetCount.ToString());
                EditorGUILayout.LabelField("Loading Requests", component.LoadingCount.ToString());
            }
        }

        /// <summary>
        /// 释放嵌入的配置编辑器。
        /// </summary>
        private void OnDisable()
        {
            if (m_SettingsEditor != null)
            {
                DestroyImmediate(m_SettingsEditor);
            }
        }
    }
}
