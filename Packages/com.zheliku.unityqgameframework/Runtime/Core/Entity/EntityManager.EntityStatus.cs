//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        /// <summary>
        /// 实体状态。
        /// </summary>
        private enum EntityStatus : byte
        {
            /// <summary>
            /// 未指定状态。
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// 即将初始化。
            /// </summary>
            WillInit,

            /// <summary>
            /// 已完成初始化。
            /// </summary>
            Inited,

            /// <summary>
            /// 即将显示。
            /// </summary>
            WillShow,

            /// <summary>
            /// 已显示。
            /// </summary>
            Showed,

            /// <summary>
            /// 即将隐藏。
            /// </summary>
            WillHide,

            /// <summary>
            /// 已隐藏。
            /// </summary>
            Hidden,

            /// <summary>
            /// 即将回收。
            /// </summary>
            WillRecycle,

            /// <summary>
            /// 已回收。
            /// </summary>
            Recycled
        }
    }
}
