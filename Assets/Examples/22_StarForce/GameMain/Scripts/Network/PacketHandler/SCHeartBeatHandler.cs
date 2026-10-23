//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Network;

namespace StarForce
{
    public class SCHeartBeatHandler : PacketHandlerBase
    {
        public override int Id => 2;

        public override void Handle(object sender, Packet packet)
        {
            // 心跳包没有业务数据；通道收到包时会更新丢失计数。
        }
    }
}
