using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>将 HTTP 内容保存到磁盘，再读取正式文件。</summary>
    public sealed class DownloadFileExample : MonoBehaviour
    {
        [SerializeField] private Button downloadButton, clearButton;
        [SerializeField] private Text stateText, pathText, contentText;
        [SerializeField] private TextAsset payload;

        private DownloadHttpServer server;
        private string path;
        private int serial;
        private string state = "尚未下载";

        private void Start()
        {
            server = new DownloadHttpServer(payload.bytes);
            path = Path.Combine(Application.persistentDataPath, "QgfExamples", "DownloadFileExample", "payload.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            DeleteFiles();
            // 先订阅结果再提交任务；事件参数会回收，只保存需要的业务值。
            QGF.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnEvent);
            QGF.Event.Subscribe(DownloadFailureEventArgs.EventId, OnEvent);
            downloadButton.onClick.AddListener(Download);
            clearButton.onClick.AddListener(() => { DeleteFiles(); state = "已清理"; contentText.text = ""; });
        }

        private void Download()
        {
            if (serial != 0 || server == null) return;
            DeleteFiles();
            string uri = server.Url("/payload");
            // 返回任务序号；结果事件用它确认本次请求。
            serial = QGF.Download.AddDownload(path, uri);
            state = $"任务 #{serial} 等待下载";
            contentText.text = "";
        }

        private void OnEvent(object sender, GameEventArgs args)
        {
            // 全局事件先匹配 SerialId；没有传 UserData 的重载也能区分本次任务。
            if (args is DownloadSuccessEventArgs success && success.SerialId == serial)
            {
                state = $"完成 · {success.CurrentLength} 字节";
                contentText.text = File.ReadAllText(path).Trim(); // 成功时临时文件已转为正式文件。
            }
            else if (args is DownloadFailureEventArgs failure && failure.SerialId == serial)
                state = "失败：" + failure.ErrorMessage;
            else return;
            serial = 0;
        }

        private void LateUpdate()
        {
            stateText.text = state;
            string finalFile = File.Exists(path) ? "已保存" : "无";
            string temporaryFile = File.Exists(path + ".download") ? "下载中" : "无";
            pathText.text = $"保存路径：{path}\n正式文件：{finalFile} · 临时文件：{temporaryFile}";
            downloadButton.interactable = clearButton.interactable = server != null && serial == 0;
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
