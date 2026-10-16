//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        private sealed class HeartBeatState
        {
            public HeartBeatState()
            {
                HeartBeatElapseSeconds = 0f;
                MissHeartBeatCount = 0;
            }

            public float HeartBeatElapseSeconds { get; set; }

            public int MissHeartBeatCount { get; set; }

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
