using System;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 保存全局 Luban Profile 和新配置的默认目录。
    /// </summary>
    [FilePath("ProjectSettings/UnityQGameFrameworkSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class LubanSettings : ScriptableSingleton<LubanSettings>
    {
        /// <summary>
        /// QGF 项目设置页的路径。
        /// </summary>
        public const string SettingsPath = "Project/QGF/Luban";

        /// <summary>
        /// 默认配置根目录。
        /// </summary>
        public const string DefaultWorkspaceRoot = "Assets/UnityQGameFramework/Config";
        [SerializeField]
        private string m_ProfileGuid;
        [SerializeField]
        private string m_WorkspaceRoot = DefaultWorkspaceRoot;

        /// <summary>
        /// 获取是否已经指定全局 Luban 生成配置。
        /// </summary>
        internal bool HasProfile
        {
            get
            {
                return !string.IsNullOrEmpty(m_ProfileGuid);
            }
        }

        /// <summary>
        /// 获取或设置全局生成配置。
        /// </summary>
        public LubanProfile Profile
        {
            get
            {
                return HasProfile ? AssetDatabase.LoadAssetAtPath<LubanProfile>(AssetDatabase.GUIDToAssetPath(m_ProfileGuid)) : null;
            }

            set
            {
                string path = AssetDatabase.GetAssetPath(value);
                if (value != null && !path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Select a LubanProfile saved inside Assets.");
                }

                m_ProfileGuid = value == null ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                Save(true);
            }
        }

        /// <summary>
        /// 获取或设置新配置的默认根目录。
        /// </summary>
        public string WorkspaceRoot
        {
            get
            {
                return m_WorkspaceRoot;
            }

            set
            {
                m_WorkspaceRoot = LubanWorkspace.ValidateRoot(value);
                Save(true);
            }
        }
    }
}
