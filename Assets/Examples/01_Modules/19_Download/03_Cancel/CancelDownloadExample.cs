using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using FileInfo = System.IO.FileInfo;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 取消后保留临时文件可续传，清理临时文件则从头下载。
    /// </summary>
    public sealed class CancelDownloadExample : MonoBehaviour
    {
        [SerializeField]
        private Button downloadButton, cancelButton, clearButton;
        [SerializeField]
        private Dropdown cancelMode;
        [SerializeField]
        private Slider progressSlider;
        [SerializeField]
        private Text stateText, fileText;
        [SerializeField]
        private TextAsset payload;
        private int totalLength;
        private long received, resumedFrom;
        private bool matched;
        private DownloadHttpServer server;
        private string path;
        private int serial;
        private string state = "Not downloaded";
        private void Start()
        {
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
            clearButton.onClick.AddListener(() =>
            {
                DeleteFiles();
                received = resumedFrom = 0;
                state = "Cleared. The next download starts from the beginning.";
            });
        }

        private void Download()
        {
            if (serial != 0 || server == null)
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // 保留 .download：代理会读它的长度，并要求服务器从该位置返回 Range 数据。
            received = File.Exists(path + ".download") ? new FileInfo(path + ".download").Length : 0;
            resumedFrom = 0;
            matched = false;
            serial = QGF.Download.AddDownload(path, server.Url("/slow"), this);
            state = "Submitted; waiting for the start event";
        }

        private void Cancel()
        {
            if (serial == 0)
            {
                return;
            }

            // RemoveDownload 关闭连接和文件流，不触发失败事件，也不会自行删除临时文件。
            bool removed = QGF.Download.RemoveDownload(serial);
            serial = 0;
            if (cancelMode.value == 1)
            {
                DeleteFiles();
            }

            received = File.Exists(path + ".download") ? new FileInfo(path + ".download").Length : 0;
            state = removed ? cancelMode.value == 0 ? "Canceled; temporary file retained. Start again to resume." : "Canceled and cleared. The next download starts from the beginning." : "The task has already finished.";
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            if (args is DownloadStartEventArgs start && start.SerialId == serial)
            {
                resumedFrom = received = start.CurrentLength;
                state = resumedFrom == 0 ? "Downloading from the start" : $"Resuming from {resumedFrom:N0} bytes";
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
                state = matched ? "Complete · File contents match" : "Complete · File contents differ";
            }
            else if (args is DownloadFailureEventArgs failure && failure.SerialId == serial)
            {
                state = "Failed: " + failure.ErrorMessage;
            }
            else
            {
                return;
            }

            serial = 0;
        }

        private void LateUpdate()
        {
            progressSlider.value = totalLength == 0 ? 0 : (float)received / totalLength;
            stateText.text = $"{state}\n{received:N0} / {totalLength:N0} bytes · {progressSlider.value:P0}";
            if (server == null)
            {
                return;
            }

            string finalFile = File.Exists(path) ? "Saved" : "None";
            string temporaryFile = File.Exists(path + ".download") ? $"Resumable · {received:N0} bytes" : "None";
            fileText.text = $"Resume offset {resumedFrom:N0} · Server Range start {server.LastRange}\n" + $"Final file: {finalFile} · Temporary file: {temporaryFile}";
            downloadButton.interactable = clearButton.interactable = serial == 0;
            cancelButton.interactable = serial != 0;
        }

        private void DeleteFiles()
        {
            if (path == null)
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(path + ".download"))
            {
                File.Delete(path + ".download");
            }
        }

        private void OnDestroy()
        {
            var download = GameEntry.GetComponent<DownloadComponent>();
            if (download != null && serial != 0)
            {
                download.RemoveDownload(serial);
            }

            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(DownloadStartEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(DownloadStartEventArgs.EventId, OnEvent);
                }

                if (events.Check(DownloadUpdateEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(DownloadUpdateEventArgs.EventId, OnEvent);
                }

                if (events.Check(DownloadSuccessEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(DownloadSuccessEventArgs.EventId, OnEvent);
                }

                if (events.Check(DownloadFailureEventArgs.EventId, OnEvent))
                {
                    events.Unsubscribe(DownloadFailureEventArgs.EventId, OnEvent);
                }
            }

            server?.Dispose();
            DeleteFiles(); // 清理正式文件和临时文件。
        }
    }
}
