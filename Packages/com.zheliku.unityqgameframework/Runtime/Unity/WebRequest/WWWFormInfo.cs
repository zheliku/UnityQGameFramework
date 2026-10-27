//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 记录 Web 表单请求及其提交信息。
    /// </summary>
    internal sealed class WWWFormInfo : IReference
    {
        /// <summary>
        /// 初始化 Web 表单请求。
        /// </summary>
        public WWWFormInfo()
        {
            WWWForm = null;
            UserData = null;
        }

        /// <summary>
        /// 获取 Web 请求表单。
        /// </summary>
        public WWWForm WWWForm { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 从引用池获取请求数据。
        /// </summary>
        /// <param name="wwwForm">Web 请求表单。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>用于传递本次请求参数的数据对象。</returns>
        public static WWWFormInfo Create(WWWForm wwwForm, object userData)
        {
            WWWFormInfo wwwFormInfo = ReferencePool.Acquire<WWWFormInfo>();
            wwwFormInfo.WWWForm = wwwForm;
            wwwFormInfo.UserData = userData;
            return wwwFormInfo;
        }

        /// <summary>
        /// 清理请求参数及用户数据。
        /// </summary>
        public void Clear()
        {
            WWWForm = null;
            UserData = null;
        }
    }
}
