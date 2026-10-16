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
                    public LocalVersionInfo(string fileSystemName, LoadType loadType, int length, int hashCode)
                    {
                        Exist = true;
                        FileSystemName = fileSystemName;
                        LoadType = loadType;
                        Length = length;
                        HashCode = hashCode;
                    }

                    public bool Exist { get; }

                    public bool UseFileSystem => !string.IsNullOrEmpty(FileSystemName);

                    public string FileSystemName { get; }

                    public LoadType LoadType { get; }

                    public int Length { get; }

                    public int HashCode { get; }
                }
            }
        }
    }
}
