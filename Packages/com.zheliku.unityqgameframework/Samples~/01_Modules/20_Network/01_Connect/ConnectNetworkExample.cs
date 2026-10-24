using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Network;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using ConnectedArgs = UnityQGameFramework.Runtime.NetworkConnectedEventArgs;
using ClosedArgs = UnityQGameFramework.Runtime.NetworkClosedEventArgs;
using ErrorArgs = UnityQGameFramework.Runtime.NetworkErrorEventArgs;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ConnectNetworkExample : MonoBehaviour
    {
        [SerializeField] private Button connectButton, closeButton;
        [SerializeField] private Text stateText;
        private NetworkEchoServer server;
        private INetworkChannel channel;
        private bool connecting;

        private void Start()
        {
            server = new NetworkEchoServer(); // 示例自带本机服务器，无需外网。
            // 创建通道只登记协议；Connect 才发起 Socket 连接。
            channel = QGF.Network.CreateNetworkChannel("Example.Main", ServiceType.Tcp, new NetworkTextProtocol());
            channel.HeartBeatInterval = 0;
            QGF.Event.Subscribe(ConnectedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ClosedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ErrorArgs.EventId, OnEvent);
            connectButton.onClick.AddListener(() =>
            {
                connecting = true;
                stateText.text = "正在连接";
                channel.Connect(IPAddress.Loopback, server.Port);
            });
            closeButton.onClick.AddListener(() => channel.Close());
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            // 全局事件先确认通道，避免处理其他业务连接的结果。
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
                stateText.text = "已连接";
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
                stateText.text = "已断开，可以重新连接";
            else if (args is ErrorArgs error && ReferenceEquals(error.NetworkChannel, channel))
            {
                stateText.text = "连接失败：" + error.SocketErrorCode;
                channel.Close();
            }
            else return;
            connecting = false;
        }

        private void Update()
        {
            if (channel == null) return;
            connectButton.interactable = !connecting && !channel.Connected;
            closeButton.interactable = connecting || channel.Connected;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ConnectedArgs.EventId, OnEvent)) events.Unsubscribe(ConnectedArgs.EventId, OnEvent);
                if (events.Check(ClosedArgs.EventId, OnEvent)) events.Unsubscribe(ClosedArgs.EventId, OnEvent);
                if (events.Check(ErrorArgs.EventId, OnEvent)) events.Unsubscribe(ErrorArgs.EventId, OnEvent);
            }
            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null && channel != null) network.DestroyNetworkChannel("Example.Main");
            server?.Dispose();
        }
    }
}
