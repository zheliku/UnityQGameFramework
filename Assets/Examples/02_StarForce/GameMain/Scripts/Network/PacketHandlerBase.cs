//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Network;

namespace StarForce
{
    public abstract class PacketHandlerBase : IPacketHandler
    {
        public abstract int Id { get; }

        public abstract void Handle(object sender, Packet packet);
    }
}
