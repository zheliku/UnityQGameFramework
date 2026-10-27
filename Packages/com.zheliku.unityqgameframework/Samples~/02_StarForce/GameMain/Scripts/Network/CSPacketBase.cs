//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    /// <summary>
    /// 客户端发往服务端的消息基类。
    /// </summary>
    public abstract class CSPacketBase : PacketBase
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
