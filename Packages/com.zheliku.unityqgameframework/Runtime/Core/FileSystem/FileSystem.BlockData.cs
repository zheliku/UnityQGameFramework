//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core.FileSystem
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 块数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BlockData
        {
            /// <summary>
            /// 空块数据。
            /// </summary>
            public static readonly BlockData Empty = new BlockData(0, 0);
            private readonly int m_StringIndex;
            private readonly int m_ClusterIndex;
            private readonly int m_Length;

            /// <summary>
            /// 初始化块数据。
            /// </summary>
            /// <param name="clusterIndex">数据起始簇索引。</param>
            /// <param name="length">数据长度，以字节为单位。</param>
            public BlockData(int clusterIndex, int length) : this(-1, clusterIndex, length)
            {
            }

            /// <summary>
            /// 初始化块数据。
            /// </summary>
            /// <param name="stringIndex">文件名字符串索引；负值表示空闲块。</param>
            /// <param name="clusterIndex">数据起始簇索引。</param>
            /// <param name="length">数据长度，以字节为单位。</param>
            public BlockData(int stringIndex, int clusterIndex, int length)
            {
                m_StringIndex = stringIndex;
                m_ClusterIndex = clusterIndex;
                m_Length = length;
            }

            /// <summary>
            /// 获取块是否已分配给文件。
            /// </summary>
            public bool Using
            {
                get
                {
                    return m_StringIndex >= 0;
                }
            }

            /// <summary>
            /// 获取文件名字符串索引。
            /// </summary>
            public int StringIndex
            {
                get
                {
                    return m_StringIndex;
                }
            }

            /// <summary>
            /// 获取数据起始簇索引。
            /// </summary>
            public int ClusterIndex
            {
                get
                {
                    return m_ClusterIndex;
                }
            }

            /// <summary>
            /// 获取数据长度，以字节为单位。
            /// </summary>
            public int Length
            {
                get
                {
                    return m_Length;
                }
            }

            /// <summary>
            /// 将块标记为空闲并按簇大小对齐长度。
            /// </summary>
            /// <returns>释放后的空闲块数据。</returns>
            public BlockData Free()
            {
                return new BlockData(m_ClusterIndex, (int)GetUpBoundClusterOffset(m_Length));
            }
        }
    }
}
