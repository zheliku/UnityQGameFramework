//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        /// <summary>
        /// 网络频道基类。
        /// </summary>
        private abstract class NetworkChannelBase : INetworkChannel, IDisposable
        {
            private const float DefaultHeartBeatInterval = 30f;

            /// <summary>
            /// 等待发送的消息包队列。
            /// </summary>
            protected readonly Queue<Packet> m_SendPacketPool;

            /// <summary>
            /// 用于派发接收消息包的事件池。
            /// </summary>
            protected readonly EventPool<Packet> m_ReceivePacketPool;

            /// <summary>
            /// 网络频道辅助器。
            /// </summary>
            protected readonly INetworkChannelHelper m_NetworkChannelHelper;

            /// <summary>
            /// 套接字地址族。
            /// </summary>
            protected AddressFamily m_AddressFamily;

            /// <summary>
            /// 收到消息包后是否重置心跳计时。
            /// </summary>
            protected bool m_ResetHeartBeatElapseSecondsWhenReceivePacket;

            /// <summary>
            /// 心跳发送间隔，以秒为单位。
            /// </summary>
            protected float m_HeartBeatInterval;

            /// <summary>
            /// 网络频道套接字。
            /// </summary>
            protected Socket m_Socket;

            /// <summary>
            /// 消息包发送状态。
            /// </summary>
            protected readonly SendState m_SendState;

            /// <summary>
            /// 消息包接收状态。
            /// </summary>
            protected readonly ReceiveState m_ReceiveState;

            /// <summary>
            /// 心跳状态。
            /// </summary>
            protected readonly HeartBeatState m_HeartBeatState;

            /// <summary>
            /// 已发送消息包数量。
            /// </summary>
            protected int m_SentPacketCount;

            /// <summary>
            /// 已接收消息包数量。
            /// </summary>
            protected int m_ReceivedPacketCount;

            /// <summary>
            /// 网络频道是否处于活动状态。
            /// </summary>
            protected bool m_Active;
            private bool m_Disposed;

            /// <summary>
            /// 网络连接成功回调。
            /// </summary>
            public FrameworkAction<NetworkChannelBase, object> NetworkChannelConnected;

            /// <summary>
            /// 网络连接关闭回调。
            /// </summary>
            public FrameworkAction<NetworkChannelBase> NetworkChannelClosed;

            /// <summary>
            /// 网络心跳丢失回调。
            /// </summary>
            public FrameworkAction<NetworkChannelBase, int> NetworkChannelMissHeartBeat;

            /// <summary>
            /// 网络错误回调。
            /// </summary>
            public FrameworkAction<NetworkChannelBase, NetworkErrorCode, SocketError, string> NetworkChannelError;

            /// <summary>
            /// 网络自定义错误回调。
            /// </summary>
            public FrameworkAction<NetworkChannelBase, object> NetworkChannelCustomError;

            /// <summary>
            /// 初始化网络频道基类的新实例。
            /// </summary>
            /// <param name="name">网络频道名称。</param>
            /// <param name="networkChannelHelper">网络频道辅助器。</param>
            public NetworkChannelBase(string name, INetworkChannelHelper networkChannelHelper)
            {
                Name = name ?? string.Empty;
                m_SendPacketPool = new Queue<Packet>();
                m_ReceivePacketPool = new EventPool<Packet>(EventPoolMode.Default);
                m_NetworkChannelHelper = networkChannelHelper;
                m_AddressFamily = AddressFamily.Unknown;
                m_ResetHeartBeatElapseSecondsWhenReceivePacket = false;
                m_HeartBeatInterval = DefaultHeartBeatInterval;
                m_Socket = null;
                m_SendState = new SendState();
                m_ReceiveState = new ReceiveState();
                m_HeartBeatState = new HeartBeatState();
                m_SentPacketCount = 0;
                m_ReceivedPacketCount = 0;
                m_Active = false;
                m_Disposed = false;
                NetworkChannelConnected = null;
                NetworkChannelClosed = null;
                NetworkChannelMissHeartBeat = null;
                NetworkChannelError = null;
                NetworkChannelCustomError = null;
                networkChannelHelper.Initialize(this);
            }

            /// <summary>
            /// 获取网络频道名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取网络频道所使用的 Socket。
            /// </summary>
            public Socket Socket
            {
                get
                {
                    return m_Socket;
                }
            }

            /// <summary>
            /// 获取是否已连接。
            /// </summary>
            public bool Connected
            {
                get
                {
                    if (m_Socket != null)
                    {
                        return m_Socket.Connected;
                    }

                    return false;
                }
            }

            /// <summary>
            /// 获取网络服务类型。
            /// </summary>
            public abstract ServiceType ServiceType { get; }

            /// <summary>
            /// 获取网络地址类型。
            /// </summary>
            public AddressFamily AddressFamily
            {
                get
                {
                    return m_AddressFamily;
                }
            }

            /// <summary>
            /// 获取要发送的消息包数量。
            /// </summary>
            public int SendPacketCount
            {
                get
                {
                    return m_SendPacketPool.Count;
                }
            }

            /// <summary>
            /// 获取累计完成 Socket 发送的消息数，不是服务端业务确认数。一次发送可包含多条消息。
            /// </summary>
            public int SentPacketCount
            {
                get
                {
                    return m_SentPacketCount;
                }
            }

            /// <summary>
            /// 获取已接收未处理的消息包数量。
            /// </summary>
            public int ReceivePacketCount
            {
                get
                {
                    return m_ReceivePacketPool.EventCount;
                }
            }

            /// <summary>
            /// 获取累计已接收的消息包数量。
            /// </summary>
            public int ReceivedPacketCount
            {
                get
                {
                    return m_ReceivedPacketCount;
                }
            }

            /// <summary>
            /// 获取或设置当收到消息包时是否重置心跳流逝时间。
            /// </summary>
            public bool ResetHeartBeatElapseSecondsWhenReceivePacket
            {
                get
                {
                    return m_ResetHeartBeatElapseSecondsWhenReceivePacket;
                }

                set
                {
                    m_ResetHeartBeatElapseSecondsWhenReceivePacket = value;
                }
            }

            /// <summary>
            /// 获取丢失心跳的次数。
            /// </summary>
            public int MissHeartBeatCount
            {
                get
                {
                    return m_HeartBeatState.MissHeartBeatCount;
                }
            }

            /// <summary>
            /// 获取或设置心跳间隔，以秒为单位；小于等于零时关闭计时。协议仍须实现发送和回复。
            /// </summary>
            public float HeartBeatInterval
            {
                get
                {
                    return m_HeartBeatInterval;
                }

                set
                {
                    m_HeartBeatInterval = value;
                }
            }

            /// <summary>
            /// 获取心跳等待时长，以秒为单位。
            /// </summary>
            public float HeartBeatElapseSeconds
            {
                get
                {
                    return m_HeartBeatState.HeartBeatElapseSeconds;
                }
            }

            /// <summary>
            /// 网络频道轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public virtual void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (m_Socket == null || !m_Active)
                {
                    return;
                }

                ProcessSend();
                ProcessReceive();
                if (m_Socket == null || !m_Active)
                {
                    return;
                }

                m_ReceivePacketPool.Update(elapseSeconds, realElapseSeconds);
                if (m_HeartBeatInterval > 0f)
                {
                    bool sendHeartBeat = false;
                    int missHeartBeatCount = 0;
                    lock (m_HeartBeatState)
                    {
                        if (m_Socket == null || !m_Active)
                        {
                            return;
                        }

                        m_HeartBeatState.HeartBeatElapseSeconds += realElapseSeconds;
                        if (m_HeartBeatState.HeartBeatElapseSeconds >= m_HeartBeatInterval)
                        {
                            sendHeartBeat = true;
                            missHeartBeatCount = m_HeartBeatState.MissHeartBeatCount;
                            m_HeartBeatState.HeartBeatElapseSeconds = 0f;
                            m_HeartBeatState.MissHeartBeatCount++;
                        }
                    }

                    if (sendHeartBeat && m_NetworkChannelHelper.SendHeartBeat())
                    {
                        if (missHeartBeatCount > 0 && NetworkChannelMissHeartBeat != null)
                        {
                            NetworkChannelMissHeartBeat(this, missHeartBeatCount);
                        }
                    }
                }
            }

            /// <summary>
            /// 关闭网络频道。
            /// </summary>
            public virtual void Shutdown()
            {
                Close();
                m_ReceivePacketPool.Shutdown();
                m_NetworkChannelHelper.Shutdown();
            }

            /// <summary>
            /// 按 Handler.Id 注册接收消息处理函数。处理在主线程更新中执行，不保留接收 Packet 引用。
            /// </summary>
            /// <param name="handler">要注册的网络消息包处理函数。</param>
            public void RegisterHandler(IPacketHandler handler)
            {
                if (handler == null)
                {
                    throw new FrameworkException("Packet handler is invalid.");
                }

                m_ReceivePacketPool.Subscribe(handler.Id, handler.Handle);
            }

            /// <summary>
            /// 设置未注册专用 Id 时使用的默认处理函数。接收 Packet 在分发后由框架归还。
            /// </summary>
            /// <param name="handler">要设置的默认事件处理函数。</param>
            public void SetDefaultHandler(EventHandler<Packet> handler)
            {
                m_ReceivePacketPool.SetDefaultHandler(handler);
            }

            /// <summary>
            /// 连接到远程主机。
            /// </summary>
            /// <param name="ipAddress">远程主机的 IP 地址。</param>
            /// <param name="port">远程主机的端口号。</param>
            public void Connect(IPAddress ipAddress, int port)
            {
                Connect(ipAddress, port, null);
            }

            /// <summary>
            /// 连接到远程主机。
            /// </summary>
            /// <param name="ipAddress">远程主机的 IP 地址。</param>
            /// <param name="port">远程主机的端口号。</param>
            /// <param name="userData">用户自定义数据。</param>
            public virtual void Connect(IPAddress ipAddress, int port, object userData)
            {
                if (m_Socket != null)
                {
                    Close();
                    m_Socket = null;
                }

                switch (ipAddress.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        m_AddressFamily = AddressFamily.IPv4;
                        break;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        m_AddressFamily = AddressFamily.IPv6;
                        break;
                    default:
                        string errorMessage = Utility.Text.Format("Not supported address family '{0}'.", ipAddress.AddressFamily);
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.AddressFamilyError, SocketError.Success, errorMessage);
                            return;
                        }

                        throw new FrameworkException(errorMessage);
                }

                m_SendState.Reset();
                m_ReceiveState.PrepareForPacketHeader(m_NetworkChannelHelper.PacketHeaderLength);
            }

            /// <summary>
            /// 关闭 Socket 并清空队列与计数。通道仍注册在管理器中，可再次 Connect。
            /// </summary>
            public void Close()
            {
                lock (this)
                {
                    if (m_Socket == null)
                    {
                        return;
                    }

                    m_Active = false;
                    try
                    {
                        m_Socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        m_Socket.Close();
                        m_Socket = null;
                        if (NetworkChannelClosed != null)
                        {
                            NetworkChannelClosed(this);
                        }
                    }

                    m_SentPacketCount = 0;
                    m_ReceivedPacketCount = 0;
                    lock (m_SendPacketPool)
                    {
                        m_SendPacketPool.Clear();
                    }

                    m_ReceivePacketPool.Clear();
                    lock (m_HeartBeatState)
                    {
                        m_HeartBeatState.Reset(true);
                    }
                }
            }

            /// <summary>
            /// 将消息包加入发送队列。发送端对象不由框架自动归还，入队后不要立即 Clear 或 Release。
            /// </summary>
            /// <typeparam name="T">消息包类型。</typeparam>
            /// <param name="packet">要发送的消息包。</param>
            public void Send<T>(T packet)
                where T : Packet
            {
                if (m_Socket == null)
                {
                    string errorMessage = "You must connect first.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new FrameworkException(errorMessage);
                }

                if (!m_Active)
                {
                    string errorMessage = "Socket is not active.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new FrameworkException(errorMessage);
                }

                if (packet == null)
                {
                    string errorMessage = "Packet is invalid.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new FrameworkException(errorMessage);
                }

                lock (m_SendPacketPool)
                {
                    m_SendPacketPool.Enqueue(packet);
                }
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            /// <param name="disposing">释放资源标记。</param>
            private void Dispose(bool disposing)
            {
                if (m_Disposed)
                {
                    return;
                }

                if (disposing)
                {
                    Close();
                    m_SendState.Dispose();
                    m_ReceiveState.Dispose();
                }

                m_Disposed = true;
            }

            /// <summary>
            /// 将等待发送的消息包序列化到发送缓冲区。
            /// </summary>
            /// <returns>是否准备好了可发送的数据。</returns>
            protected virtual bool ProcessSend()
            {
                if (m_SendState.Stream.Length > 0 || m_SendPacketPool.Count <= 0)
                {
                    return false;
                }

                while (m_SendPacketPool.Count > 0)
                {
                    Packet packet = null;
                    lock (m_SendPacketPool)
                    {
                        packet = m_SendPacketPool.Dequeue();
                    }

                    bool serializeResult = false;
                    try
                    {
                        serializeResult = m_NetworkChannelHelper.Serialize(packet, m_SendState.Stream);
                    }
                    catch (Exception exception)
                    {
                        m_Active = false;
                        if (NetworkChannelError != null)
                        {
                            SocketException socketException = exception as SocketException;
                            NetworkChannelError(this, NetworkErrorCode.SerializeError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                            return false;
                        }

                        throw;
                    }

                    if (!serializeResult)
                    {
                        string errorMessage = "Serialized packet failure.";
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.SerializeError, SocketError.Success, errorMessage);
                            return false;
                        }

                        throw new FrameworkException(errorMessage);
                    }

                    m_SendState.PacketCount++;
                }

                m_SendState.Stream.Position = 0L;
                return true;
            }

            /// <summary>
            /// 处理网络接收。
            /// </summary>
            protected virtual void ProcessReceive()
            {
            }

            /// <summary>
            /// 解析消息包头并准备接收消息包体。
            /// </summary>
            /// <returns>是否成功处理消息包头。</returns>
            protected virtual bool ProcessPacketHeader()
            {
                try
                {
                    object customErrorData = null;
                    IPacketHeader packetHeader = m_NetworkChannelHelper.DeserializePacketHeader(m_ReceiveState.Stream, out customErrorData);
                    if (customErrorData != null && NetworkChannelCustomError != null)
                    {
                        NetworkChannelCustomError(this, customErrorData);
                    }

                    if (packetHeader == null)
                    {
                        string errorMessage = "Packet header is invalid.";
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.DeserializePacketHeaderError, SocketError.Success, errorMessage);
                            return false;
                        }

                        throw new FrameworkException(errorMessage);
                    }

                    m_ReceiveState.PrepareForPacket(packetHeader);
                    if (packetHeader.PacketLength <= 0)
                    {
                        bool processSuccess = ProcessPacket();
                        m_ReceivedPacketCount++;
                        return processSuccess;
                    }
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        SocketException socketException = exception as SocketException;
                        NetworkChannelError(this, NetworkErrorCode.DeserializePacketHeaderError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                        return false;
                    }

                    throw;
                }

                return true;
            }

            /// <summary>
            /// 反序列化并派发消息包。
            /// </summary>
            /// <returns>是否成功处理消息包。</returns>
            protected virtual bool ProcessPacket()
            {
                lock (m_HeartBeatState)
                {
                    m_HeartBeatState.Reset(m_ResetHeartBeatElapseSecondsWhenReceivePacket);
                }

                try
                {
                    object customErrorData = null;
                    Packet packet = m_NetworkChannelHelper.DeserializePacket(m_ReceiveState.PacketHeader, m_ReceiveState.Stream, out customErrorData);
                    if (customErrorData != null && NetworkChannelCustomError != null)
                    {
                        NetworkChannelCustomError(this, customErrorData);
                    }

                    if (packet != null)
                    {
                        m_ReceivePacketPool.Fire(this, packet);
                    }

                    m_ReceiveState.PrepareForPacketHeader(m_NetworkChannelHelper.PacketHeaderLength);
                }
                catch (Exception exception)
                {
                    m_Active = false;
                    if (NetworkChannelError != null)
                    {
                        SocketException socketException = exception as SocketException;
                        NetworkChannelError(this, NetworkErrorCode.DeserializePacketError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                        return false;
                    }

                    throw;
                }

                return true;
            }
        }
    }
}
