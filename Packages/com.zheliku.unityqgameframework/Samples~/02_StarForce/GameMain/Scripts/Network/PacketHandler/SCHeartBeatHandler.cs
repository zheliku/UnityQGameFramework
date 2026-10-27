//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Network;

namespace StarForce
{
    /// <summary>
    /// 处理服务端心跳回复。
    /// </summary>
    public class SCHeartBeatHandler : PacketHandlerBase
    {
        /// <summary>
        /// 获取当前实例或消息的标识。
        /// </summary>
        public override int Id
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// 网络消息包处理函数。
        /// </summary>
        /// <param name="sender">网络消息包源。</param>
        /// <param name="packet">网络消息包内容。</param>
        public override void Handle(object sender, Packet packet)
        {
            // 心跳包没有业务数据；通道收到包时会更新丢失计数。
        }
    }
}
