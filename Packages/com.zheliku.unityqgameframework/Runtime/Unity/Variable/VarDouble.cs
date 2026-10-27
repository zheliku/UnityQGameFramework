//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// System.Double 变量类。
    /// </summary>
    public sealed class VarDouble : Variable<double>
    {
        /// <summary>
        /// 初始化 System.Double 变量类的新实例。
        /// </summary>
        public VarDouble()
        {
        }

        /// <summary>
        /// 从 System.Double 到 System.Double 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarDouble(double value)
        {
            VarDouble varValue = ReferencePool.Acquire<VarDouble>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Double 变量类到 System.Double 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator double(VarDouble value)
        {
            return value.Value;
        }
    }
}
