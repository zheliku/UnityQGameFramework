//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// System.Byte 变量类。
    /// </summary>
    public sealed class VarByte : Variable<byte>
    {
        /// <summary>
        /// 初始化 System.Byte 变量类的新实例。
        /// </summary>
        public VarByte()
        {
        }

        /// <summary>
        /// 从 System.Byte 到 System.Byte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarByte(byte value)
        {
            VarByte varValue = ReferencePool.Acquire<VarByte>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Byte 变量类到 System.Byte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator byte(VarByte value)
        {
            return value.Value;
        }
    }
}
