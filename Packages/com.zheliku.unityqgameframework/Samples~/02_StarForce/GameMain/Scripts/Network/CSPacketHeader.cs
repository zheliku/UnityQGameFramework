//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    /// <summary>
    /// 客户端发送消息的消息头。
    /// </summary>
    public sealed class CSPacketHeader : PacketHeaderBase
    {
        /// <summary>
        /// 消息的发送方向。
        /// </summary>
        public override PacketType PacketType
        {
            get
            {
                return PacketType.ClientToServer;
            }
        }
    }
}
