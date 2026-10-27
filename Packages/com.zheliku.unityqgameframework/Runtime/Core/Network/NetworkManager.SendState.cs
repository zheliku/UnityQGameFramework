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
        /// <summary>
        /// 管理待发送数据及发送缓冲。
        /// </summary>
        private sealed class SendState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private bool m_Disposed;

            /// <summary>
            /// 初始化消息发送状态。
            /// </summary>
            public SendState()
            {
                Stream = new MemoryStream(DefaultBufferLength);
                m_Disposed = false;
            }

            /// <summary>
            /// 获取发送数据缓冲流。
            /// </summary>
            public MemoryStream Stream { get; private set; }

            /// <summary>
            /// 获取或设置缓冲区中的消息包数量。
            /// </summary>
            public int PacketCount { get; set; }

            /// <summary>
            /// 清空发送缓冲区和消息包数量。
            /// </summary>
            public void Reset()
            {
                Stream.Position = 0L;
                Stream.SetLength(0L);
                PacketCount = 0;
            }

            /// <summary>
            /// 释放发送缓冲流。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 释放发送缓冲流。
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
        }
    }
}
