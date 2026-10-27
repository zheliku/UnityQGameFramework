//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        private sealed partial class ResourceChecker
        {
            private sealed partial class CheckInfo
            {
                /// <summary>
                /// 本地资源状态信息。
                /// </summary>
                [StructLayout(LayoutKind.Auto)]
                private struct LocalVersionInfo
                {
                    /// <summary>
                    /// 初始化资源版本记录。
                    /// </summary>
                    /// <param name="fileSystemName">文件系统名称。</param>
                    /// <param name="loadType">资源加载方式。</param>
                    /// <param name="length">资源字节数。</param>
                    /// <param name="hashCode">资源 CRC32 校验值。</param>
                    public LocalVersionInfo(string fileSystemName, LoadType loadType, int length, int hashCode)
                    {
                        Exist = true;
                        FileSystemName = fileSystemName;
                        LoadType = loadType;
                        Length = length;
                        HashCode = hashCode;
                    }

                    /// <summary>
                    /// 获取该版本记录是否存在。
                    /// </summary>
                    public bool Exist { get; }

                    /// <summary>
                    /// 获取资源是否保存在文件系统中。
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
                    /// 获取资源加载方式。
                    /// </summary>
                    public LoadType LoadType { get; }

                    /// <summary>
                    /// 获取资源字节数。
                    /// </summary>
                    public int Length { get; }

                    /// <summary>
                    /// 获取资源 CRC32 校验值。
                    /// </summary>
                    public int HashCode { get; }
                }
            }
        }
    }
}
