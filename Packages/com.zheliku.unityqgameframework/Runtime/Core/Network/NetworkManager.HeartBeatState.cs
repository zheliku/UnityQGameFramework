//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        /// <summary>
        /// 记录心跳时间和连续丢失次数。
        /// </summary>
        private sealed class HeartBeatState
        {
            /// <summary>
            /// 初始化心跳状态。
            /// </summary>
            public HeartBeatState()
            {
                HeartBeatElapseSeconds = 0f;
                MissHeartBeatCount = 0;
            }

            /// <summary>
            /// 获取或设置距上次心跳的时长，以秒为单位。
            /// </summary>
            public float HeartBeatElapseSeconds { get; set; }

            /// <summary>
            /// 获取或设置连续丢失心跳次数。
            /// </summary>
            public int MissHeartBeatCount { get; set; }

            /// <summary>
            /// 重置心跳丢失计数及可选的计时。
            /// </summary>
            /// <param name="resetHeartBeatElapseSeconds">是否同时重置心跳计时。</param>
            public void Reset(bool resetHeartBeatElapseSeconds)
            {
                if (resetHeartBeatElapseSeconds)
                {
                    HeartBeatElapseSeconds = 0f;
                }

                MissHeartBeatCount = 0;
            }
        }
    }
}
