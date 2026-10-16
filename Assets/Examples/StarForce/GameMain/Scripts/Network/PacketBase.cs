//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Network;
using ProtoBuf;

namespace StarForce
{
    public abstract class PacketBase : Packet, IExtensible
    {
        private IExtension m_ExtensionObject;

        public PacketBase()
        {
            m_ExtensionObject = null;
        }

        public abstract PacketType PacketType { get; }

        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref m_ExtensionObject, createIfMissing);
        }
    }
}
