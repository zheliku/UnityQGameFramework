using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UnityQGameFramework.Examples
{
    /// <summary>本机随机端口的示例服务；后台任务只操作 .NET 对象。</summary>
    public sealed class LocalHttpListener : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
        private readonly List<Task> workers = new List<Task>();
        private readonly Task accept;
        private bool disposed;
        public int Port { get; }

        public LocalHttpListener(Func<NetworkStream, CancellationToken, Task> serve)
        {
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            accept = Task.Run(async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    TcpClient client;
                    try { client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                    catch (SocketException) when (stop.IsCancellationRequested) { break; }
                    catch (ObjectDisposedException) when (stop.IsCancellationRequested) { break; }
                    catch (InvalidOperationException) when (stop.IsCancellationRequested) { break; }
                    lock (clients)
                    {
                        if (stop.IsCancellationRequested) { client.Dispose(); break; }
                        clients.Add(client);
                        workers.Add(Task.Run(async () =>
                        {
                            try { await serve(client.GetStream(), stop.Token).ConfigureAwait(false); }
                            catch (IOException) { /* 取消请求会关闭连接。 */ }
                            catch (SocketException) { }
                            catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
                            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                            finally { lock (clients) clients.Remove(client); client.Dispose(); }
                        }));
                    }
                }
            });
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            stop.Cancel();
            listener.Stop();
            lock (clients) foreach (var client in clients) client.Dispose();
            accept.GetAwaiter().GetResult();
            Task[] pending;
            lock (clients) pending = workers.ToArray();
            Task.WhenAll(pending).GetAwaiter().GetResult();
            stop.Dispose();
        }
    }
}
