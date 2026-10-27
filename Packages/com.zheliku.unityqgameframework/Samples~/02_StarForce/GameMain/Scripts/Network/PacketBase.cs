//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Network;
using ProtoBuf;

namespace StarForce
{
    /// <summary>
    /// 游戏网络消息的公共数据。
    /// </summary>
    public abstract class PacketBase : Packet, IExtensible
    {
        private IExtension m_ExtensionObject;

        /// <summary>
        /// 初始化游戏网络消息基类的新实例。
        /// </summary>
        public PacketBase()
        {
            m_ExtensionObject = null;
        }

        /// <summary>
        /// 获取消息的发送方向。
        /// </summary>
        public abstract PacketType PacketType { get; }

        /// <summary>
        /// 获取 Protobuf 扩展数据。
        /// </summary>
        /// <param name="createIfMissing">扩展数据不存在时是否创建。</param>
        /// <returns>扩展数据；不存在且不创建时返回空。</returns>
        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref m_ExtensionObject, createIfMissing);
        }
    }
}
