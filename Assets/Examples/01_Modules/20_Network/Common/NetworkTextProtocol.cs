using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Network;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 消息 Id：1 文本，2 心跳，3 通知，4 业务错误。
    /// </summary>
    public sealed class NetworkTextPacket : Packet
    {
        /// <summary>
        /// 获取或设置消息编号。
        /// </summary>
        public int MessageId { get; set; } = 1;

        /// <summary>
        /// 获取当前标识。
        /// </summary>
        public override int Id
        {
            get
            {
                return MessageId;
            }
        }

        /// <summary>
        /// 获取或设置消息文本。
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 重置数据，供引用池复用。
        /// </summary>
        public override void Clear()
        {
            MessageId = 1;
            Text = null;
        }
    }

    /// <summary>
    /// 只处理 Id 1。接收 Packet 由框架归还，业务只取出文本。
    /// </summary>
    public sealed class NetworkTextHandler : IPacketHandler
    {
        private readonly Action<string> receive;

        /// <summary>
        /// 获取当前标识。
        /// </summary>
        public int Id
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 将文本消息交给业务回调。
        /// </summary>
        /// <param name="receive">接收消息文本的回调。</param>
        public NetworkTextHandler(Action<string> receive)
        {
            this.receive = receive;
        }

        /// <summary>
        /// 将接收包的文本交给业务回调。
        /// </summary>
        /// <param name="sender">所属网络通道。</param>
        /// <param name="packet">框架派发的接收包，由框架归还。</param>
        public void Handle(object sender, Packet packet)
        {
            receive(((NetworkTextPacket)packet).Text);
        }
    }

    /// <summary>
    /// 4 字节小端包体长度 + 4 字节消息 Id + UTF-8 正文。TCP 读取次数与消息数无关。
    /// </summary>
    public sealed class NetworkTextProtocol : INetworkChannelHelper
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private INetworkChannel channel;
        private int heartBeatsReceived;

        /// <summary>
        /// 获取已发送的心跳数量。
        /// </summary>
        public int HeartBeatsSent { get; private set; }

        /// <summary>
        /// 获取已收到的心跳数量。
        /// </summary>
        public int HeartBeatsReceived
        {
            get
            {
                return Volatile.Read(ref heartBeatsReceived);
            }
        }

        /// <summary>
        /// 保存包体长度的网络消息头。
        /// </summary>
        private sealed class Header : IPacketHeader
        {
            /// <summary>
            /// 获取消息包体的字节数。
            /// </summary>
            public int PacketLength { get; }

            /// <summary>
            /// 保存包体长度的网络消息头。
            /// </summary>
            /// <param name="length">消息包体的字节数。</param>
            public Header(int length)
            {
                PacketLength = length;
            }
        }

        /// <summary>
        /// 获取固定消息头字节数。
        /// </summary>
        public int PacketHeaderLength
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// 保存所属网络通道。
        /// </summary>
        /// <param name="owner">使用此协议的通道。</param>
        public void Initialize(INetworkChannel owner)
        {
            channel = owner;
        }

        /// <summary>
        /// 清理所属通道引用。
        /// </summary>
        public void Shutdown()
        {
            channel = null;
        }

        /// <summary>
        /// 连接前清零心跳统计。
        /// </summary>
        public void PrepareForConnecting()
        {
            HeartBeatsSent = 0;
            Interlocked.Exchange(ref heartBeatsReceived, 0);
        }

        /// <summary>
        /// 发送协议心跳并增加计数。
        /// </summary>
        /// <returns>心跳加入发送队列时返回真。</returns>
        public bool SendHeartBeat()
        {
            // 发送协议心跳，收到服务端回复后重置丢失计数。
            channel.Send(new NetworkTextPacket { MessageId = 2 });
            HeartBeatsSent++;
            return true;
        }

        /// <summary>
        /// 写入包体长度、消息编号和 UTF-8 正文。
        /// </summary>
        /// <typeparam name="T">发送消息的类型。</typeparam>
        /// <param name="packet">发送消息。</param>
        /// <param name="destination">发送目标流，保持打开。</param>
        /// <returns>写入成功时返回真，非法消息抛出异常。</returns>
        public bool Serialize<T>(T packet, Stream destination)
            where T : Packet
        {
            if (!(packet is NetworkTextPacket text))
            {
                throw new InvalidDataException("Unknown message type.");
            }

            byte[] bytes = Utf8.GetBytes(text.Text ?? string.Empty);
            if (bytes.Length > 65532)
            {
                throw new InvalidDataException("The message exceeds 65532 bytes.");
            }

            using (var writer = new BinaryWriter(destination, Utf8, true))
            {
                writer.Write(bytes.Length + 4); // 包体长度不包含这个长度字段本身。
                writer.Write(text.Id);
                writer.Write(bytes);
            }

            // 发送 Packet 由调用方管理；本例用 new，序列化期间保持内容不变。
            return true;
        }

        /// <summary>
        /// 读取包体长度。
        /// </summary>
        /// <param name="source">消息头输入流。</param>
        /// <param name="customErrorData">业务错误数据，此处为空。</param>
        /// <returns>保存包体长度的消息头。</returns>
        public IPacketHeader DeserializePacketHeader(Stream source, out object customErrorData)
        {
            customErrorData = null;
            using (var reader = new BinaryReader(source, Utf8, true))
            {
                int length = reader.ReadInt32();
                if (length < 4 || length > 65536)
                {
                    throw new InvalidDataException("Invalid packet body length.");
                }

                return new Header(length);
            }
        }

        /// <summary>
        /// 解析文本消息并分离心跳和业务错误。
        /// </summary>
        /// <param name="header">消息头。</param>
        /// <param name="source">包体输入流。</param>
        /// <param name="customErrorData">业务错误消息，正常消息或心跳时为空。</param>
        /// <returns>借出的接收包，心跳或业务错误返回空。</returns>
        public Packet DeserializePacket(IPacketHeader header, Stream source, out object customErrorData)
        {
            customErrorData = null;
            using (var reader = new BinaryReader(source, Utf8, true))
            {
                int id = reader.ReadInt32();
                byte[] bytes = reader.ReadBytes(header.PacketLength - 4);
                if (bytes.Length != header.PacketLength - 4)
                {
                    throw new EndOfStreamException();
                }

                string text = Utf8.GetString(bytes);
                if (id == 2)
                {
                    Interlocked.Increment(ref heartBeatsReceived);
                    return null;
                }

                if (id == 4)
                {
                    customErrorData = text;
                    return null;
                } // 协议有效，转为业务错误事件。

                if (id != 1 && id != 3)
                {
                    throw new InvalidDataException("Unknown message ID.");
                }

                // 解析成功后才借出接收 Packet；通道在主线程分发后归还它。
                var packet = ReferencePool.Acquire<NetworkTextPacket>();
                packet.MessageId = id;
                packet.Text = text;
                return packet;
            }
        }
    }
}
