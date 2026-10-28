//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制事件组件的 Inspector。
    /// </summary>
    [CustomEditor(typeof(EventComponent))]
    internal sealed class EventComponentInspector : GameFrameworkInspector
    {
        /// <summary>
        /// 绘制组件的 Inspector 配置和运行时信息。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available during runtime only.", MessageType.Info);
                return;
            }

            EventComponent t = (EventComponent)target;

            if (IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Event Handler Count", t.EventHandlerCount.ToString());
                EditorGUILayout.LabelField("Event Count", t.EventCount.ToString());
            }

            Repaint();
        }

        /// <summary>
        /// 初始化序列化属性和编辑器缓存。
        /// </summary>
        private void OnEnable()
        {
        }
    }
}
