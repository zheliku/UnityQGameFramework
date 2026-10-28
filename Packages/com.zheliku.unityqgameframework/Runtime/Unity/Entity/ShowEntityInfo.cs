//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 等待实体显示时保存的请求信息。
    /// </summary>
    internal sealed class ShowEntityInfo : IReference
    {
        /// <summary>
        /// 初始化实体显示请求。
        /// </summary>
        public ShowEntityInfo()
        {
            EntityLogicType = null;
            UserData = null;
        }

        /// <summary>
        /// 获取实体逻辑类型。
        /// </summary>
        public Type EntityLogicType
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
        /// <param name="entityLogicType">实体逻辑类型。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>用于传递本次请求参数的数据对象。</returns>
        public static ShowEntityInfo Create(Type entityLogicType, object userData)
        {
            ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo.EntityLogicType = entityLogicType;
            showEntityInfo.UserData = userData;
            return showEntityInfo;
        }

        /// <summary>
        /// 清理请求参数及用户数据。
        /// </summary>
        public void Clear()
        {
            EntityLogicType = null;
            UserData = null;
        }
    }
}
