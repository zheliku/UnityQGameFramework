//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class ResourceCode
        {
            public ResourceCode(Platform platform, int length, int hashCode, int compressedLength, int compressedHashCode)
            {
                Platform = platform;
                Length = length;
                HashCode = hashCode;
                CompressedLength = compressedLength;
                CompressedHashCode = compressedHashCode;
            }

            public Platform Platform { get; }

            public int Length { get; }

            public int HashCode { get; }

            public int CompressedLength { get; }

            public int CompressedHashCode { get; }
        }
    }
}
