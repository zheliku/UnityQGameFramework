using System;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    [FilePath("ProjectSettings/UnityQGameFrameworkSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class LubanSettings : ScriptableSingleton<LubanSettings>
    {
        public const string SettingsPath = "Project/Unity Q Game Framework/Luban";
        public const string DefaultWorkspaceRoot = "Assets/UnityQGameFramework/Config";

        [SerializeField] private string m_ProfileGuid;
        [SerializeField] private string m_WorkspaceRoot = DefaultWorkspaceRoot;

        internal bool HasProfile => !string.IsNullOrEmpty(m_ProfileGuid);

        public LubanProfile Profile
        {
            get => HasProfile ? AssetDatabase.LoadAssetAtPath<LubanProfile>(AssetDatabase.GUIDToAssetPath(m_ProfileGuid)) : null;
            set
            {
                string path = AssetDatabase.GetAssetPath(value);
                if (value != null && !path.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new ArgumentException("请选择保存在 Assets 中的 LubanProfile。");
                m_ProfileGuid = value == null ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                Save(true);
            }
        }

        public string WorkspaceRoot
        {
            get => m_WorkspaceRoot;
            set { m_WorkspaceRoot = LubanWorkspace.ValidateRoot(value); Save(true); }
        }
    }
}
