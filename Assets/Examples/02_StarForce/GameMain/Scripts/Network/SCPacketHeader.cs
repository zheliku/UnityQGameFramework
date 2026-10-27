//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    /// <summary>
    /// 服务端发送消息的消息头。
    /// </summary>
    public sealed class SCPacketHeader : PacketHeaderBase
    {
        /// <summary>
        /// 消息的发送方向。
        /// </summary>
        public override PacketType PacketType
        {
            get
            {
                return PacketType.ServerToClient;
            }
        }
    }
}
