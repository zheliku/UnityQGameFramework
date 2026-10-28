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
    /// <summary>
    /// 心跳发送、响应和丢失计数，以及按丢失次数关闭连接。
    /// </summary>
    public sealed class NetworkHeartbeat : MonoBehaviour
    {
        [SerializeField]
        private Button connectButton;
        [SerializeField]
        private Slider intervalSlider, timerSlider;
        [SerializeField]
        private Toggle replyToggle;
        [SerializeField]
        private Text stateText, counterText;
        private NetworkTextProtocol protocol;
        private bool connecting;
        private int misses;
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
            QGF.Event.Subscribe(MissedHeartbeatArgs.EventId, OnEvent);
            connectButton.onClick.AddListener(Connect);
            intervalSlider.onValueChanged.AddListener(value =>
            {
                if (channel != null)
                {
                    channel.HeartBeatInterval = value;
                }
            });
            replyToggle.onValueChanged.AddListener(value => server.ReplyHeartBeats = value);
        }

        private void Connect()
        {
            if (channel != null)
            {
                channel.Close();
            }
            else
            {
                protocol = new NetworkTextProtocol();
                channel = QGF.Network.CreateNetworkChannel("Example.Heartbeat", ServiceType.Tcp, protocol);
            }

            channel.HeartBeatInterval = intervalSlider.value;
            channel.ResetHeartBeatElapseSecondsWhenReceivePacket = true;
            misses = 0;
            connecting = true;
            state = "Connecting";
            channel.Connect(IPAddress.Loopback, server.Port, this);
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is ConnectedArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                state = "Heartbeat connection established";
            }
            else if (args is MissedHeartbeatArgs missed && ReferenceEquals(missed.NetworkChannel, channel))
            {
                misses = missed.MissCount;
                state = "No heartbeat response received";
                // 框架报告丢失次数；连续三次未响应时由业务关闭连接。
                if (misses >= 3)
                {
                    channel.Close();
                    state = "The application closed the unresponsive connection.";
                }
            }
            else if (args is ClosedArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                connecting = false;
            }
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
            string connection = channel.Connected ? "Connected" : "Disconnected";
            counterText.text = $"Interval {channel.HeartBeatInterval:F2} s · Elapsed {channel.HeartBeatElapseSeconds:F2} s\n" + $"Heartbeats sent {protocol.HeartBeatsSent} · Replies received {protocol.HeartBeatsReceived}\n" + $"Current missed heartbeats {channel.MissHeartBeatCount} · Last notification {misses}\nConnection: {connection}";
            connectButton.interactable = !connecting && !channel.Connected;
        }

        private void OnDestroy()
        {
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

                if (events.Check(MissedHeartbeatArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(MissedHeartbeatArgs.EventId, OnEvent);
                }
            }

            var network = GameEntry.GetComponent<NetworkComponent>();
            if (network != null)
            {
                network.DestroyNetworkChannel("Example.Heartbeat");
            }

            server?.Dispose();
        }
    }
}
