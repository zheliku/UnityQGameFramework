//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    public static partial class Utility
    {
        public static partial class Verifier
        {
            /// <summary>
            /// CRC32 算法。
            /// </summary>
            private sealed class Crc32
            {
                private const int TableLength = 256;
                private const uint DefaultPolynomial = 0xedb88320;
                private const uint DefaultSeed = 0xffffffff;

                private readonly uint m_Seed;
                private readonly uint[] m_Table;
                private uint m_Hash;

                /// <summary>
                /// 初始化 CRC32 校验器。
                /// </summary>
                public Crc32()
                    : this(DefaultPolynomial, DefaultSeed)
                {
                }

                /// <summary>
                /// 初始化 CRC32 校验器。
                /// </summary>
                /// <param name="polynomial">CRC32 多项式。</param>
                /// <param name="seed">校验初始值。</param>
                public Crc32(uint polynomial, uint seed)
                {
                    m_Seed = seed;
                    m_Table = InitializeTable(polynomial);
                    m_Hash = seed;
                }

                /// <summary>
                /// 重置 CRC32 校验值。
                /// </summary>
                public void Initialize()
                {
                    m_Hash = m_Seed;
                }

                /// <summary>
                /// 将指定数据段加入 CRC32 校验。
                /// </summary>
                /// <param name="bytes">待校验数据。</param>
                /// <param name="offset">数据段起始偏移。</param>
                /// <param name="length">数据段长度。</param>
                public void HashCore(byte[] bytes, int offset, int length)
                {
                    m_Hash = CalculateHash(m_Table, m_Hash, bytes, offset, length);
                }

                /// <summary>
                /// 获取最终 CRC32 校验值。
                /// </summary>
                /// <returns>最终 CRC32 校验值。</returns>
                public uint HashFinal()
                {
                    return ~m_Hash;
                }

                /// <summary>
                /// 计算数据段的累计 CRC32 校验值。
                /// </summary>
                /// <param name="table">CRC32 查找表。</param>
                /// <param name="value">累计校验值。</param>
                /// <param name="bytes">待校验数据。</param>
                /// <param name="offset">数据段起始偏移。</param>
                /// <param name="length">数据段长度。</param>
                /// <returns>更新后的累计校验值。</returns>
                private static uint CalculateHash(uint[] table, uint value, byte[] bytes, int offset, int length)
                {
                    int last = offset + length;
                    for (int i = offset; i < last; i++)
                    {
                        unchecked
                        {
                            value = (value >> 8) ^ table[bytes[i] ^ value & 0xff];
                        }
                    }

                    return value;
                }

                /// <summary>
                /// 创建 CRC32 查找表。
                /// </summary>
                /// <param name="polynomial">CRC32 多项式。</param>
                /// <returns>CRC32 查找表。</returns>
                private static uint[] InitializeTable(uint polynomial)
                {
                    uint[] table = new uint[TableLength];
                    for (int i = 0; i < TableLength; i++)
                    {
                        uint entry = (uint)i;
                        for (int j = 0; j < 8; j++)
                        {
                            if ((entry & 1) == 1)
                            {
                                entry = (entry >> 1) ^ polynomial;
                            }
                            else
                            {
                                entry >>= 1;
                            }
                        }

                        table[i] = entry;
                    }

                    return table;
                }
            }
        }
    }
}
