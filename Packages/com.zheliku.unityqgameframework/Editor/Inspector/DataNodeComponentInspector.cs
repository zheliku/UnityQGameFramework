//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.DataNode;
using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制数据结点组件的 Inspector。
    /// </summary>
    [CustomEditor(typeof(DataNodeComponent))]
    internal sealed class DataNodeComponentInspector : FrameworkInspector
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

            DataNodeComponent t = (DataNodeComponent)target;

            if (IsPrefabInHierarchy(t.gameObject))
            {
                DrawDataNode(t.Root);
            }

            Repaint();
        }

        /// <summary>
        /// 初始化序列化属性和编辑器缓存。
        /// </summary>
        private void OnEnable()
        {
        }

        /// <summary>
        /// 绘制数据结点的路径和值。
        /// </summary>
        /// <param name="dataNode">要显示的数据结点。</param>
        private void DrawDataNode(IDataNode dataNode)
        {
            EditorGUILayout.LabelField(dataNode.FullName, dataNode.ToDataString());
            IDataNode[] child = dataNode.GetAllChild();
            foreach (IDataNode c in child)
            {
                DrawDataNode(c);
            }
        }
    }
}
