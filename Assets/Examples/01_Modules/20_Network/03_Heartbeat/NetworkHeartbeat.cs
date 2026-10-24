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
using MissedHeartbeatArgs = UnityQGameFramework.Runtime.NetworkMissHeartBeatEventArgs;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>心跳发送、响应和丢失计数，以及按丢失次数关闭连接。</summary>
    public sealed class NetworkHeartbeat : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private Slider intervalSlider, timerSlider;
        [SerializeField] private Toggle replyToggle;
        [SerializeField] private Text stateText, counterText;
        private NetworkTextProtocol protocol;
        private bool connecting;
        private int misses;

        private NetworkEchoServer server;
        private INetworkChannel channel;
        private string state = "尚未连接";

        private void Start()
        {
            server = new NetworkEchoServer();
            // Runtime 事件在主线程分发；先订阅，再建立连接。
            QGF.Event.Subscribe(ConnectedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ClosedArgs.EventId, OnEvent);
            QGF.Event.Subscribe(ErrorArgs.EventId, OnEvent);
            QGF.Event.Subscribe(MissedHeartbeatArgs.EventId, OnEvent);
            connectButton.onClick.AddListener(Connect);
            intervalSlider.onValueChanged.AddListener(value =>
            {
                if (channel != null) channel.HeartBeatInterval = value;
            });
            replyToggle.onValueChanged.AddListener(value => server.ReplyHeartBeats = value);
        }

        private void Connect()
        {
            if (channel != null) channel.Close();
            else
            {
                protocol = new NetworkTextProtocol();
                channel = QGF.Network.CreateNetworkChannel("Example.Heartbeat", ServiceType.Tcp, protocol);
            }
            channel.HeartBeatInterval = intervalSlider.value;
            channel.ResetHeartBeatElapseSecondsWhenReceivePacket = true;
            misses = 0;
            connecting = true;
            state = "正在连接";
            channel.Connect(IPAddress.Loopback, server.Port, this);
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                state = "心跳连接已建立";
            }
            else if (args is MissedHeartbeatArgs missed && ReferenceEquals(missed.NetworkChannel, channel))
            {
                misses = missed.MissCount;
                state = "未收到心跳响应";
                // 框架报告丢失次数；连续三次未响应时由业务关闭连接。
                if (misses >= 3)
                {
                    channel.Close();
                    state = "业务已关闭失去响应的连接";
                }
            }
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel)) connecting = false;
            else if (args is ErrorArgs error && ReferenceEquals(error.NetworkChannel, channel))
            {
                connecting = false;
                state = error.ErrorCode.ToString();
                channel.Close();
            }
        }

        private void LateUpdate()
        {
            stateText.text = state;
            if (protocol == null)
            {
                connectButton.interactable = server != null;
                return;
            }
            timerSlider.value = channel.HeartBeatInterval > 0 ? channel.HeartBeatElapseSeconds / channel.HeartBeatInterval : 0;
            string connection = channel.Connected ? "已连接" : "已断开";
            counterText.text = $"间隔 {channel.HeartBeatInterval:F2} 秒 · 已等 {channel.HeartBeatElapseSeconds:F2} 秒\n"
                + $"协议发送 {protocol.HeartBeatsSent} · 收到响应 {protocol.HeartBeatsReceived}\n"
                + $"当前丢失计数 {channel.MissHeartBeatCount} · 最近通知 {misses}\n连接：{connection}";
            connectButton.interactable = !connecting && !channel.Connected;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ConnectedArgs.EventId, OnEvent)) events.Unsubscribe(ConnectedArgs.EventId, OnEvent);
                if (events.Check(ClosedArgs.EventId, OnEvent)) events.Unsubscribe(ClosedArgs.EventId, OnEvent);
                if (events.Check(ErrorArgs.EventId, OnEvent)) events.Unsubscribe(ErrorArgs.EventId, OnEvent);
                if (events.Check(MissedHeartbeatArgs.EventId, OnEvent))
                    events.Unsubscribe(MissedHeartbeatArgs.EventId, OnEvent);
            }
            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null) network.DestroyNetworkChannel("Example.Heartbeat");
            server?.Dispose();
        }
    }
}
