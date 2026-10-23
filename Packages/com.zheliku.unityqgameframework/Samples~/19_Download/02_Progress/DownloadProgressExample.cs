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
    /// <summary>按已知文件总长度显示字节进度，区分已接收与正式文件。</summary>
    public sealed class DownloadProgressExample : MonoBehaviour
    {
        [SerializeField] private Button downloadButton;
        [SerializeField] private Dropdown flushMode;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text stateText, fileText;
        [SerializeField] private TextAsset payload;

        private int totalLength;
        private long received;
        private int flushSize;
        private bool matched;
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
                byte[] bytes = payload.bytes;
                totalLength = bytes.Length;
                server = new DownloadHttpServer(bytes);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "DownloadProgressExample", "payload.bin");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                DeleteFiles();
                // 先订阅结果再提交任务；事件参数会回收，只保存需要的业务值。
                QGF.Event.Subscribe(DownloadUpdateEventArgs.EventId, OnEvent);
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
            received = 0;
            matched = false;
            flushSize = flushMode.value == 0 ? 4096 : flushMode.value == 1 ? 65536 : 1048576;
            int previous = QGF.Download.FlushSize;
            try
            {
                // FlushSize 在加入任务时保存；它控制刷新文件流的批量，不是网络块大小。
                QGF.Download.FlushSize = flushSize;
                serial = QGF.Download.AddDownload(path, server.Url("/slow"), this);
            }
            finally { QGF.Download.FlushSize = previous; }
            state = "下载中";
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is DownloadUpdateEventArgs update && update.SerialId == serial)
            {
                received = update.CurrentLength; // 累计字节，包含续传前已有的部分；不是 0–1 比例。
                return;
            }
            if (args is DownloadSuccessEventArgs success && success.SerialId == serial)
            {
                received = success.CurrentLength;
                matched = File.ReadAllBytes(path).SequenceEqual(payload.bytes);
                state = matched ? "完成 · 文件内容一致" : "完成 · 文件内容不一致";
            }
            else if (args is DownloadFailureEventArgs failure && failure.SerialId == serial)
                state = "失败：" + failure.ErrorMessage;
            else return;
            serial = 0;
        }

        private void LateUpdate()
        {
            // 载荷总长度已知，用累计字节计算百分比。
            progressSlider.value = totalLength == 0 ? 0 : (float)received / totalLength;
            stateText.text = $"{state}\n{received:N0} / {totalLength:N0} 字节 · {progressSlider.value:P0}";
            if (server == null) return;
            long temporary = File.Exists(path + ".download") ? new FileInfo(path + ".download").Length : 0;
            string finalFile = File.Exists(path) ? "已保存" : "尚未生成";
            fileText.text = $"组件总速度 {QGF.Download.CurrentSpeed / 1024:F1} KiB/s · FlushSize {flushSize:N0}\n"
                + $"临时文件 {temporary:N0} 字节 · 正式文件：{finalFile}";
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
                if (events.Check(DownloadUpdateEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadUpdateEventArgs.EventId, OnEvent);
                if (events.Check(DownloadSuccessEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                if (events.Check(DownloadFailureEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadFailureEventArgs.EventId, OnEvent);
            }
            server?.Dispose();
            DeleteFiles(); // 清理正式文件和临时文件。

        }
    }
}
