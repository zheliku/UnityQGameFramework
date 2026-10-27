using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 提供 JSON、404、延迟与回显响应的本机 HTTP 服务。
    /// </summary>
    public sealed class LocalHttpServer : IDisposable
    {
        private readonly LocalHttpListener listener;

        /// <summary>
        /// 向本机 HTTP 请求返回示例文本。
        /// </summary>
        /// <param name="response">请求对应的响应生成方法。</param>
        public LocalHttpServer(byte[] response)
        {
            listener = new LocalHttpListener((stream, stop) => Serve(stream, response, stop));
        }

        /// <summary>
        /// 取得本机测试服务器的请求地址。
        /// </summary>
        /// <param name="path">以斜杠开头的请求路径。</param>
        /// <returns>包含本机端口的 HTTP 地址。</returns>
        public string Url(string path)
        {
            return "http://127.0.0.1:" + listener.Port + path;
        }

        /// <summary>
        /// 停止后台服务并关闭测试连接。
        /// </summary>
        public void Dispose()
        {
            listener.Dispose();
        }

        private static async Task Serve(NetworkStream stream, byte[] response, CancellationToken stop)
        {
            // 逐字节读取小型请求头，保留后面可能紧接着到达的请求体，不把正文当成头丢掉。
            var headerBytes = new MemoryStream();
            byte[] one = new byte[1];
            int tail = 0;
            while (tail != 0x0D0A0D0A)
            {
                if (await stream.ReadAsync(one, 0, 1, stop).ConfigureAwait(false) == 0)
                {
                    return;
                }

                headerBytes.WriteByte(one[0]);
                tail = (tail << 8) | one[0];
                if (headerBytes.Length > 8192)
                {
                    throw new IOException("Example HTTP header exceeds 8 KiB.");
                }
            }

            string[] headers = Encoding.ASCII.GetString(headerBytes.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] first = headers[0].Split(' ');
            string method = first[0], path = first[1], contentType = "";
            int length = 0;
            foreach (string header in headers)
            {
                if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    length = int.Parse(header.Substring(15).Trim());
                }

                if (header.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = header.Substring(13).Trim();
                }
            }

            if (length < 0 || length > 65536)
            {
                throw new IOException("Example HTTP body exceeds 64 KiB.");
            }

            byte[] received = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int count = await stream.ReadAsync(received, offset, length - offset, stop).ConfigureAwait(false);
                if (count == 0)
                {
                    return;
                }

                offset += count;
            }

            if (path == "/hold")
            {
                await Task.Delay(15000, stop).ConfigureAwait(false);
            }

            if (path == "/echo-slow")
            {
                await Task.Delay(3000, stop).ConfigureAwait(false);
            }

            if (path == "/low" || path == "/high" || path == "/other")
            {
                await Task.Delay(1200, stop).ConfigureAwait(false);
            }

            bool missing = path == "/missing";
            byte[] body = missing ? Encoding.UTF8.GetBytes("Not found") : response;
            if (path == "/echo" || path == "/echo-slow")
            {
                // /echo-bytes 的二进制验收由下方原样响应，不经过字符串转换。
                string text = Encoding.UTF8.GetString(received);
                string field = contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ? Uri.UnescapeDataString(text.Substring(text.IndexOf('=') + 1).Replace("+", " ")) : text;
                body = Encoding.UTF8.GetBytes($"{method} {path}\nContent-Type: {contentType}\n收到 {received.Length} 字节\n正文：{text}\n解码内容：{field}");
            }

            if (path == "/echo-bytes")
            {
                body = received;
            }

            byte[] reply = Encoding.ASCII.GetBytes("HTTP/1.1 " + (missing ? "404 Not Found" : "200 OK") + "\r\nContent-Type: application/octet-stream\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(reply, 0, reply.Length, stop).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, stop).ConfigureAwait(false);
        }
    }
}
