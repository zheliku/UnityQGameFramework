using System;
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
    /// <summary>
    /// 发送两份文本并回显，默认处理器接收未注册的消息。
    /// </summary>
    public sealed class EchoNetworkExample : MonoBehaviour
    {
        [SerializeField]
        private Dropdown serviceMode;
        [SerializeField]
        private Toggle splitToggle;
        [SerializeField]
        private InputField firstInput, secondInput;
        [SerializeField]
        private Button connectButton, sendButton, noticeButton;
        [SerializeField]
        private Text stateText, responseText;
        private readonly System.Collections.Generic.List<string> responses = new System.Collections.Generic.List<string>();
        private int received, notices;
        private bool connecting;
        private NetworkEchoServer server;
        private INetworkChannel channel;
        private string state = "Not connected";
        private void Start()
        {
            server = new NetworkEchoServer();
            // Runtime 事件在主线程分发；先订阅，再建立连接。
            QGF.Event.Subscribe(ConnectedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ClosedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ErrorArgs.EventId, OnEvent);
            connectButton.onClick.AddListener(Connect);
            sendButton.onClick.AddListener(Send);
            noticeButton.onClick.AddListener(() => channel.Send(new NetworkTextPacket { MessageId = 3, Text = "Notification for the default handler" }));
            splitToggle.isOn = true;
            splitToggle.onValueChanged.AddListener(value => server.SplitResponses = value);
            firstInput.text = "Hello QGF";
            secondInput.text = "Second message + &= ?";
        }

        private void Connect()
        {
            responses.Clear();
            received = notices = 0;
            // 重连时先移除旧通道，避免同名注册和上一条 Socket 遗留。
            if (channel != null)
            {
                QGF.Network.DestroyNetworkChannel("Example.Echo");
            }

            // 创建只登记通道与协议；真正建立 Socket 的步骤在下面的 Connect。
            channel = QGF.Network.CreateNetworkChannel("Example.Echo", serviceMode.value == 0 ? ServiceType.Tcp : ServiceType.TcpWithSyncReceive, new NetworkTextProtocol());
            // 本例只观察消息回显，关闭周期心跳以免混入额外数据包。
            channel.HeartBeatInterval = 0;
            // Id 1 交给专门处理器；没有注册的 Id 3 交给默认处理器。
            channel.RegisterHandler(new NetworkTextHandler(text =>
            {
                responses.Add(text);
                received++;
            }));
            channel.SetDefaultHandler((sender, packet) =>
            {
                responses.Add("Default handler: " + ((NetworkTextPacket)packet).Text);
                notices++;
            });
            connecting = true;
            state = "Connecting";
            // this 会作为 UserData 随连接事件返回，便于识别请求来源。
            channel.Connect(IPAddress.Loopback, server.Port, this);
        }

        private void Send()
        {
            responses.Clear();
            received = notices = 0;
            // Send 排队持有 Packet；发送端用 new，提交后保持内容不变。
            channel.Send(new NetworkTextPacket { Text = firstInput.text });
            channel.Send(new NetworkTextPacket { Text = secondInput.text });
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                state = "Connected. Ready to send.";
            }
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                connecting = false;
                state = "Connection closed";
            }
            else if (args is ErrorArgs error && ReferenceEquals(error.NetworkChannel, channel))
            {
                connecting = false;
                state = error.ErrorCode + "：" + error.SocketErrorCode;
                channel.Close();
            }
        }

        private void LateUpdate()
        {
            stateText.text = state;
            if (channel != null)
            {
                stateText.text += $"\nSend queue {channel.SendPacketCount} · Total sent {channel.SentPacketCount}" + $"\nReceive queue {channel.ReceivePacketCount} · Total received {channel.ReceivedPacketCount}" + $"\nText echoes {received} · Default handler {notices}";
            }

            responseText.text = responses.Count == 0 ? "Waiting for a response packet…" : string.Join("\n", responses.ConvertAll(text => text.Length == 0 ? "(empty text)" : text));
            bool ready = channel != null && channel.Connected && !connecting;
            sendButton.interactable = noticeButton.interactable = ready;
            connectButton.interactable = server != null && !connecting;
        }

        private void OnDestroy()
        {
            // 场景退出时解除监听并销毁通道，避免旧回调或 Socket 留在模块中。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ConnectedArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(ConnectedArgs.EventId, OnEvent);
                }

                if (events.Check(ClosedArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(ClosedArgs.EventId, OnEvent);
                }

                if (events.Check(ErrorArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(ErrorArgs.EventId, OnEvent);
                }
            }

            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null)
            {
                network.DestroyNetworkChannel("Example.Echo");
            }

            server?.Dispose();
        }
    }
}
