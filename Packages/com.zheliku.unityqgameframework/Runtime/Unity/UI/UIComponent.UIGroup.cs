//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class UIComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 界面组的序列化配置。
        /// </summary>
        [Serializable]
        private sealed class UIGroup
        {
            [SerializeField]
            private string m_Name = null;

            [SerializeField]
            private int m_Depth = 0;

            /// <summary>
            /// 获取界面组名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            /// <summary>
            /// 获取界面组深度。
            /// </summary>
            public int Depth
            {
                get
                {
                    return m_Depth;
                }
            }
        }
    }
}
