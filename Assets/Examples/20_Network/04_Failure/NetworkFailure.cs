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
using CustomErrorArgs = UnityQGameFramework.Runtime.NetworkCustomErrorEventArgs;

namespace UnityQGameFramework.Examples
{
    /// <summary>比较连接错误、协议错误、业务错误和远端关闭；修正后重新连接同一通道。</summary>
    public sealed class NetworkFailure : MonoBehaviour
    {
        [SerializeField] private Dropdown failureMode, serviceMode;
        [SerializeField] private Button connectButton;
        [SerializeField] private Text stateText, detailText;
        private bool connecting;
        private int errors, customErrors, closes;
        private string detail = "选择响应后连接";

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
                QGF.Event.Subscribe(CustomErrorArgs.EventId, OnEvent);
                connectButton.onClick.AddListener(Connect);
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Connect()
        {
            // 改变接收实现时重建；同一种实现则重用通道并重新 Connect。
            var service = serviceMode.value == 0 ? ServiceType.Tcp : ServiceType.TcpWithSyncReceive;
            if (channel != null && channel.ServiceType != service)
            {
                QGF.Network.DestroyNetworkChannel("Example.Failure");
                channel = null;
            }
            if (channel == null)
            {
                channel = QGF.Network.CreateNetworkChannel("Example.Failure", service, new NetworkTextProtocol());
                channel.HeartBeatInterval = 0;
                channel.RegisterHandler(new NetworkTextHandler(text =>
                {
                    state = "已恢复正常回显";
                    detail = text;
                }));
            }
            else channel.Close();
            connecting = true;
            errors = customErrors = closes = 0;
            server.ResponseMode = failureMode.value == 0 ? 0 : failureMode.value;
            detail = "等待连接和响应";
            state = "正在连接";
            channel.Connect(IPAddress.Loopback, failureMode.value == 0 ? server.RefusedPort : server.Port, this);
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                state = "已连接，等待协议响应";
                channel.Send(new NetworkTextPacket { Text = "恢复后的业务消息" });
            }
            else if (args is ErrorArgs error && ReferenceEquals(error.NetworkChannel, channel))
            {
                connecting = false;
                errors++;
                state = "网络 / 协议错误";
                detail = error.ErrorCode + (error.SocketErrorCode == System.Net.Sockets.SocketError.Success ? "" : " · " + error.SocketErrorCode);
                channel.Close(); // 关闭出错的连接，等待用户重试。
            }
            else if (args is CustomErrorArgs custom && ReferenceEquals(custom.NetworkChannel, channel))
            {
                customErrors++;
                state = "业务错误，连接仍可用";
                detail = custom.CustomErrorData.ToString();
            }
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                if (connecting) return; // 重连前主动 Close 的通知可能仍在事件队列中。
                closes++;
                if (errors == 0 && customErrors == 0) state = "服务端关闭了连接";
            }
        }

        private void LateUpdate()
        {
            string connection = channel == null ? "未创建" : channel.Connected ? "已连接" : "已断开";
            stateText.text = state + $"\n连接：{connection} · 网络错误 {errors} · 业务错误 {customErrors} · 关闭 {closes}";
            detailText.text = detail;
            connectButton.interactable = server != null && !connecting;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ConnectedArgs.EventId, OnEvent)) events.Unsubscribe(ConnectedArgs.EventId, OnEvent);
                if (events.Check(ClosedArgs.EventId, OnEvent)) events.Unsubscribe(ClosedArgs.EventId, OnEvent);
                if (events.Check(ErrorArgs.EventId, OnEvent)) events.Unsubscribe(ErrorArgs.EventId, OnEvent);
                if (events.Check(CustomErrorArgs.EventId, OnEvent))
                    events.Unsubscribe(CustomErrorArgs.EventId, OnEvent);
            }
            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null) network.DestroyNetworkChannel("Example.Failure");
            server?.Dispose();
        }
    }
}
