using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Network;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>消息 Id：1 文本，2 心跳，3 通知，4 业务错误。</summary>
    public sealed class NetworkTextPacket : Packet
    {
        public int MessageId { get; set; } = 1;
        public override int Id => MessageId;
        public string Text { get; set; }
        public override void Clear() { MessageId = 1; Text = null; }
    }

    /// <summary>只处理 Id 1。接收 Packet 由框架归还，业务只取出文本。</summary>
    public sealed class NetworkTextHandler : IPacketHandler
    {
        private readonly Action<string> receive;
        public int Id => 1;
        public NetworkTextHandler(Action<string> receive) => this.receive = receive;
        public void Handle(object sender, Packet packet) => receive(((NetworkTextPacket)packet).Text);
    }

    /// <summary>4 字节小端包体长度 + 4 字节消息 Id + UTF-8 正文。TCP 读取次数与消息数无关。</summary>
    public sealed class NetworkTextProtocol : INetworkChannelHelper
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private INetworkChannel channel;
        private int heartBeatsReceived;
        public int HeartBeatsSent { get; private set; }
        public int HeartBeatsReceived => Volatile.Read(ref heartBeatsReceived);
        private sealed class Header : IPacketHeader
        {
            public int PacketLength { get; }
            public Header(int length) => PacketLength = length;
        }
        public int PacketHeaderLength => 4;
        public void Initialize(INetworkChannel owner) => channel = owner;
        public void Shutdown() => channel = null;
        public void PrepareForConnecting() { HeartBeatsSent = 0; Interlocked.Exchange(ref heartBeatsReceived, 0); }

        public bool SendHeartBeat()
        {
            // 发送协议心跳，收到服务端回复后重置丢失计数。
            channel.Send(new NetworkTextPacket { MessageId = 2 });
            HeartBeatsSent++;
            return true;
        }

        public bool Serialize<T>(T packet, Stream destination) where T : Packet
        {
            if (!(packet is NetworkTextPacket text)) throw new InvalidDataException("未知消息类型。");
            byte[] bytes = Utf8.GetBytes(text.Text ?? string.Empty);
            if (bytes.Length > 65532) throw new InvalidDataException("消息超过 65532 字节。");
            using (var writer = new BinaryWriter(destination, Utf8, true))
            {
                writer.Write(bytes.Length + 4); // 包体长度不包含这个长度字段本身。
                writer.Write(text.Id);
                writer.Write(bytes);
            }
            // 发送 Packet 由调用方管理；本例用 new，序列化期间保持内容不变。
            return true;
        }

        public IPacketHeader DeserializePacketHeader(Stream source, out object customErrorData)
        {
            customErrorData = null;
            using (var reader = new BinaryReader(source, Utf8, true))
            {
                int length = reader.ReadInt32();
                if (length < 4 || length > 65536) throw new InvalidDataException("非法包体长度。");
                return new Header(length);
            }
        }

        public Packet DeserializePacket(IPacketHeader header, Stream source, out object customErrorData)
        {
            customErrorData = null;
            using (var reader = new BinaryReader(source, Utf8, true))
            {
                int id = reader.ReadInt32();
                byte[] bytes = reader.ReadBytes(header.PacketLength - 4);
                if (bytes.Length != header.PacketLength - 4) throw new EndOfStreamException();
                string text = Utf8.GetString(bytes);
                if (id == 2) { Interlocked.Increment(ref heartBeatsReceived); return null; }
                if (id == 4) { customErrorData = text; return null; } // 协议有效，转为业务错误事件。
                if (id != 1 && id != 3) throw new InvalidDataException("未知消息 Id。");
                // 解析成功后才借出接收 Packet；通道在主线程分发后归还它。
                var packet = ReferencePool.Acquire<NetworkTextPacket>();
                packet.MessageId = id;
                packet.Text = text;
                return packet;
            }
        }
    }
}
