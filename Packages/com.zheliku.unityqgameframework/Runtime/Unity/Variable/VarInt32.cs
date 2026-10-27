//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// System.Int32 变量类。
    /// </summary>
    public sealed class VarInt32 : Variable<int>
    {
        /// <summary>
        /// 初始化 System.Int32 变量类的新实例。
        /// </summary>
        public VarInt32()
        {
        }

        /// <summary>
        /// 从 System.Int32 到 System.Int32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarInt32(int value)
        {
            VarInt32 varValue = ReferencePool.Acquire<VarInt32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Int32 变量类到 System.Int32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator int(VarInt32 value)
        {
            return value.Value;
        }
    }
}
