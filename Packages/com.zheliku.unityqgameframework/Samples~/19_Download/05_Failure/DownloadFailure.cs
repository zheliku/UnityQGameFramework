using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    /// <summary>失败保留旧正式文件；观察 404、超时和无效的 Range 响应。</summary>
    public sealed class DownloadFailure : MonoBehaviour
    {
        [SerializeField] private Dropdown failureMode;
        [SerializeField] private Button downloadButton;
        [SerializeField] private Text stateText, fileText;
        [SerializeField] private TextAsset payload;

        private float started, elapsed;
        private DownloadHttpServer server;
        private string path;
        private int serial;
        private string state = "尚未下载";

        private async void Start()
        {
            try
            {
                // 等待框架组件创建下载代理，再提交任务。
                await UniTask.NextFrame(destroyCancellationToken);
                server = new DownloadHttpServer(payload.bytes);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "DownloadFailure", "payload.bin");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                DeleteFiles();
                // 先订阅结果再提交任务；事件参数会回收，只保存需要的业务值。
                QGF.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                QGF.Event.Subscribe(DownloadFailureEventArgs.EventId, OnEvent);
                downloadButton.onClick.AddListener(Download);
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Download()
        {
            if (serial != 0 || server == null) return;
            DeleteFiles();
            File.WriteAllText(path, "existing file"); // 失败时应保留的旧正式文件。
            if (failureMode.value == 2) File.WriteAllBytes(path + ".download", payload.bytes.Take(8).ToArray());
            if (failureMode.value == 3) File.WriteAllBytes(path + ".download", payload.bytes); // 起点已到末尾，服务返回 416。
            string uri = server.Url(new[] { "/missing", "/hold", "/ignore-range", "/payload", "/payload" }[failureMode.value]);
            float previous = QGF.Download.Timeout;
            try
            {
                QGF.Download.Timeout = failureMode.value == 1 ? 1f : previous;
                serial = QGF.Download.AddDownload(path, uri, this);
            }
            finally { QGF.Download.Timeout = previous; }
            started = Time.realtimeSinceStartup;
            state = "等待结果";
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is DownloadFailureEventArgs failure && failure.SerialId == serial)
                state = "失败：" + failure.ErrorMessage;
            else if (args is DownloadSuccessEventArgs success && success.SerialId == serial)
                state = File.ReadAllBytes(path).SequenceEqual(payload.bytes) ? "完成 · 已替换旧文件，内容一致" : "完成 · 内容不一致";
            else return;
            elapsed = Time.realtimeSinceStartup - started;
            serial = 0;
        }

        private void LateUpdate()
        {
            stateText.text = state + $"\n耗时 {(serial == 0 ? elapsed : Time.realtimeSinceStartup - started):F2} 秒";
            if (server == null) return;
            bool old = File.Exists(path) && new FileInfo(path).Length == 13 && File.ReadAllText(path) == "existing file";
            bool hasTemporaryFile = File.Exists(path + ".download");
            long partial = hasTemporaryFile ? new FileInfo(path + ".download").Length : 0;
            string finalFile = old ? "旧文件仍保留" : File.Exists(path) ? "新文件已保存" : "无";
            string temporaryFile = hasTemporaryFile ? $"保留 · {partial:N0} 字节" : "无";
            fileText.text = $"正式文件：{finalFile}\n临时文件：{temporaryFile}\n服务器收到 Range 起点：{server.LastRange}";
            downloadButton.interactable = serial == 0;
        }

        private void DeleteFiles()
        {
            if (path == null) return;
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".download")) File.Delete(path + ".download");
        }

        private void OnDestroy()
        {
            var download = GameEntry.GetComponent<DownloadComponent>();
            if (download != null && serial != 0) download.RemoveDownload(serial);
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(DownloadSuccessEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                if (events.Check(DownloadFailureEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadFailureEventArgs.EventId, OnEvent);
            }
            server?.Dispose();
            DeleteFiles(); // 清理正式文件和临时文件。

        }
    }
}
