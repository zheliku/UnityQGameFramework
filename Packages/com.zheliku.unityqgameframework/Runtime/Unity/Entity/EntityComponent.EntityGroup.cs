//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class EntityComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 实体组的序列化配置。
        /// </summary>
        [Serializable]
        private sealed class EntityGroup
        {
            [SerializeField]
            private string m_Name = null;

            [SerializeField]
            private float m_InstanceAutoReleaseInterval = 60f;

            [SerializeField]
            private int m_InstanceCapacity = 16;

            [SerializeField]
            private float m_InstanceExpireTime = 60f;

            [SerializeField]
            private int m_InstancePriority = 0;

            /// <summary>
            /// 获取实体组名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            /// <summary>
            /// 获取实例池自动释放间隔，以秒为单位。
            /// </summary>
            public float InstanceAutoReleaseInterval
            {
                get
                {
                    return m_InstanceAutoReleaseInterval;
                }
            }

            /// <summary>
            /// 获取实例池容量。
            /// </summary>
            public int InstanceCapacity
            {
                get
                {
                    return m_InstanceCapacity;
                }
            }

            /// <summary>
            /// 获取闲置实例过期时间，以秒为单位。
            /// </summary>
            public float InstanceExpireTime
            {
                get
                {
                    return m_InstanceExpireTime;
                }
            }

            /// <summary>
            /// 获取实例池优先级。
            /// </summary>
            public int InstancePriority
            {
                get
                {
                    return m_InstancePriority;
                }
            }
        }
    }
}
