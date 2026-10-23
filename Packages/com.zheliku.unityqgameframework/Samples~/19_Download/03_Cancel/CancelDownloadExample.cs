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
    /// <summary>取消后保留临时文件可续传，清理临时文件则从头下载。</summary>
    public sealed class CancelDownloadExample : MonoBehaviour
    {
        [SerializeField] private Button downloadButton, cancelButton, clearButton;
        [SerializeField] private Dropdown cancelMode;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text stateText, fileText;
        [SerializeField] private TextAsset payload;

        private int totalLength;
        private long received, resumedFrom;
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
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "CancelDownloadExample", "payload.bin");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                DeleteFiles();
                // 先订阅结果再提交任务；事件参数会回收，只保存需要的业务值。
                QGF.Event.Subscribe(DownloadStartEventArgs.EventId, OnEvent);
                QGF.Event.Subscribe(DownloadUpdateEventArgs.EventId, OnEvent);
                QGF.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                QGF.Event.Subscribe(DownloadFailureEventArgs.EventId, OnEvent);
                downloadButton.onClick.AddListener(Download);
                cancelButton.onClick.AddListener(Cancel);
                clearButton.onClick.AddListener(() => { DeleteFiles(); received = resumedFrom = 0; state = "已清理，下次从头下载"; });
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Download()
        {
            if (serial != 0 || server == null) return;
            if (File.Exists(path)) File.Delete(path);
            // 保留 .download：代理会读它的长度，并要求服务器从该位置返回 Range 数据。
            received = File.Exists(path + ".download") ? new FileInfo(path + ".download").Length : 0;
            resumedFrom = 0;
            matched = false;
            serial = QGF.Download.AddDownload(path, server.Url("/slow"), this);
            state = "已提交；等待开始事件";
        }

        private void Cancel()
        {
            if (serial == 0) return;
            // RemoveDownload 关闭连接和文件流，不触发失败事件，也不会自行删除临时文件。
            bool removed = QGF.Download.RemoveDownload(serial);
            serial = 0;
            if (cancelMode.value == 1) DeleteFiles();
            received = File.Exists(path + ".download") ? new FileInfo(path + ".download").Length : 0;
            state = removed ? cancelMode.value == 0 ? "已取消，保留临时文件。再次开始将续传。" : "已取消并清理，下次从头下载。" : "任务已经结束";
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is DownloadStartEventArgs start && start.SerialId == serial)
            {
                resumedFrom = received = start.CurrentLength;
                state = resumedFrom == 0 ? "从头下载" : $"从 {resumedFrom:N0} 字节继续下载";
                return;
            }
            if (args is DownloadUpdateEventArgs update && update.SerialId == serial)
            {
                received = update.CurrentLength;
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
            progressSlider.value = totalLength == 0 ? 0 : (float)received / totalLength;
            stateText.text = $"{state}\n{received:N0} / {totalLength:N0} 字节 · {progressSlider.value:P0}";
            if (server == null) return;
            string finalFile = File.Exists(path) ? "已保存" : "无";
            string temporaryFile = File.Exists(path + ".download") ? $"可续传 · {received:N0} 字节" : "无";
            fileText.text = $"本次起点 {resumedFrom:N0} · 服务器收到 Range 起点 {server.LastRange}\n"
                + $"正式文件：{finalFile} · 临时文件：{temporaryFile}";
            downloadButton.interactable = clearButton.interactable = serial == 0;
            cancelButton.interactable = serial != 0;
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
                if (events.Check(DownloadStartEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadStartEventArgs.EventId, OnEvent);
                if (events.Check(DownloadUpdateEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadUpdateEventArgs.EventId, OnEvent);
                if (events.Check(DownloadSuccessEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                if (events.Check(DownloadFailureEventArgs.EventId, OnEvent)) events.Unsubscribe(DownloadFailureEventArgs.EventId, OnEvent);
            }
            server?.Dispose();
            DeleteFiles(); // 清理正式文件和临时文件。

        }
    }
}
