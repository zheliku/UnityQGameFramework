//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Network;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 显示网络通道和连接状态。
        /// </summary>
        private sealed class NetworkInformationWindow : ScrollableDebuggerWindowBase
        {
            private NetworkComponent m_NetworkComponent = null;

            /// <summary>
            /// 获取网络组件。
            /// </summary>
            /// <param name="args">窗口初始化参数。</param>
            public override void Initialize(params object[] args)
            {
                m_NetworkComponent = GameEntry.GetComponent<NetworkComponent>();
                if (m_NetworkComponent == null)
                {
                    Log.Fatal("Network component is invalid.");
                    return;
                }
            }

            /// <summary>
            /// 绘制网络频道信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Network Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Network Channel Count", m_NetworkComponent.NetworkChannelCount.ToString());
                }
                GUILayout.EndVertical();
                INetworkChannel[] networkChannels = m_NetworkComponent.GetAllNetworkChannels();
                for (int i = 0; i < networkChannels.Length; i++)
                {
                    DrawNetworkChannel(networkChannels[i]);
                }
            }

            /// <summary>
            /// 绘制网络频道收发及心跳状态。
            /// </summary>
            /// <param name="networkChannel">要显示的网络频道。</param>
            private void DrawNetworkChannel(INetworkChannel networkChannel)
            {
                GUILayout.Label(Utility.Text.Format("<b>Network Channel: {0} ({1})</b>", networkChannel.Name, networkChannel.Connected ? "Connected" : "Disconnected"));
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Service Type", networkChannel.ServiceType.ToString());
                    DrawItem("Address Family", networkChannel.AddressFamily.ToString());
                    DrawItem("Local Address", networkChannel.Connected ? networkChannel.Socket.LocalEndPoint.ToString() : "Unavailable");
                    DrawItem("Remote Address", networkChannel.Connected ? networkChannel.Socket.RemoteEndPoint.ToString() : "Unavailable");
                    DrawItem("Send Packet", Utility.Text.Format("{0} / {1}", networkChannel.SendPacketCount, networkChannel.SentPacketCount));
                    DrawItem("Receive Packet", Utility.Text.Format("{0} / {1}", networkChannel.ReceivePacketCount, networkChannel.ReceivedPacketCount));
                    DrawItem("Miss Heart Beat Count", networkChannel.MissHeartBeatCount.ToString());
                    DrawItem("Heart Beat", Utility.Text.Format("{0:F2} / {1:F2}", networkChannel.HeartBeatElapseSeconds, networkChannel.HeartBeatInterval));
                    if (networkChannel.Connected)
                    {
                        if (GUILayout.Button("Disconnect", GUILayout.Height(30f)))
                        {
                            networkChannel.Close();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}
