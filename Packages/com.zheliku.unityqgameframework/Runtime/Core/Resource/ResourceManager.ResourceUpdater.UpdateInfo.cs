//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceUpdater
        {
            /// <summary>
            /// 资源更新信息。
            /// </summary>
            private sealed class UpdateInfo
            {
                /// <summary>
                /// 初始化资源更新信息的新实例。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                /// <param name="fileSystemName">资源所在的文件系统名称。</param>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                /// <param name="compressedLength">压缩后大小。</param>
                /// <param name="compressedHashCode">压缩后哈希值。</param>
                /// <param name="resourcePath">资源路径。</param>
                public UpdateInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode, string resourcePath)
                {
                    ResourceName = resourceName;
                    FileSystemName = fileSystemName;
                    LoadType = loadType;
                    Length = length;
                    HashCode = hashCode;
                    CompressedLength = compressedLength;
                    CompressedHashCode = compressedHashCode;
                    ResourcePath = resourcePath;
                    Downloading = false;
                    RetryCount = 0;
                }

                /// <summary>
                /// 获取资源名称。
                /// </summary>
                public ResourceName ResourceName
                {
                    get;
                }

                /// <summary>
                /// 获取资源是否使用文件系统。
                /// </summary>
                public bool UseFileSystem
                {
                    get
                    {
                        return !string.IsNullOrEmpty(FileSystemName);
                    }
                }

                /// <summary>
                /// 获取资源所在的文件系统名称。
                /// </summary>
                public string FileSystemName
                {
                    get;
                }

                /// <summary>
                /// 获取资源加载方式。
                /// </summary>
                public LoadType LoadType
                {
                    get;
                }

                /// <summary>
                /// 获取资源大小。
                /// </summary>
                public int Length
                {
                    get;
                }

                /// <summary>
                /// 获取资源哈希值。
                /// </summary>
                public int HashCode
                {
                    get;
                }

                /// <summary>
                /// 获取压缩后大小。
                /// </summary>
                public int CompressedLength
                {
                    get;
                }

                /// <summary>
                /// 获取压缩后哈希值。
                /// </summary>
                public int CompressedHashCode
                {
                    get;
                }

                /// <summary>
                /// 获取资源路径。
                /// </summary>
                public string ResourcePath
                {
                    get;
                }

                /// <summary>
                /// 获取或设置下载状态。
                /// </summary>
                public bool Downloading
                {
                    get; set;
                }

                /// <summary>
                /// 获取或设置已重试次数。
                /// </summary>
                public int RetryCount
                {
                    get; set;
                }
            }
        }
    }
}
