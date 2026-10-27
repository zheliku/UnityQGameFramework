//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// System.UInt32 变量类。
    /// </summary>
    public sealed class VarUInt32 : Variable<uint>
    {
        /// <summary>
        /// 初始化 System.UInt32 变量类的新实例。
        /// </summary>
        public VarUInt32()
        {
        }

        /// <summary>
        /// 从 System.UInt32 到 System.UInt32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarUInt32(uint value)
        {
            VarUInt32 varValue = ReferencePool.Acquire<VarUInt32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.UInt32 变量类到 System.UInt32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator uint(VarUInt32 value)
        {
            return value.Value;
        }
    }
}
