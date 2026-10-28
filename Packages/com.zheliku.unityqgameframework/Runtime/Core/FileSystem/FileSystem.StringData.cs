//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 字符串数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct StringData
        {
            private static readonly byte[] s_CachedBytes = new byte[byte.MaxValue + 1];

            private readonly byte m_Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = byte.MaxValue)]
            private readonly byte[] m_Bytes;

            /// <summary>
            /// 初始化文件名字符串数据。
            /// </summary>
            /// <param name="length">有效字符串的 UTF-8 字节数。</param>
            /// <param name="bytes">用于保存加密字符串的缓冲区。</param>
            public StringData(byte length, byte[] bytes)
            {
                m_Length = length;
                m_Bytes = bytes;
            }

            /// <summary>
            /// 解密并读取文件名字符串。
            /// </summary>
            /// <param name="encryptBytes">文件名加密字节序列。</param>
            /// <returns>解密后的文件名字符串。</returns>
            public string GetString(byte[] encryptBytes)
            {
                if (m_Length <= 0)
                {
                    return null;
                }

                Array.Copy(m_Bytes, 0, s_CachedBytes, 0, m_Length);
                Utility.Encryption.GetSelfXorBytes(s_CachedBytes, 0, m_Length, encryptBytes);
                return Utility.Converter.GetString(s_CachedBytes, 0, m_Length);
            }

            /// <summary>
            /// 加密并写入文件名字符串。
            /// </summary>
            /// <param name="value">文件名字符串。</param>
            /// <param name="encryptBytes">文件名加密字节序列。</param>
            /// <returns>写入后的字符串数据。</returns>
            public StringData SetString(string value, byte[] encryptBytes)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return Clear();
                }

                int length = Utility.Converter.GetBytes(value, s_CachedBytes);
                if (length > byte.MaxValue)
                {
                    throw new GameFrameworkException(Utility.Text.Format("String '{0}' is too long.", value));
                }

                Utility.Encryption.GetSelfXorBytes(s_CachedBytes, encryptBytes);
                Array.Copy(s_CachedBytes, 0, m_Bytes, 0, length);
                return new StringData((byte)length, m_Bytes);
            }

            /// <summary>
            /// 清空字符串的有效长度。
            /// </summary>
            /// <returns>清空后的字符串数据。</returns>
            public StringData Clear()
            {
                return new StringData(0, m_Bytes);
            }
        }
    }
}
