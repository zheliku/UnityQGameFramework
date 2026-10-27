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
        /// 头数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct HeaderData
        {
            private const int HeaderLength = 3;
            private const int FileSystemVersion = 0;
            private const int EncryptBytesLength = 4;
            private static readonly byte[] Header = new byte[HeaderLength]
            {
                (byte)'G',
                (byte)'F',
                (byte)'F'
            };
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = HeaderLength)]
            private readonly byte[] m_Header;
            private readonly byte m_Version;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = EncryptBytesLength)]
            private readonly byte[] m_EncryptBytes;
            private readonly int m_MaxFileCount;
            private readonly int m_MaxBlockCount;
            private readonly int m_BlockCount;

            /// <summary>
            /// 初始化文件系统头数据。
            /// </summary>
            /// <param name="maxFileCount">最大文件数量。</param>
            /// <param name="maxBlockCount">最大块数量。</param>
            public HeaderData(int maxFileCount, int maxBlockCount) : this(FileSystemVersion, new byte[EncryptBytesLength], maxFileCount, maxBlockCount, 0)
            {
                Utility.Random.GetRandomBytes(m_EncryptBytes);
            }

            /// <summary>
            /// 初始化文件系统头数据。
            /// </summary>
            /// <param name="version">文件系统版本。</param>
            /// <param name="encryptBytes">文件名加密字节序列。</param>
            /// <param name="maxFileCount">最大文件数量。</param>
            /// <param name="maxBlockCount">最大块数量。</param>
            /// <param name="blockCount">已有块数量。</param>
            public HeaderData(byte version, byte[] encryptBytes, int maxFileCount, int maxBlockCount, int blockCount)
            {
                m_Header = Header;
                m_Version = version;
                m_EncryptBytes = encryptBytes;
                m_MaxFileCount = maxFileCount;
                m_MaxBlockCount = maxBlockCount;
                m_BlockCount = blockCount;
            }

            /// <summary>
            /// 获取文件系统头标识是否有效。
            /// </summary>
            public bool IsValid
            {
                get
                {
                    return m_Header.Length == HeaderLength && m_Header[0] == Header[0] && m_Header[1] == Header[1] && m_Header[2] == Header[2] && m_Version == FileSystemVersion && m_EncryptBytes.Length == EncryptBytesLength && m_MaxFileCount > 0 && m_MaxBlockCount > 0 && m_MaxFileCount <= m_MaxBlockCount && m_BlockCount > 0 && m_BlockCount <= m_MaxBlockCount;
                }
            }

            /// <summary>
            /// 获取文件系统版本。
            /// </summary>
            public byte Version
            {
                get
                {
                    return m_Version;
                }
            }

            /// <summary>
            /// 获取最大文件数量。
            /// </summary>
            public int MaxFileCount
            {
                get
                {
                    return m_MaxFileCount;
                }
            }

            /// <summary>
            /// 获取最大块数量。
            /// </summary>
            public int MaxBlockCount
            {
                get
                {
                    return m_MaxBlockCount;
                }
            }

            /// <summary>
            /// 获取已有块数量。
            /// </summary>
            public int BlockCount
            {
                get
                {
                    return m_BlockCount;
                }
            }

            /// <summary>
            /// 获取文件名加密字节序列。
            /// </summary>
            /// <returns>文件名加密字节序列。</returns>
            public byte[] GetEncryptBytes()
            {
                return m_EncryptBytes;
            }

            /// <summary>
            /// 生成指定块数量的头数据。
            /// </summary>
            /// <param name="blockCount">新的块数量。</param>
            /// <returns>更新块数量后的头数据。</returns>
            public HeaderData SetBlockCount(int blockCount)
            {
                return new HeaderData(m_Version, m_EncryptBytes, m_MaxFileCount, m_MaxBlockCount, blockCount);
            }
        }
    }
}
