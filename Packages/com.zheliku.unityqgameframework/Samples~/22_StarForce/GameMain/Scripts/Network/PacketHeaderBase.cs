//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Network;

namespace StarForce
{
    public abstract class PacketHeaderBase : IPacketHeader, IReference
    {
        public abstract PacketType PacketType { get; }

        public int Id { get; set; }

        public int PacketLength { get; set; }

        public bool IsValid => PacketType != PacketType.Undefined && Id > 0 && PacketLength >= 0;

        public void Clear()
        {
            Id = 0;
            PacketLength = 0;
        }
    }
}
