using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 资源包构建配置。
    /// </summary>
    [CreateAssetMenu(menuName = "QGF/Resource Build Profile", fileName = "ResourceBuildProfile")]
    public sealed class ResourceBuildProfile : ScriptableObject
    {
        [SerializeField]
        private ResourceSettings m_Settings;

        [SerializeField]
        private string m_Version = "1.0.0";

        [SerializeField]
        private EBundleType m_BundleType = EBundleType.AssetBundle;

        [SerializeField]
        private BundleCollectorPackage m_Package = new BundleCollectorPackage();

        /// <summary>
        /// 获取运行时资源配置。
        /// </summary>
        public ResourceSettings Settings
        {
            get
            {
                return m_Settings;
            }
        }

        /// <summary>
        /// 获取构建版本。
        /// </summary>
        public string Version
        {
            get
            {
                return m_Version;
            }
        }

        /// <summary>
        /// 获取资源包类型。
        /// </summary>
        public EBundleType BundleType
        {
            get
            {
                return m_BundleType;
            }
        }

        /// <summary>
        /// 获取收集规则。
        /// </summary>
        public BundleCollectorPackage Package
        {
            get
            {
                return m_Package;
            }
        }

        internal void Initialize(ResourceSettings settings, string packageName)
        {
            m_Settings = settings;
            m_Package.PackageName = packageName;
            m_Package.IncludeAssetGUID = true;
        }
    }
}
