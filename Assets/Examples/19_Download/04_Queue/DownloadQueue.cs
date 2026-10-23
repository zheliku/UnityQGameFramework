using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    /// <summary>暂停调度后建立队列，恢复时按优先级下载；每个任务使用不同保存路径。</summary>
    public sealed class DownloadQueue : MonoBehaviour
    {
        [SerializeField] private Button enqueueButton, cancelButton;
        [SerializeField] private Toggle pausedToggle;
        [SerializeField] private Dropdown queryMode, cancelMode;
        [SerializeField] private Text queueText, orderText, resultText;
        private readonly List<TaskInfo> tasks = new List<TaskInfo>();
        private readonly List<string> order = new List<string>();
        private DownloadHttpServer server;
        private string directory;
        private int low, callbacks;

        private async void Start()
        {
            try
            {
                await UniTask.NextFrame(destroyCancellationToken);
                server = new DownloadHttpServer(new byte[] { 1, 2, 3, 4 });
                directory = Path.Combine(Application.persistentDataPath, "QgfExamples", "DownloadQueue");
                Directory.CreateDirectory(directory);
                QGF.Event.Subscribe(DownloadStartEventArgs.EventId, OnStart);
                QGF.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnResult);
                QGF.Event.Subscribe(DownloadFailureEventArgs.EventId, OnResult);
                // Paused 暂停后续任务调度，已发出的请求继续接收。
                pausedToggle.onValueChanged.AddListener(value => QGF.Download.Paused = value);
                enqueueButton.onClick.AddListener(Enqueue);
                cancelButton.onClick.AddListener(Cancel);
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Enqueue()
        {
            if (server == null || QGF.Download.GetAllDownloadInfos().Length != 0) return;
            pausedToggle.isOn = true; // 先暂停，便于观察入队顺序与优先级，不依赖网络快慢。
            order.Clear();
            callbacks = 0;
            foreach (string name in new[] { "low", "high", "other" })
            {
                string path = Path.Combine(directory, name + ".bin");
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".download")) File.Delete(path + ".download");
            }
            low = QGF.Download.AddDownload(Path.Combine(directory, "low.bin"), server.Url("/low"), "Example.Batch", 0, this);
            QGF.Download.AddDownload(Path.Combine(directory, "high.bin"), server.Url("/high"), "Example.Batch", 10, this);
            QGF.Download.AddDownload(Path.Combine(directory, "other.bin"), server.Url("/other"), "Example.Other", 5, this);
            resultText.text = "取消勾选暂停调度，观察 high → other → low。";
        }

        private void Cancel()
        {
            // 取消不产生业务结果事件；在这里更新界面，不等待失败回调。
            // 单项重载返回是否移除；标签与全部移除返回实际任务数。
            if (cancelMode.value == 0)
                resultText.text = QGF.Download.RemoveDownload(low) ? "low 任务已移除。" : "low 任务已不在队列中。";
            else if (cancelMode.value == 1)
            {
                int removed = QGF.Download.RemoveDownloads("Example.Batch");
                resultText.text = removed == 0 ? "Example.Batch 没有待移除任务。" : $"Example.Batch 已移除 {removed} 个任务。";
            }
            else
            {
                int removed = QGF.Download.RemoveAllDownloads();
                resultText.text = removed == 0 ? "没有待移除任务。" : $"全部下载已移除 {removed} 个任务。";
            }
        }

        private void OnStart(object sender, GameEventArgs args)
        {
            var e = (DownloadStartEventArgs)args;
            if (ReferenceEquals(e.UserData, this)) order.Add(Path.GetFileNameWithoutExtension(e.DownloadPath));
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            object owner = args is DownloadSuccessEventArgs success ? success.UserData : ((DownloadFailureEventArgs)args).UserData;
            if (ReferenceEquals(owner, this)) callbacks++;
        }

        private void LateUpdate()
        {
            if (server == null) return;
            // 四种查询得到同一时刻的快照。List 版本会清空再填入，可复用同一容器。
            switch (queryMode.value)
            {
                case 0:
                    tasks.Clear();
                    tasks.AddRange(QGF.Download.GetAllDownloadInfos());
                    break;
                case 1:
                    QGF.Download.GetAllDownloadInfos(tasks);
                    break;
                case 2:
                    tasks.Clear();
                    tasks.AddRange(QGF.Download.GetDownloadInfos("Example.Batch"));
                    break;
                default:
                    QGF.Download.GetDownloadInfos("Example.Batch", tasks);
                    break;
            }
            var rows = new StringBuilder(
                $"代理 {QGF.Download.TotalAgentCount} · 工作 {QGF.Download.WorkingAgentCount} · "
                + $"空闲 {QGF.Download.FreeAgentCount} · 排队 {QGF.Download.WaitingTaskCount}\n");
            foreach (var task in tasks)
            {
                string status = task.Status == TaskStatus.Doing ? "执行中" : "等待中";
                rows.AppendLine($"#{task.SerialId}  {status}  优先级 {task.Priority}  {task.Tag}");
            }
            queueText.text = rows.ToString();
            // 单项查询返回当前快照；任务完成或移除后，IsValid 会变为 false。
            var lowInfo = low == 0 ? default : QGF.Download.GetDownloadInfo(low);
            string lowState = low == 0 ? "未入队"
                : lowInfo.IsValid ? $"{lowInfo.Status} · {lowInfo.Description}" : "已完成或移除";
            orderText.text = $"low 单项查询：{lowState}\n实际开始顺序：{string.Join(" → ", order)}\n结果回调：{callbacks} 次";
            enqueueButton.interactable = QGF.Download.GetAllDownloadInfos().Length == 0;
            cancelButton.interactable = !enqueueButton.interactable;
        }

        private void OnDestroy()
        {
            var download = GameEntry.GetComponent<DownloadComponent>();
            if (download != null)
            {
                download.RemoveDownloads("Example.Batch");
                download.RemoveDownloads("Example.Other");
                download.Paused = false;
            }
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(DownloadStartEventArgs.EventId, OnStart)) events.Unsubscribe(DownloadStartEventArgs.EventId, OnStart);
                if (events.Check(DownloadSuccessEventArgs.EventId, OnResult)) events.Unsubscribe(DownloadSuccessEventArgs.EventId, OnResult);
                if (events.Check(DownloadFailureEventArgs.EventId, OnResult)) events.Unsubscribe(DownloadFailureEventArgs.EventId, OnResult);
            }
            server?.Dispose();
            if (directory == null) return;
            foreach (string name in new[] { "low", "high", "other" })
                foreach (string suffix in new[] { ".bin", ".bin.download" })
                    if (File.Exists(Path.Combine(directory, name + suffix))) File.Delete(Path.Combine(directory, name + suffix));
        }
    }
}
