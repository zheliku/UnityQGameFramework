//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 加载数据表成功事件。
    /// </summary>
    public sealed class LoadDataTableSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 加载数据表成功事件编号。
        /// </summary>
        public static readonly int EventId = typeof(LoadDataTableSuccessEventArgs).GetHashCode();

        /// <summary>
        /// 初始化加载数据表成功事件的新实例。
        /// </summary>
        public LoadDataTableSuccessEventArgs()
        {
            DataTableAssetName = null;
            Duration = 0f;
            UserData = null;
        }

        /// <summary>
        /// 获取加载数据表成功事件编号。
        /// </summary>
        public override int Id => EventId;

        /// <summary>
        /// 获取数据表资源名称。
        /// </summary>
        public string DataTableAssetName { get; private set; }

        /// <summary>
        /// 获取加载持续时间。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建加载数据表成功事件。
        /// </summary>
        /// <returns>创建的加载数据表成功事件。</returns>
        public static LoadDataTableSuccessEventArgs Create(string assetName, float duration, object userData)
        {
            var args = ReferencePool.Acquire<LoadDataTableSuccessEventArgs>();
            args.DataTableAssetName = assetName;
            args.Duration = duration;
            args.UserData = userData;
            return args;
        }

        /// <summary>
        /// 清理加载数据表成功事件。
        /// </summary>
        public override void Clear()
        {
            DataTableAssetName = null;
            Duration = 0f;
            UserData = null;
        }
    }
}
