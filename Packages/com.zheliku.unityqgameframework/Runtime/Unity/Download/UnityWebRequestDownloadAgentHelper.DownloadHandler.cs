//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Download;
using System;
using UnityEngine.Networking;

namespace UnityQGameFramework.Runtime
{
    public partial class UnityWebRequestDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        /// <summary>
        /// 接收 UnityWebRequest 的下载数据并转发进度。
        /// </summary>
        private sealed class DownloadHandler : DownloadHandlerScript
        {
            private readonly UnityWebRequestDownloadAgentHelper m_Owner;

            /// <summary>
            /// 初始化下载数据处理器。
            /// </summary>
            /// <param name="owner">所属下载代理辅助器。</param>
            public DownloadHandler(UnityWebRequestDownloadAgentHelper owner) : base(owner.m_CachedBytes)
            {
                m_Owner = owner;
            }

            /// <summary>
            /// 校验响应并转发本次接收的数据和长度。
            /// </summary>
            /// <param name="data">接收到的数据缓冲区。</param>
            /// <param name="dataLength">本次有效字节数。</param>
            /// <returns>是否继续接收下载数据。</returns>
            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (m_Owner != null && m_Owner.m_UnityWebRequest != null && dataLength > 0)
                {
                    if (!m_Owner.ValidateResponse())
                    {
                        return false;
                    }

                    DownloadAgentHelperUpdateBytesEventArgs downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(data, 0, dataLength);
                    m_Owner.m_DownloadAgentHelperUpdateBytesEventHandler(this, downloadAgentHelperUpdateBytesEventArgs);
                    ReferencePool.Release(downloadAgentHelperUpdateBytesEventArgs);
                    if (m_Owner.m_UnityWebRequest == null)
                    {
                        return false;
                    }

                    DownloadAgentHelperUpdateLengthEventArgs downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(dataLength);
                    m_Owner.m_DownloadAgentHelperUpdateLengthEventHandler(this, downloadAgentHelperUpdateLengthEventArgs);
                    ReferencePool.Release(downloadAgentHelperUpdateLengthEventArgs);
                }

                return base.ReceiveData(data, dataLength);
            }
        }
    }
}
