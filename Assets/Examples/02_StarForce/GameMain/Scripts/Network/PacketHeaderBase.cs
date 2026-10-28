//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace StarForce
{
    /// <summary>
    /// 游戏网络消息头的公共数据。
    /// </summary>
    public abstract class PacketHeaderBase : IPacketHeader, IReference
    {
        /// <summary>
        /// 消息的发送方向。
        /// </summary>
        public abstract PacketType PacketType
        {
            get;
        }

        /// <summary>
        /// 获取当前实例或消息的标识。
        /// </summary>
        public int Id
        {
            get; set;
        }

        /// <summary>
        /// 获取包体字节数。
        /// </summary>
        public int PacketLength
        {
            get; set;
        }

        /// <summary>
        /// 获取消息头是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return PacketType != PacketType.Undefined && Id > 0 && PacketLength >= 0;
            }
        }

        /// <summary>
        /// 重置数据，供引用池复用。
        /// </summary>
        public void Clear()
        {
            Id = 0;
            PacketLength = 0;
        }
    }
}
