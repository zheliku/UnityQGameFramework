using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源包配置。
    /// </summary>
    [Serializable]
    public sealed class ResourcePackageSettings
    {
        [SerializeField]
        private string m_Name = "DefaultPackage";

        [SerializeField]
        private ResourcePlayMode m_PlayMode = ResourcePlayMode.Offline;

        [SerializeField]
        [Tooltip("联机模式是否读取 StreamingAssets 中的首包；纯远端资源包请关闭。")]
        private bool m_UseBuiltinFiles = true;

        [SerializeField]
        private string m_HostServer;

        [SerializeField]
        private string m_FallbackServer;

        [SerializeField]
        private string m_PackageVersion;

        /// <summary>
        /// 获取资源包名称。
        /// </summary>
        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        /// <summary>
        /// 获取发行时的资源运行模式。
        /// </summary>
        public ResourcePlayMode PlayMode
        {
            get
            {
                return m_PlayMode;
            }
        }

        /// <summary>
        /// 获取联机模式是否使用内置资源。
        /// </summary>
        public bool UseBuiltinFiles
        {
            get
            {
                return m_UseBuiltinFiles;
            }
        }

        /// <summary>
        /// 获取资源服务器地址。
        /// </summary>
        public string HostServer
        {
            get
            {
                return m_HostServer;
            }
        }

        /// <summary>
        /// 获取备用资源服务器地址。
        /// </summary>
        public string FallbackServer
        {
            get
            {
                return string.IsNullOrEmpty(m_FallbackServer) ? m_HostServer : m_FallbackServer;
            }
        }

        /// <summary>
        /// 获取指定的清单版本，为空时请求服务器版本。
        /// </summary>
        public string PackageVersion
        {
            get
            {
                return m_PackageVersion;
            }
        }
    }
}
