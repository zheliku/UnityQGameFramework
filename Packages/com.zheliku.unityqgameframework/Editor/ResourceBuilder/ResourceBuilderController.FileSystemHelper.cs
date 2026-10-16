//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.FileSystem;

namespace UnityQGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class FileSystemHelper : IFileSystemHelper
        {
            public FileSystemStream CreateFileSystemStream(string fullPath, FileSystemAccess access, bool createNew)
            {
                return new CommonFileSystemStream(fullPath, access, createNew);
            }
        }
    }
}
