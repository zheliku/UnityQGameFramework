//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Resource
{
    /// <summary>
    /// 版本资源列表更新回调函数集。
    /// </summary>
    public sealed class UpdateVersionListCallbacks
    {
        /// <summary>
        /// 初始化版本资源列表更新回调函数集的新实例。
        /// </summary>
        /// <param name="updateVersionListSuccessCallback">版本资源列表更新成功回调函数。</param>
        public UpdateVersionListCallbacks(UpdateVersionListSuccessCallback updateVersionListSuccessCallback)
            : this(updateVersionListSuccessCallback, null)
        {
        }

        /// <summary>
        /// 初始化版本资源列表更新回调函数集的新实例。
        /// </summary>
        /// <param name="updateVersionListSuccessCallback">版本资源列表更新成功回调函数。</param>
        /// <param name="updateVersionListFailureCallback">版本资源列表更新失败回调函数。</param>
        public UpdateVersionListCallbacks(UpdateVersionListSuccessCallback updateVersionListSuccessCallback, UpdateVersionListFailureCallback updateVersionListFailureCallback)
        {
            if (updateVersionListSuccessCallback == null)
            {
                throw new FrameworkException("Update version list success callback is invalid.");
            }

            UpdateVersionListSuccessCallback = updateVersionListSuccessCallback;
            UpdateVersionListFailureCallback = updateVersionListFailureCallback;
        }

        /// <summary>
        /// 获取版本资源列表更新成功回调函数。
        /// </summary>
        public UpdateVersionListSuccessCallback UpdateVersionListSuccessCallback { get; }

        /// <summary>
        /// 获取版本资源列表更新失败回调函数。
        /// </summary>
        public UpdateVersionListFailureCallback UpdateVersionListFailureCallback { get; }
    }
}
