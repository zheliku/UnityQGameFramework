using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源配置。
    /// </summary>
    [CreateAssetMenu(menuName = "QGF/Resource Settings", fileName = "ResourceSettings")]
    public sealed class ResourceSettings : ScriptableObject
    {
        [SerializeField]
        private string m_DefaultPackageName = "DefaultPackage";

        [SerializeField]
        private bool m_UseEditorSimulation = true;

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
        /// 获取是否在编辑器中使用模拟模式。
        /// </summary>
        public bool UseEditorSimulation
        {
            get
            {
                return m_UseEditorSimulation;
            }
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
