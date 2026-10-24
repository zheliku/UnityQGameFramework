using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 保存生成的 bytes 资产引用与结构版本。
    /// </summary>
    public sealed class LubanDataTableCatalog : ScriptableObject
    {
        [Serializable]
        /// <summary>
        /// 数据文件条目。
        /// </summary>
        public sealed class Entry
        {
            /// <summary>
            /// 数据文件名称。
            /// </summary>
            public string Name;
            /// <summary>
            /// 数据文件资源。
            /// </summary>
            public TextAsset Data;
        }

        [SerializeField] private string m_SchemaHash;
        [SerializeField] private string m_GeneratorVersion;
        [SerializeField] private Entry[] m_Entries = Array.Empty<Entry>();

        /// <summary>
        /// 获取生成代码的结构版本。
        /// </summary>
        public string SchemaHash
        {
            get
            {
                return m_SchemaHash;
            }
        }
        /// <summary>
        /// 获取数据生成器版本。
        /// </summary>
        public string GeneratorVersion
        {
            get
            {
                return m_GeneratorVersion;
            }
        }
        /// <summary>
        /// 获取数据文件条目。
        /// </summary>
        public IReadOnlyList<Entry> Entries
        {
            get
            {
                return m_Entries;
            }
        }

        /// <summary>
        /// 获取数据文件资源。
        /// </summary>
        /// <returns>按文件名称索引的数据资源。</returns>
        public Dictionary<string, TextAsset> GetAssets()
        {
            if (string.IsNullOrWhiteSpace(m_SchemaHash))
            {
                throw new InvalidOperationException("The Luban catalog has no generated schema version. Regenerate the configuration.");
            }
            if (m_Entries == null || m_Entries.Length == 0)
            {
                throw new InvalidOperationException("The Luban catalog is empty.");
            }
            var assets = new Dictionary<string, TextAsset>(StringComparer.Ordinal);
            foreach (Entry entry in m_Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name) || entry.Data == null)
                {
                    throw new InvalidOperationException("The Luban catalog contains a missing data asset.");
                }
                if (assets.ContainsKey(entry.Name))
                {
                    throw new InvalidOperationException("Duplicate Luban data file: " + entry.Name);
                }
                assets.Add(entry.Name, entry.Data);
            }
            return assets;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 设置生成结果。
        /// </summary>
        /// <param name="schemaHash">生成代码的结构版本。</param>
        /// <param name="generatorVersion">数据生成器版本。</param>
        /// <param name="entries">数据文件条目。</param>
        public void SetGeneratedData(string schemaHash, string generatorVersion, Entry[] entries)
        {
            m_SchemaHash = schemaHash;
            m_GeneratorVersion = generatorVersion;
            m_Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }
#endif
    }
}
