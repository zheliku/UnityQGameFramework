using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YooAsset;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 提供示例的本地资源服务器，后台线程不访问 Unity 对象。
    /// </summary>
    public sealed class ResourceHttpServer : IDisposable, IRemoteService
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
        private readonly List<Task> workers = new List<Task>();
        private readonly Task accept;
        private readonly string root;
        private int rejectResources, requests;
        private bool disposed;
        /// <summary>
        /// 获取本地服务器地址。
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 获取已接收的请求数量。
        /// </summary>
        public int Requests
        {
            get
            {
                return Volatile.Read(ref requests);
            }
        }

        /// <summary>
        /// 获取或设置是否拒绝资源文件请求。
        /// </summary>
        public bool RejectResources
        {
            get
            {
                return Volatile.Read(ref rejectResources) != 0;
            }
            set
            {
                Volatile.Write(ref rejectResources, value ? 1 : 0);
            }
        }

        /// <summary>
        /// 创建本地资源服务器。
        /// </summary>
        /// <param name="folder">资源包输出目录。</param>
        public ResourceHttpServer(string folder)
        {
            root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            accept = Task.Run(async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    }
                    catch (SocketException) when (stop.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException) when (stop.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (InvalidOperationException) when (stop.IsCancellationRequested)
                    {
                        break;
                    }
                    lock (clients)
                    {
                        if (stop.IsCancellationRequested)
                        {
                            client.Dispose();
                            break;
                        }
                        clients.Add(client);
                        workers.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await Serve(client.GetStream()).ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                // 请求取消时连接可能已经关闭。
                            }
                            catch (SocketException)
                            {
                            }
                            catch (ObjectDisposedException) when (stop.IsCancellationRequested)
                            {
                            }
                            catch (OperationCanceledException) when (stop.IsCancellationRequested)
                            {
                            }
                            finally
                            {
                                lock (clients)
                                {
                                    clients.Remove(client);
                                }
                                client.Dispose();
                            }
                        }));
                    }
                }
            });
        }

        private async Task Serve(NetworkStream stream)
        {
            var header = new StringBuilder();
            var buffer = new byte[1024];
            while (!header.ToString().Contains("\r\n\r\n"))
            {
                int count = await stream.ReadAsync(buffer, 0, buffer.Length, stop.Token).ConfigureAwait(false);
                if (count == 0)
                {
                    return;
                }
                header.Append(Encoding.ASCII.GetString(buffer, 0, count));
                if (header.Length > 8192)
                {
                    return;
                }
            }
            string[] lines = header.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] request = lines[0].Split(' ');
            if (request.Length < 2 || request[0] != "GET")
            {
                return;
            }
            string relative = Uri.UnescapeDataString(request[1].Split('?')[0]).TrimStart('/');
            string path = Path.GetFullPath(Path.Combine(root, relative));
            bool version = Path.GetFileName(path).StartsWith("PackageManifest_", StringComparison.Ordinal);
            bool found = path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path) && (version || !RejectResources);
            byte[] bytes = found ? File.ReadAllBytes(path) : Array.Empty<byte>();
            int offset = 0;
            bool partial = false, validRange = true;
            foreach (string line in lines)
            {
                if (!line.StartsWith("Range: bytes=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                partial = true;
                validRange = int.TryParse(line.Substring(13).Split('-')[0], out offset) && offset >= 0 && offset < bytes.Length;
            }
            Interlocked.Increment(ref requests);
            string status = !found ? "404 Not Found" : !validRange ? "416 Range Not Satisfiable" : partial ? "206 Partial Content" : "200 OK";
            int length = found && validRange ? bytes.Length - offset : 0;
            string range = partial && found && validRange ? $"Content-Range: bytes {offset}-{bytes.Length - 1}/{bytes.Length}\r\n" : "";
            byte[] response = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Length: {length}\r\n{range}Connection: close\r\n\r\n");
            // 留出可观察的下载时间，方便点击停止；版本清单立即返回。
            if (!version)
            {
                await Task.Delay(1000, stop.Token).ConfigureAwait(false);
            }
            await stream.WriteAsync(response, 0, response.Length, stop.Token).ConfigureAwait(false);
            for (int position = offset; position < offset + length; position += 1024)
            {
                await stream.WriteAsync(bytes, position, Math.Min(1024, offset + length - position), stop.Token).ConfigureAwait(false);
                if (!version)
                {
                    await Task.Delay(40, stop.Token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 获取文件下载地址。
        /// </summary>
        /// <param name="fileName">文件名称。</param>
        /// <returns>本地服务器上的文件地址。</returns>
        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            return new[] { Url + "/" + fileName };
        }

        /// <summary>
        /// 关闭服务器并等待后台请求退出。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            stop.Cancel();
            listener.Stop();
            lock (clients)
            {
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
            accept.GetAwaiter().GetResult();
            Task[] pending;
            lock (clients)
            {
                pending = workers.ToArray();
            }
            Task.WhenAll(pending).GetAwaiter().GetResult();
            stop.Dispose();
        }
    }
}
