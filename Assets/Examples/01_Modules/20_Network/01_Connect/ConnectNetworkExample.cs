using TMPro;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示连接 TCP 通道。
    /// </summary>
    public sealed class ConnectNetworkExample : MonoBehaviour
    {
        [SerializeField]
        private Button connectButton, closeButton;
        [SerializeField]
        private TMP_Text stateText;
        private NetworkEchoServer server;
        private INetworkChannel channel;
        private bool connecting;
        private void Start()
        {
            server = new NetworkEchoServer(); // 示例自带本机服务器，无需外网。
            // 创建通道只登记协议；Connect 才发起 Socket 连接。
            channel = QGF.Network.CreateNetworkChannel("Example.Main", ServiceType.Tcp, new NetworkTextProtocol());
            channel.HeartBeatInterval = 0;
            QGF.Event.Subscribe(NetworkConnectedEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(NetworkClosedEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(NetworkErrorEventArgs.EventId, OnEvent);
            connectButton.onClick.AddListener(() =>
            {
                connecting = true;
                stateText.text = "Connecting";
                channel.Connect(IPAddress.Loopback, server.Port);
            });
            closeButton.onClick.AddListener(() => channel.Close());
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            // 全局事件先确认通道，避免处理其他业务连接的结果。
            if (args is NetworkConnectedEventArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                stateText.text = "Connected";
            }
            else if (args is NetworkClosedEventArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                stateText.text = "Disconnected. You can connect again.";
            }
            else if (args is NetworkErrorEventArgs error && ReferenceEquals(error.NetworkChannel, channel))
            {
                stateText.text = "Connection failed: " + error.SocketErrorCode;
                channel.Close();
            }
            else
            {
                return;
            }

            connecting = false;
        }

        private void Update()
        {
            if (channel == null)
            {
                return;
            }

            connectButton.interactable = !connecting && !channel.Connected;
            closeButton.interactable = connecting || channel.Connected;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(NetworkConnectedEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(NetworkConnectedEventArgs.EventId, OnEvent);
                }

                if (events.Check(NetworkClosedEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(NetworkClosedEventArgs.EventId, OnEvent);
                }

                if (events.Check(NetworkErrorEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(NetworkErrorEventArgs.EventId, OnEvent);
                }
            }

            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null && channel != null)
            {
                network.DestroyNetworkChannel("Example.Main");
            }

            server?.Dispose();
        }
    }
}
