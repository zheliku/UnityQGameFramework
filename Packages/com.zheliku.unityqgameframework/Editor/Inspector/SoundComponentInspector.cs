//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制声音组件的 Inspector。
    /// </summary>
    [CustomEditor(typeof(SoundComponent))]
    internal sealed class SoundComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty m_EnablePlaySoundUpdateEvent = null;
        private SerializedProperty m_EnablePlaySoundDependencyAssetEvent = null;
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_AudioMixer = null;
        private SerializedProperty m_SoundGroups = null;

        private HelperInfo<SoundHelperBase> m_SoundHelperInfo = new HelperInfo<SoundHelperBase>("Sound");
        private HelperInfo<SoundGroupHelperBase> m_SoundGroupHelperInfo = new HelperInfo<SoundGroupHelperBase>("SoundGroup");
        private HelperInfo<SoundAgentHelperBase> m_SoundAgentHelperInfo = new HelperInfo<SoundAgentHelperBase>("SoundAgent");

        /// <summary>
        /// 绘制组件的 Inspector 配置和运行时信息。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            SoundComponent t = (SoundComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnablePlaySoundUpdateEvent);
                EditorGUILayout.PropertyField(m_EnablePlaySoundDependencyAssetEvent);
                EditorGUILayout.PropertyField(m_InstanceRoot);
                EditorGUILayout.PropertyField(m_AudioMixer);
                m_SoundHelperInfo.Draw();
                m_SoundGroupHelperInfo.Draw();
                m_SoundAgentHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_SoundGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Sound Group Count", t.SoundGroupCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();

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
            m_EnablePlaySoundUpdateEvent = serializedObject.FindProperty("m_EnablePlaySoundUpdateEvent");
            m_EnablePlaySoundDependencyAssetEvent = serializedObject.FindProperty("m_EnablePlaySoundDependencyAssetEvent");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_AudioMixer = serializedObject.FindProperty("m_AudioMixer");
            m_SoundGroups = serializedObject.FindProperty("m_SoundGroups");

            m_SoundHelperInfo.Init(serializedObject);
            m_SoundGroupHelperInfo.Init(serializedObject);
            m_SoundAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        /// <summary>
        /// 刷新可用辅助器或流程类型名称。
        /// </summary>
        private void RefreshTypeNames()
        {
            m_SoundHelperInfo.Refresh();
            m_SoundGroupHelperInfo.Refresh();
            m_SoundAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
