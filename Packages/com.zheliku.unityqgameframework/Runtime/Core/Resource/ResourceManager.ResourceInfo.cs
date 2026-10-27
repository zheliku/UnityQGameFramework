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
        private sealed class ResourceInfo
        {
            /// <summary>
            /// 初始化资源信息的新实例。
            /// </summary>
            /// <param name="resourceName">资源名称。</param>
            /// <param name="fileSystemName">文件系统名称。</param>
            /// <param name="loadType">资源加载方式。</param>
            /// <param name="length">资源大小。</param>
            /// <param name="hashCode">资源哈希值。</param>
            /// <param name="compressedLength">压缩后资源大小。</param>
            /// <param name="storageInReadOnly">资源是否在只读区。</param>
            /// <param name="ready">资源是否准备完毕。</param>
            public ResourceInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, bool storageInReadOnly, bool ready)
            {
                ResourceName = resourceName;
                FileSystemName = fileSystemName;
                LoadType = loadType;
                Length = length;
                HashCode = hashCode;
                CompressedLength = compressedLength;
                StorageInReadOnly = storageInReadOnly;
                Ready = ready;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public ResourceName ResourceName { get; }

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
            /// 获取文件系统名称。
            /// </summary>
            public string FileSystemName { get; }

            /// <summary>
            /// 获取资源是否通过二进制方式加载。
            /// </summary>
            public bool IsLoadFromBinary
            {
                get
                {
                    return LoadType == LoadType.LoadFromBinary || LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || LoadType == LoadType.LoadFromBinaryAndDecrypt;
                }
            }

            /// <summary>
            /// 获取资源加载方式。
            /// </summary>
            public LoadType LoadType { get; }

            /// <summary>
            /// 获取资源大小。
            /// </summary>
            public int Length { get; }

            /// <summary>
            /// 获取资源哈希值。
            /// </summary>
            public int HashCode { get; }

            /// <summary>
            /// 获取压缩后资源大小。
            /// </summary>
            public int CompressedLength { get; }

            /// <summary>
            /// 获取资源是否在只读区。
            /// </summary>
            public bool StorageInReadOnly { get; }

            /// <summary>
            /// 获取资源是否准备完毕。
            /// </summary>
            public bool Ready { get; private set; }

            /// <summary>
            /// 标记资源准备完毕。
            /// </summary>
            public void MarkReady()
            {
                Ready = true;
            }
        }
    }
}
