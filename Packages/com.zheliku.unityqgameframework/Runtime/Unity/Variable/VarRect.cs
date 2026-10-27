//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// UnityEngine.Rect 变量类。
    /// </summary>
    public sealed class VarRect : Variable<Rect>
    {
        /// <summary>
        /// 初始化 UnityEngine.Rect 变量类的新实例。
        /// </summary>
        public VarRect()
        {
        }

        /// <summary>
        /// 从 UnityEngine.Rect 到 UnityEngine.Rect 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarRect(Rect value)
        {
            VarRect varValue = ReferencePool.Acquire<VarRect>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Rect 变量类到 UnityEngine.Rect 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator Rect(VarRect value)
        {
            return value.Value;
        }
    }
}
