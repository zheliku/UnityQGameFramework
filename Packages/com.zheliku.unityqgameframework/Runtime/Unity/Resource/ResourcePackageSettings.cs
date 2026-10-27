using System;
using UnityEngine;
using UnityEngine.Serialization;
using YooAsset;

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

        [SerializeField, FormerlySerializedAs("m_RuntimePlayMode")]
        private EPlayMode m_Mode = EPlayMode.EditorSimulateMode;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_PlayMode")]
        private int m_LegacyPlayMode = -1;

        [SerializeField]
        [Tooltip("Include StreamingAssets in HostPlayMode. Disable for a remote-only package.")]
        private bool m_UseBuiltinFiles = true;

        [SerializeField]
        private string m_HostServer;

        [SerializeField]
        private string m_FallbackServer;

        [SerializeField]
        private string m_PackageVersion;

        [SerializeField]
        private string m_InitializerTypeName;

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
        /// 获取资源包使用的 YooAsset 运行模式。
        /// </summary>
        public EPlayMode PlayMode
        {
            get
            {
                return m_Mode;
            }
        }

        /// <summary>
        /// 获取自定义初始化扩展的完整类型名。
        /// </summary>
        public string InitializerTypeName
        {
            get
            {
                return m_InitializerTypeName;
            }
        }

        /// <summary>
        /// 将旧版模式编号迁移为单一运行模式，保留原 Player 模式。
        /// </summary>
        /// <param name="serializationVersion">原配置的序列化版本。</param>
        internal void UpgradePlayMode(int serializationVersion)
        {
            if (serializationVersion < 2 && m_LegacyPlayMode >= 0)
            {
                m_Mode = (EPlayMode)(m_LegacyPlayMode + 1);
            }
            m_LegacyPlayMode = -1;
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
