//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// UnityEngine.Quaternion 变量类。
    /// </summary>
    public sealed class VarQuaternion : Variable<Quaternion>
    {
        /// <summary>
        /// 初始化 UnityEngine.Quaternion 变量类的新实例。
        /// </summary>
        public VarQuaternion()
        {
        }

        /// <summary>
        /// 从 UnityEngine.Quaternion 到 UnityEngine.Quaternion 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>从引用池获取并保存指定值的变量对象。</returns>
        public static implicit operator VarQuaternion(Quaternion value)
        {
            VarQuaternion varValue = ReferencePool.Acquire<VarQuaternion>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Quaternion 变量类到 UnityEngine.Quaternion 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>变量对象中保存的值。</returns>
        public static implicit operator Quaternion(VarQuaternion value)
        {
            return value.Value;
        }
    }
}
