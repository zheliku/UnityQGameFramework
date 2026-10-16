//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Network
{
    /// <summary>
    /// 网络消息包头接口。
    /// </summary>
    public interface IPacketHeader
    {
        /// <summary>
        /// 获取网络消息包长度。
        /// </summary>
        int PacketLength { get; }
    }
}
