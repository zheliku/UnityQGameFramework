//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceVerifier
        {
            /// <summary>
            /// 资源校验信息。
            /// </summary>
            private struct VerifyInfo
            {
                /// <summary>
                /// 初始化资源校验信息的新实例。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                /// <param name="fileSystemName">资源所在的文件系统名称。</param>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                public VerifyInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode)
                {
                    ResourceName = resourceName;
                    FileSystemName = fileSystemName;
                    LoadType = loadType;
                    Length = length;
                    HashCode = hashCode;
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
            }
        }
    }
}
