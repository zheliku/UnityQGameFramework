//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 等待实体附加时保存的请求信息。
    /// </summary>
    internal sealed class AttachEntityInfo : IReference
    {
        /// <summary>
        /// 初始化实体附加请求。
        /// </summary>
        public AttachEntityInfo()
        {
            ParentTransform = null;
            UserData = null;
        }

        /// <summary>
        /// 获取附加实体的父变换。
        /// </summary>
        public Transform ParentTransform
        {
            get; private set;
        }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData
        {
            get; private set;
        }

        /// <summary>
        /// 从引用池获取请求数据。
        /// </summary>
        /// <param name="parentTransform">附加实体的父变换。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>用于传递本次请求参数的数据对象。</returns>
        public static AttachEntityInfo Create(Transform parentTransform, object userData)
        {
            AttachEntityInfo attachEntityInfo = ReferencePool.Acquire<AttachEntityInfo>();
            attachEntityInfo.ParentTransform = parentTransform;
            attachEntityInfo.UserData = userData;
            return attachEntityInfo;
        }

        /// <summary>
        /// 清理请求参数及用户数据。
        /// </summary>
        public void Clear()
        {
            ParentTransform = null;
            UserData = null;
        }
    }
}
