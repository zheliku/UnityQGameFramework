//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 事件基类。
    /// </summary>
    public abstract class BaseEventArgs : FrameworkEventArgs
    {
        /// <summary>
        /// 获取类型编号。
        /// </summary>
        public abstract int Id { get; }
    }
}
