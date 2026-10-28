//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制文件系统组件的 Inspector。
    /// </summary>
    [CustomEditor(typeof(FileSystemComponent))]
    internal sealed class FileSystemComponentInspector : GameFrameworkInspector
    {
        private HelperInfo<FileSystemHelperBase> m_FileSystemHelperInfo = new HelperInfo<FileSystemHelperBase>("FileSystem");

        /// <summary>
        /// 绘制组件的 Inspector 配置和运行时信息。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            FileSystemComponent t = (FileSystemComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_FileSystemHelperInfo.Draw();
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("File System Count", t.Count.ToString());

                IFileSystem[] fileSystems = t.GetAllFileSystems();
                foreach (IFileSystem fileSystem in fileSystems)
                {
                    DrawFileSystem(fileSystem);
                }
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
            m_FileSystemHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        /// <summary>
        /// 刷新可用辅助器或流程类型名称。
        /// </summary>
        private void RefreshTypeNames()
        {
            m_FileSystemHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制文件系统容量和文件数量。
        /// </summary>
        /// <param name="fileSystem">要显示的文件系统。</param>
        private void DrawFileSystem(IFileSystem fileSystem)
        {
            EditorGUILayout.LabelField(fileSystem.FullPath, Utility.Text.Format("{0}, {1} / {2} Files", fileSystem.Access, fileSystem.FileCount, fileSystem.MaxFileCount));
        }
    }
}
