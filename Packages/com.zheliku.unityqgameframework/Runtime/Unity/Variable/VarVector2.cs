//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// UnityEngine.Vector2 变量类。
    /// </summary>
    public sealed class VarVector2 : Variable<Vector2>
    {
        /// <summary>
        /// 初始化 UnityEngine.Vector2 变量类的新实例。
        /// </summary>
        public VarVector2()
        {
        }

        /// <summary>
        /// 从 UnityEngine.Vector2 到 UnityEngine.Vector2 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarVector2(Vector2 value)
        {
            VarVector2 varValue = ReferencePool.Acquire<VarVector2>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Vector2 变量类到 UnityEngine.Vector2 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator Vector2(VarVector2 value)
        {
            return value.Value;
        }
    }
}
