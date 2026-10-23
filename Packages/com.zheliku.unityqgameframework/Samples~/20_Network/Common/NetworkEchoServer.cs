using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityQGameFramework.Examples
{
    /// <summary>只监听本机随机端口。可拆分回包、忽略心跳、发送错误响应或主动关闭。</summary>
    public sealed class NetworkEchoServer : IDisposable
    {
        private readonly NetworkLoopbackServer listener;
        private readonly Socket refused = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public int Port => listener.Port;
        public int RefusedPort => ((IPEndPoint)refused.LocalEndPoint).Port;
        public volatile bool ReplyHeartBeats = true;
        public volatile bool SplitResponses = true;
        public volatile int ResponseMode;
        public NetworkEchoServer()
        {
            refused.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // 占用但不监听，用于稳定复现连接被拒绝。
            listener = new NetworkLoopbackServer(Serve);
        }
        public void DisconnectClients() => listener.DisconnectClients();
        public void Dispose() { listener.Dispose(); refused.Dispose(); }

        private async Task Serve(NetworkStream stream, CancellationToken stop)
        {
            var header = new byte[4];
            while (await ReadExact(stream, header, stop).ConfigureAwait(false))
            {
                int length = BitConverter.ToInt32(header, 0);
                if (length < 4 || length > 65536) return;
                byte[] body = new byte[length];
                if (!await ReadExact(stream, body, stop).ConfigureAwait(false)) return;
                int id = BitConverter.ToInt32(body, 0);
                if (id == 2 && !ReplyHeartBeats) continue;
                if (ResponseMode == 4) return; // 正常关闭连接，向客户端发送 FIN。
                if (ResponseMode == 1) { header = BitConverter.GetBytes(-1); body = new byte[0]; }
                if (ResponseMode == 2)
                {
                    header = BitConverter.GetBytes(5);
                    body = new byte[] { 1, 0, 0, 0, 255 }; // 故意提供非法 UTF-8。
                }
                if (ResponseMode == 3)
                {
                    byte[] error = Encoding.UTF8.GetBytes("服务器拒绝这次业务请求");
                    body = new byte[4 + error.Length];
                    Buffer.BlockCopy(BitConverter.GetBytes(4), 0, body, 0, 4);
                    Buffer.BlockCopy(error, 0, body, 4, error.Length);
                    header = BitConverter.GetBytes(body.Length);
                }
                byte[] response = new byte[header.Length + body.Length];
                Buffer.BlockCopy(header, 0, response, 0, header.Length);
                Buffer.BlockCopy(body, 0, response, header.Length, body.Length);
                // 拆分长度头，迫使客户端跨多次读取恢复完整消息；正常模式则一次写完整包。
                int first = SplitResponses ? 1 : response.Length;
                await stream.WriteAsync(response, 0, first, stop).ConfigureAwait(false);
                if (first < response.Length)
                {
                    await Task.Delay(80, stop).ConfigureAwait(false);
                    await stream.WriteAsync(response, first, response.Length - first, stop).ConfigureAwait(false);
                }
                if (ResponseMode == 1 || ResponseMode == 2) return;
            }
        }

        private static async Task<bool> ReadExact(NetworkStream stream, byte[] bytes, CancellationToken stop)
        {
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = await stream.ReadAsync(bytes, offset, bytes.Length - offset, stop).ConfigureAwait(false);
                if (count == 0) return false;
                offset += count;
            }
            return true;
        }
    }
}
