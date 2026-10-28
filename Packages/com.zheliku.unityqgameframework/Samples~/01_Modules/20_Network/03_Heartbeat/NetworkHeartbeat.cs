using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

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
            QGF.Event.Subscribe(NetworkConnectedEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(NetworkClosedEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(NetworkErrorEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(NetworkMissHeartBeatEventArgs.EventId, OnEvent);
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
            if (args is NetworkConnectedEventArgs connected && ReferenceEquals(connected.NetworkChannel, channel))
            {
                connecting = false;
                state = "Heartbeat connection established";
            }
            else if (args is NetworkMissHeartBeatEventArgs missed && ReferenceEquals(missed.NetworkChannel, channel))
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
            else if (args is NetworkClosedEventArgs closed && ReferenceEquals(closed.NetworkChannel, channel))
            {
                connecting = false;
            }
            else if (args is NetworkErrorEventArgs error && ReferenceEquals(error.NetworkChannel, channel))
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

                if (events.Check(NetworkMissHeartBeatEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(NetworkMissHeartBeatEventArgs.EventId, OnEvent);
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
