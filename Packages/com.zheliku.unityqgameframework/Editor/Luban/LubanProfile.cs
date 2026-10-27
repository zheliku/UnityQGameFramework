using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 数据表生成配置。
    /// </summary>
    [CreateAssetMenu(menuName = "QGF/Luban Profile", fileName = "LubanProfile")]
    public sealed class LubanProfile : ScriptableObject
    {
        [FormerlySerializedAs("SourceFolder")]
        [SerializeField, HideInInspector]
        private DefaultAsset m_LegacySourceFolder;

        /// <summary>
        /// Luban 配置文件。
        /// </summary>
        public DefaultAsset ConfigurationFile;

        /// <summary>
        /// 生成代码目录。
        /// </summary>
        public DefaultAsset CodeFolder;

        /// <summary>
        /// 生成数据目录。
        /// </summary>
        public DefaultAsset DataFolder;

        /// <summary>
        /// Luban 导出目标名称。
        /// </summary>
        public string Target = "client";

        /// <summary>
        /// 已生成的代码文件。
        /// </summary>
        [HideInInspector] public List<string> GeneratedCodeFiles = new List<string>();

        /// <summary>
        /// 已生成的数据文件。
        /// </summary>
        [HideInInspector] public List<string> GeneratedDataFiles = new List<string>();

        /// <summary>
        /// 已生成代码所属的目录标识。
        /// </summary>
        [HideInInspector] public string GeneratedCodeFolderGuid;

        /// <summary>
        /// 已生成数据所属的目录标识。
        /// </summary>
        [HideInInspector] public string GeneratedDataFolderGuid;

        /// <summary>
        /// 将旧版 Luban 配置升级为当前结构。
        /// </summary>
        internal void UpgradeConfiguration()
        {
            if (ConfigurationFile != null || m_LegacySourceFolder == null)
            {
                return;
            }
            string source = AssetDatabase.GetAssetPath(m_LegacySourceFolder);
            ConfigurationFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>(source + "/luban.conf");
            if (ConfigurationFile == null)
            {
                return;
            }
            m_LegacySourceFolder = null;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
