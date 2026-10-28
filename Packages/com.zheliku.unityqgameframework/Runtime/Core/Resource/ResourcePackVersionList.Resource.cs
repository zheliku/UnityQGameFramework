//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core
{
    public partial struct ResourcePackVersionList
    {
        /// <summary>
        /// 资源。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Resource
        {
            /// <summary>
            /// 初始化资源的新实例。
            /// </summary>
            /// <param name="name">资源名称。</param>
            /// <param name="variant">资源变体名称。</param>
            /// <param name="extension">资源扩展名称。</param>
            /// <param name="loadType">资源加载方式。</param>
            /// <param name="offset">资源偏移。</param>
            /// <param name="length">资源长度。</param>
            /// <param name="hashCode">资源哈希值。</param>
            /// <param name="compressedLength">资源压缩后长度。</param>
            /// <param name="compressedHashCode">资源压缩后哈希值。</param>
            public Resource(string name, string variant, string extension, byte loadType, long offset, int length, int hashCode, int compressedLength, int compressedHashCode)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new GameFrameworkException("Name is invalid.");
                }

                Name = name;
                Variant = variant;
                Extension = extension;
                LoadType = loadType;
                Offset = offset;
                Length = length;
                HashCode = hashCode;
                CompressedLength = compressedLength;
                CompressedHashCode = compressedHashCode;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public string Name
            {
                get;
            }

            /// <summary>
            /// 获取资源变体名称。
            /// </summary>
            public string Variant
            {
                get;
            }

            /// <summary>
            /// 获取资源扩展名称。
            /// </summary>
            public string Extension
            {
                get;
            }

            /// <summary>
            /// 获取资源加载方式。
            /// </summary>
            public byte LoadType
            {
                get;
            }

            /// <summary>
            /// 获取资源偏移。
            /// </summary>
            public long Offset
            {
                get;
            }

            /// <summary>
            /// 获取资源长度。
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
            /// 获取资源压缩后长度。
            /// </summary>
            public int CompressedLength
            {
                get;
            }

            /// <summary>
            /// 获取资源压缩后哈希值。
            /// </summary>
            public int CompressedHashCode
            {
                get;
            }
        }
    }
}
