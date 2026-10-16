//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    public abstract class CSPacketBase : PacketBase
    {
        public override PacketType PacketType => PacketType.ClientToServer;
    }
}
