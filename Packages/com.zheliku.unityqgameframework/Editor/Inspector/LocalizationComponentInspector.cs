//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制本地化组件的 Inspector。
    /// </summary>
    [CustomEditor(typeof(LocalizationComponent))]
    internal sealed class LocalizationComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty m_EnableLoadDictionaryUpdateEvent = null;
        private SerializedProperty m_EnableLoadDictionaryDependencyAssetEvent = null;
        private SerializedProperty m_CachedBytesSize = null;
        private SerializedProperty m_Languages;
        private SerializedProperty m_FallbackLanguageCode;
        private int m_PreviewLanguage;
        private string m_ValidationMessage;
        private bool m_ValidationFailed;

        private HelperInfo<LocalizationHelperBase> m_LocalizationHelperInfo = new HelperInfo<LocalizationHelperBase>("Localization");

        /// <summary>
        /// 绘制组件的 Inspector 配置和运行时信息。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            LocalizationComponent t = (LocalizationComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnableLoadDictionaryUpdateEvent);
                EditorGUILayout.PropertyField(m_EnableLoadDictionaryDependencyAssetEvent);
                m_LocalizationHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_CachedBytesSize);
                EditorGUILayout.PropertyField(m_Languages, true);
                LocalizationLanguageDrawer.DrawConfiguredCode(EditorGUILayout.GetControlRect(), m_FallbackLanguageCode,
                    new GUIContent("Final Fallback Language"), "None");
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Language", t.Language.Code);
                EditorGUILayout.LabelField("Changing Language", t.IsChangingLanguage.ToString());
                EditorGUILayout.LabelField("System Language", t.SystemLanguage.ToString());
                EditorGUILayout.LabelField("Dictionary Count", t.DictionaryCount.ToString());
                EditorGUILayout.LabelField("Cached Bytes Size", t.CachedBytesSize.ToString());
                var languages = t.Languages.Where(configuration => configuration != null).Select(configuration => configuration.Language).ToArray();
                if (languages.Length > 0)
                {
                    m_PreviewLanguage = Mathf.Clamp(m_PreviewLanguage, 0, languages.Length - 1);
                    m_PreviewLanguage = EditorGUILayout.Popup("Preview Language", m_PreviewLanguage, languages.Select(language => LocalizationLanguageDrawer.GetLabel(language.Code)).ToArray());
                    using (new EditorGUI.DisabledScope(t.IsChangingLanguage))
                    {
                        if (GUILayout.Button("Switch Language"))
                        {
                            t.SetLanguageAsync(languages[m_PreviewLanguage]).Forget(exception => Log.Warning("Language change failed: {0}", exception.Message));
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || t.Languages.Count == 0))
            {
                if (GUILayout.Button("Validate Dictionaries and Assets"))
                {
                    try
                    {
                        LocalizationBuildValidator.Validate(t);
                        m_ValidationMessage = "Localization dictionaries and collected assets are valid.";
                        m_ValidationFailed = false;
                    }
                    catch (Exception exception)
                    {
                        m_ValidationMessage = exception.Message;
                        m_ValidationFailed = true;
                    }
                }
            }
            if (!string.IsNullOrEmpty(m_ValidationMessage))
            {
                EditorGUILayout.HelpBox(m_ValidationMessage, m_ValidationFailed ? MessageType.Error : MessageType.Info);
            }

            try
            {
                t.ValidateLanguages();
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }

            Repaint();
        }

        /// <summary>
        /// 脚本重新编译后刷新可用类型。
        /// </summary>
        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        /// <summary>
        /// 初始化序列化属性和编辑器缓存。
        /// </summary>
        private void OnEnable()
        {
            m_EnableLoadDictionaryUpdateEvent = serializedObject.FindProperty("m_EnableLoadDictionaryUpdateEvent");
            m_EnableLoadDictionaryDependencyAssetEvent = serializedObject.FindProperty("m_EnableLoadDictionaryDependencyAssetEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");
            m_Languages = serializedObject.FindProperty("m_Languages");
            m_FallbackLanguageCode = serializedObject.FindProperty("m_FallbackLanguageCode");

            m_LocalizationHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        /// <summary>
        /// 刷新可用辅助器或流程类型名称。
        /// </summary>
        private void RefreshTypeNames()
        {
            m_LocalizationHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
