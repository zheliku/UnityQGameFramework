//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace StarForce
{
    /// <summary>
    /// 游戏消息处理器的基类。
    /// </summary>
    public abstract class PacketHandlerBase : IPacketHandler
    {
        /// <summary>
        /// 获取当前实例或消息的标识。
        /// </summary>
        public abstract int Id
        {
            get;
        }

        /// <summary>
        /// 处理收到的网络消息。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="packet">待处理的网络消息。</param>
        public abstract void Handle(object sender, Packet packet);
    }
}
