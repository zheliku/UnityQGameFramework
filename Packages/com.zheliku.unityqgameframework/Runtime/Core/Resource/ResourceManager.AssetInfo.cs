//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源信息。
        /// </summary>
        private sealed class AssetInfo
        {
            private readonly string[] m_DependencyAssetNames;

            /// <summary>
            /// 初始化资源信息的新实例。
            /// </summary>
            /// <param name="assetName">资源名称。</param>
            /// <param name="resourceName">所在资源名称。</param>
            /// <param name="dependencyAssetNames">依赖资源名称。</param>
            public AssetInfo(string assetName, ResourceName resourceName, string[] dependencyAssetNames)
            {
                AssetName = assetName;
                ResourceName = resourceName;
                m_DependencyAssetNames = dependencyAssetNames;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public string AssetName { get; }

            /// <summary>
            /// 获取所在资源名称。
            /// </summary>
            public ResourceName ResourceName { get; }

            /// <summary>
            /// 获取依赖资源名称。
            /// </summary>
            /// <returns>依赖资源名称。</returns>
            public string[] GetDependencyAssetNames()
            {
                return m_DependencyAssetNames;
            }
        }
    }
}
