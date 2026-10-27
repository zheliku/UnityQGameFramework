//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// System.Decimal 变量类。
    /// </summary>
    public sealed class VarDecimal : Variable<decimal>
    {
        /// <summary>
        /// 初始化 System.Decimal 变量类的新实例。
        /// </summary>
        public VarDecimal()
        {
        }

        /// <summary>
        /// 从 System.Decimal 到 System.Decimal 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarDecimal(decimal value)
        {
            VarDecimal varValue = ReferencePool.Acquire<VarDecimal>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Decimal 变量类到 System.Decimal 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator decimal(VarDecimal value)
        {
            return value.Value;
        }
    }
}
