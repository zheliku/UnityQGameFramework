using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源配置。
    /// </summary>
    [CreateAssetMenu(menuName = "QGF/Resource Settings", fileName = "ResourceSettings")]
    public sealed class ResourceSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_DefaultPackageName = "DefaultPackage";

        [SerializeField, HideInInspector]
        private int m_SerializationVersion;

        [SerializeField]
        private ReadWritePathType m_CachePathType = ReadWritePathType.PersistentData;

        [SerializeField]
        private float m_MinUnloadUnusedAssetsInterval = 60f;

        [SerializeField]
        private float m_MaxUnloadUnusedAssetsInterval = 300f;

#if UNITY_EDITOR
        // 仅编辑器使用；Player 不包含收集配置及其引用。
        [SerializeField]
        [HideInInspector]
        private UnityEngine.Object m_EditorCollector;
#endif

        [SerializeField]
        private List<ResourcePackageSettings> m_Packages = new List<ResourcePackageSettings>
        {
            new ResourcePackageSettings()
        };

        /// <summary>
        /// 获取默认资源包名称。
        /// </summary>
        public string DefaultPackageName
        {
            get
            {
                return m_DefaultPackageName;
            }
        }

        /// <summary>
        /// 获取下载资源缓存的位置。
        /// </summary>
        public ReadWritePathType CachePathType
        {
            get
            {
                return m_CachePathType;
            }
        }

        /// <summary>
        /// 获取预约清理未使用资源的最短等待秒数。
        /// </summary>
        public float MinUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MinUnloadUnusedAssetsInterval;
            }
        }

        /// <summary>
        /// 获取自动清理未使用资源的最长间隔秒数。
        /// </summary>
        public float MaxUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MaxUnloadUnusedAssetsInterval;
            }
        }

        /// <summary>
        /// 获取当前平台的资源缓存根目录。
        /// </summary>
        /// <returns>持久化目录或临时缓存目录。</returns>
        public string GetCacheRoot()
        {
            return m_CachePathType == ReadWritePathType.TemporaryCache ? Application.temporaryCachePath : Application.persistentDataPath;
        }

        /// <summary>
        /// 序列化前升级旧版资源包运行模式配置。
        /// </summary>
        public void OnBeforeSerialize()
        {
            UpgradePlayModes();
        }

        /// <summary>
        /// 反序列化后升级旧版资源包运行模式配置。
        /// </summary>
        public void OnAfterDeserialize()
        {
            UpgradePlayModes();
        }

        /// <summary>
        /// 将旧配置迁移为各资源包的单一运行模式。
        /// </summary>
        private void UpgradePlayModes()
        {
            if (m_SerializationVersion >= 3)
            {
                return;
            }
            foreach (ResourcePackageSettings package in m_Packages)
            {
                package?.UpgradePlayMode(m_SerializationVersion);
            }
            m_SerializationVersion = 3;
        }

        /// <summary>
        /// 保证清理间隔有效且最长间隔不小于最短间隔。
        /// </summary>
        private void OnValidate()
        {
            m_MinUnloadUnusedAssetsInterval = Mathf.Max(0f, m_MinUnloadUnusedAssetsInterval);
            m_MaxUnloadUnusedAssetsInterval = Mathf.Max(m_MinUnloadUnusedAssetsInterval, m_MaxUnloadUnusedAssetsInterval);
        }

        /// <summary>
        /// 获取资源包配置。
        /// </summary>
        public IReadOnlyList<ResourcePackageSettings> Packages
        {
            get
            {
                return m_Packages;
            }
        }

        /// <summary>
        /// 获取指定资源包的配置。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <returns>资源包配置。</returns>
        public ResourcePackageSettings GetPackage(string packageName = null)
        {
            string name = string.IsNullOrEmpty(packageName) ? m_DefaultPackageName : packageName;
            ResourcePackageSettings result = null;
            foreach (ResourcePackageSettings package in m_Packages)
            {
                if (package != null && string.Equals(package.Name, name, StringComparison.Ordinal))
                {
                    if (result != null)
                    {
                        throw new InvalidOperationException("Duplicate resource package: " + name);
                    }
                    result = package;
                }
            }
            if (result == null)
            {
                throw new InvalidOperationException("Resource package is not configured: " + name);
            }
            return result;
        }
    }
}
