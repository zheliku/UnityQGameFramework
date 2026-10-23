using System;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Network;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using ConnectedArgs = UnityQGameFramework.Runtime.NetworkConnectedEventArgs;
using ClosedArgs = UnityQGameFramework.Runtime.NetworkClosedEventArgs;
using ErrorArgs = UnityQGameFramework.Runtime.NetworkErrorEventArgs;

namespace UnityQGameFramework.Examples
{
    /// <summary>创建、连接、关闭与销毁各有不同结果；比较异步接收和同步接收。</summary>
    public sealed class ConnectNetworkExample : MonoBehaviour
    {
        [SerializeField] private Dropdown serviceMode, connectMode, queryMode;
        [SerializeField] private Button createButton, connectButton, closeButton, destroyButton, spareButton;
        [SerializeField] private Text stateText, queryText;
        private readonly System.Collections.Generic.List<INetworkChannel> channels = new System.Collections.Generic.List<INetworkChannel>();
        private bool connecting;
        private int connectedEvents, closedEvents;

        private NetworkEchoServer server;
        private INetworkChannel channel;
        private string state = "尚未连接";

        private async void Start()
        {
            try
            {
                await UniTask.NextFrame(destroyCancellationToken);
                server = new NetworkEchoServer();
                // Runtime 事件在主线程分发；先订阅，再建立连接。
                QGF.Event.Subscribe(ConnectedArgs.EventId, OnEvent);
                QGF.Event.Subscribe(ClosedArgs.EventId, OnEvent);
                QGF.Event.Subscribe(ErrorArgs.EventId, OnEvent);
                createButton.onClick.AddListener(Create);
                connectButton.onClick.AddListener(Connect);
                closeButton.onClick.AddListener(() =>
                {
                    channel.Close();
                    state = "连接已关闭，通道仍注册";
                });
                destroyButton.onClick.AddListener(() =>
                {
                    QGF.Network.DestroyNetworkChannel("Example.Main");
                    channel = null;
                    connecting = false;
                    state = "通道已移除";
                });
                spareButton.onClick.AddListener(() => QGF.Network.CreateNetworkChannel("Example.Spare", ServiceType.Tcp, new NetworkTextProtocol()));
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Create()
        {
            // 创建只是登记通道与协议，不会建立 Socket 连接。
            channel = QGF.Network.CreateNetworkChannel(
                "Example.Main",
                serviceMode.value == 0 ? ServiceType.Tcp : ServiceType.TcpWithSyncReceive,
                new NetworkTextProtocol());
            channel.HeartBeatInterval = 0;
            state = "通道已创建，尚未连接";
        }

        private void Connect()
        {
            connecting = true;
            state = "连接请求已提交";
            // 两个重载只差 UserData；它会在 Connected 事件中带回。
            if (connectMode.value == 0) channel.Connect(IPAddress.Loopback, server.Port);
            else channel.Connect(IPAddress.Loopback, server.Port, this);
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                connectedEvents++;
                string context = connected.UserData == null ? "未传入"
                    : ReferenceEquals(connected.UserData, this) ? "当前组件" : connected.UserData.GetType().Name;
                state = $"已收到 Connected · 上下文：{context}";
            }
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                connecting = false;
                closedEvents++;
                state = "已收到 Closed，仍可重新连接";
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
            if (server == null) return;
            bool exists = QGF.Network.HasNetworkChannel("Example.Main");
            string connection = channel == null ? "未创建" : channel.Connected ? "已连接" : "未连接";
            stateText.text = $"{state}\n注册通道 {QGF.Network.NetworkChannelCount} · 主通道：{connection}\n"
                + $"Connected {connectedEvents} 次 · Closed {closedEvents} 次\n"
                + (channel == null
                    ? string.Empty
                    : $"{channel.Name} · {channel.ServiceType} · {channel.AddressFamily}\n"
                        + $"Socket：{(channel.Socket != null ? "已创建" : "未创建")}");
            // 名称查询取得同一个实例；数组/List 查询也包含未连接的备用通道。
            channels.Clear();
            if (queryMode.value == 0)
            {
                var found = QGF.Network.GetNetworkChannel("Example.Main");
                if (found != null) channels.Add(found);
            }
            else if (queryMode.value == 1) channels.AddRange(QGF.Network.GetAllNetworkChannels());
            else QGF.Network.GetAllNetworkChannels(channels);
            queryText.text = "查询结果：" + string.Join(", ", channels.ConvertAll(item => item.Name));
            createButton.interactable = !exists;
            connectButton.interactable = exists && !connecting && !channel.Connected;
            closeButton.interactable = exists && (connecting || channel.Connected);
            destroyButton.interactable = exists;
            spareButton.interactable = !QGF.Network.HasNetworkChannel("Example.Spare");
            serviceMode.interactable = !exists;
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
            if (network != null)
            {
                network.DestroyNetworkChannel("Example.Main");
                network.DestroyNetworkChannel("Example.Spare");
            }
            server?.Dispose();
        }
    }
}
