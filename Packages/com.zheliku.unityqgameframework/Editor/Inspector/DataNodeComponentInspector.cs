//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.DataNode;
using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    [CustomEditor(typeof(DataNodeComponent))]
    internal sealed class DataNodeComponentInspector : FrameworkInspector
    {
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

        private void OnEnable()
        {
        }

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
