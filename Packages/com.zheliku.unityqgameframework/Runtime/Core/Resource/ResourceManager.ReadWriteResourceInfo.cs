//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        [StructLayout(LayoutKind.Auto)]
        private struct ReadWriteResourceInfo
        {
            public ReadWriteResourceInfo(string fileSystemName, LoadType loadType, int length, int hashCode)
            {
                FileSystemName = fileSystemName;
                LoadType = loadType;
                Length = length;
                HashCode = hashCode;
            }

            public bool UseFileSystem => !string.IsNullOrEmpty(FileSystemName);

            public string FileSystemName { get; }

            public LoadType LoadType { get; }

            public int Length { get; }

            public int HashCode { get; }
        }
    }
}
