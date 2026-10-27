//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// StarForce 使用的常量。
    /// </summary>
    public static partial class Constant
    {
        /// <summary>
        /// 层。
        /// </summary>
        public static class Layer
        {
            /// <summary>
            /// 默认物体层名称。
            /// </summary>
            public const string DefaultLayerName = "Default";

            /// <summary>
            /// 默认物体层编号。
            /// </summary>
            public static readonly int DefaultLayerId = LayerMask.NameToLayer(DefaultLayerName);

            /// <summary>
            /// 界面层名称。
            /// </summary>
            public const string UILayerName = "UI";

            /// <summary>
            /// 界面层编号。
            /// </summary>
            public static readonly int UILayerId = LayerMask.NameToLayer(UILayerName);

        }
    }
}
