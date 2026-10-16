//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        private sealed class ReceiveState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private bool m_Disposed;

            public ReceiveState()
            {
                Stream = new MemoryStream(DefaultBufferLength);
                PacketHeader = null;
                m_Disposed = false;
            }

            public MemoryStream Stream { get; private set; }

            public IPacketHeader PacketHeader { get; private set; }

            public void PrepareForPacketHeader(int packetHeaderLength)
            {
                Reset(packetHeaderLength, null);
            }

            public void PrepareForPacket(IPacketHeader packetHeader)
            {
                if (packetHeader == null)
                {
                    throw new FrameworkException("Packet header is invalid.");
                }

                Reset(packetHeader.PacketLength, packetHeader);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (m_Disposed)
                {
                    return;
                }

                if (disposing)
                {
                    if (Stream != null)
                    {
                        Stream.Dispose();
                        Stream = null;
                    }
                }

                m_Disposed = true;
            }

            private void Reset(int targetLength, IPacketHeader packetHeader)
            {
                if (targetLength < 0)
                {
                    throw new FrameworkException("Target length is invalid.");
                }

                Stream.Position = 0L;
                Stream.SetLength(targetLength);
                PacketHeader = packetHeader;
            }
        }
    }
}
