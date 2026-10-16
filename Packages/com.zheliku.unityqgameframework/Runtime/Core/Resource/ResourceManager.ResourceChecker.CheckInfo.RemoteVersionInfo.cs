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
                /// 远程资源状态信息。
                /// </summary>
                [StructLayout(LayoutKind.Auto)]
                private struct RemoteVersionInfo
                {
                    public RemoteVersionInfo(string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode)
                    {
                        Exist = true;
                        FileSystemName = fileSystemName;
                        LoadType = loadType;
                        Length = length;
                        HashCode = hashCode;
                        CompressedLength = compressedLength;
                        CompressedHashCode = compressedHashCode;
                    }

                    public bool Exist { get; }

                    public bool UseFileSystem => !string.IsNullOrEmpty(FileSystemName);

                    public string FileSystemName { get; }

                    public LoadType LoadType { get; }

                    public int Length { get; }

                    public int HashCode { get; }

                    public int CompressedLength { get; }

                    public int CompressedHashCode { get; }
                }
            }
        }
    }
}
