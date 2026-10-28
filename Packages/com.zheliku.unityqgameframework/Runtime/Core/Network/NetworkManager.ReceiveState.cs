//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;

namespace UnityQGameFramework.Core
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        /// <summary>
        /// 管理接收缓冲及消息拆包进度。
        /// </summary>
        private sealed class ReceiveState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private bool m_Disposed;

            /// <summary>
            /// 初始化消息接收状态。
            /// </summary>
            public ReceiveState()
            {
                Stream = new MemoryStream(DefaultBufferLength);
                PacketHeader = null;
                m_Disposed = false;
            }

            /// <summary>
            /// 获取接收数据缓冲流。
            /// </summary>
            public MemoryStream Stream
            {
                get; private set;
            }

            /// <summary>
            /// 获取当前消息包头。
            /// </summary>
            public IPacketHeader PacketHeader
            {
                get; private set;
            }

            /// <summary>
            /// 准备接收消息包头。
            /// </summary>
            /// <param name="packetHeaderLength">消息包头字节数。</param>
            public void PrepareForPacketHeader(int packetHeaderLength)
            {
                Reset(packetHeaderLength, null);
            }

            /// <summary>
            /// 准备接收消息包体。
            /// </summary>
            /// <param name="packetHeader">已解析的消息包头。</param>
            public void PrepareForPacket(IPacketHeader packetHeader)
            {
                if (packetHeader == null)
                {
                    throw new GameFrameworkException("Packet header is invalid.");
                }

                Reset(packetHeader.PacketLength, packetHeader);
            }

            /// <summary>
            /// 释放接收缓冲流。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 释放接收缓冲流。
            /// </summary>
            /// <param name="disposing">是否释放托管资源。</param>
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

            /// <summary>
            /// 重置接收缓冲区和消息包头。
            /// </summary>
            /// <param name="targetLength">需要接收的字节数。</param>
            /// <param name="packetHeader">消息包头；接收包头时为空。</param>
            private void Reset(int targetLength, IPacketHeader packetHeader)
            {
                if (targetLength < 0)
                {
                    throw new GameFrameworkException("Target length is invalid.");
                }

                Stream.Position = 0L;
                Stream.SetLength(targetLength);
                PacketHeader = packetHeader;
            }
        }
    }
}
