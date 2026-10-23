using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityQGameFramework.Examples
{
    /// <summary>本机下载输入，支持 Range、分段传输、404、超时和故意忽略 Range 的响应。</summary>
    public sealed class DownloadHttpServer : IDisposable
    {
        private readonly DownloadHttpListener listener;
        private long lastRange = -1;
        public long LastRange => Interlocked.Read(ref lastRange);
        public DownloadHttpServer(byte[] payload) => listener = new DownloadHttpListener((stream, stop) => Serve(stream, payload, stop));
        public string Url(string path) => "http://127.0.0.1:" + listener.Port + path;
        public void Dispose() => listener.Dispose();

        private async Task Serve(NetworkStream stream, byte[] payload, CancellationToken stop)
        {
            byte[] buffer = new byte[1024];
            var request = new StringBuilder();
            while (!request.ToString().Contains("\r\n\r\n"))
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, stop).ConfigureAwait(false);
                if (read == 0) return;
                request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (request.Length > 8192) throw new IOException("Example HTTP header exceeds 8 KiB.");
            }
            string[] lines = request.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);
            string path = lines[0].Split(' ')[1];
            long from = -1;
            foreach (string line in lines)
                if (line.StartsWith("Range: bytes=", StringComparison.OrdinalIgnoreCase))
                    from = long.Parse(line.Substring(13).Split('-')[0]);
            Interlocked.Exchange(ref lastRange, from);
            if (path == "/hold") await Task.Delay(15000, stop).ConfigureAwait(false);
            if (path == "/low" || path == "/high" || path == "/other") await Task.Delay(900, stop).ConfigureAwait(false);
            bool missing = path == "/missing";
            bool invalid = from >= payload.Length && path != "/ignore-range";
            bool partial = from >= 0 && !invalid && path != "/ignore-range" && !missing;
            byte[] body = missing ? Encoding.UTF8.GetBytes("Not found") : invalid ? new byte[0] : payload;
            int offset = partial ? (int)from : 0;
            string status = missing ? "404 Not Found" : invalid ? "416 Range Not Satisfiable" : partial ? "206 Partial Content" : "200 OK";
            string range = partial ? $"Content-Range: bytes {from}-{payload.Length - 1}/{payload.Length}\r\n" : "";
            byte[] header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\n"
                + $"Content-Length: {body.Length - offset}\r\n"
                + $"{range}Content-Type: application/octet-stream\r\n"
                + "Connection: close\r\n\r\n");
            await stream.WriteAsync(header, 0, header.Length, stop).ConfigureAwait(false);
            // /slow 分段发送，确保能观察到真实进度，并留出取消、续传的操作时间。
            int chunk = path == "/slow" ? 4096 : Math.Max(1, body.Length);
            while (offset < body.Length)
            {
                int count = Math.Min(chunk, body.Length - offset);
                await stream.WriteAsync(body, offset, count, stop).ConfigureAwait(false);
                offset += count;
                if (path == "/slow") await Task.Delay(50, stop).ConfigureAwait(false);
            }
        }
    }
}
