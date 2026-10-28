//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class SoundComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 声音组的序列化配置。
        /// </summary>
        [Serializable]
        private sealed class SoundGroup
        {
            [SerializeField]
            private string m_Name = null;

            [SerializeField]
            private bool m_AvoidBeingReplacedBySamePriority = false;

            [SerializeField]
            private bool m_Mute = false;

            [SerializeField, Range(0f, 1f)]
            private float m_Volume = 1f;

            [SerializeField]
            private int m_AgentHelperCount = 1;

            /// <summary>
            /// 获取声音组名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            /// <summary>
            /// 获取是否避免被同优先级声音替换。
            /// </summary>
            public bool AvoidBeingReplacedBySamePriority
            {
                get
                {
                    return m_AvoidBeingReplacedBySamePriority;
                }
            }

            /// <summary>
            /// 获取声音组是否静音。
            /// </summary>
            public bool Mute
            {
                get
                {
                    return m_Mute;
                }
            }

            /// <summary>
            /// 获取声音组音量。
            /// </summary>
            public float Volume
            {
                get
                {
                    return m_Volume;
                }
            }

            /// <summary>
            /// 获取声音代理辅助器数量。
            /// </summary>
            public int AgentHelperCount
            {
                get
                {
                    return m_AgentHelperCount;
                }
            }
        }
    }
}
