//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    /// <summary>
    /// 服务端发往客户端的消息基类。
    /// </summary>
    public abstract class SCPacketBase : PacketBase
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
