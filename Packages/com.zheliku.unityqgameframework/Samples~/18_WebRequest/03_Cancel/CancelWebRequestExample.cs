using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    /// <summary>一个代理执行请求，其余任务排队；优先级只影响尚未执行的任务。</summary>
    public sealed class CancelWebRequestExample : MonoBehaviour
    {
        [SerializeField] private Button enqueueButton, cancelButton;
        [SerializeField] private Dropdown queryMode, cancelMode;
        [SerializeField] private Text queueText, orderText, resultText;
        private readonly List<TaskInfo> tasks = new List<TaskInfo>();
        private readonly List<string> order = new List<string>();
        private LocalHttpServer server;
        private int hold, callbacks;
        private bool adding;

        private async void Start()
        {
            try
            {
                await UniTask.NextFrame(destroyCancellationToken);
                server = new LocalHttpServer(new byte[0]);
                QGF.Event.Subscribe(WebRequestStartEventArgs.EventId, OnStart);
                QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
                enqueueButton.onClick.AddListener(Enqueue);
                cancelButton.onClick.AddListener(Cancel);
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private async void Enqueue()
        {
            if (server == null || adding || QGF.WebRequest.GetAllWebRequestInfos().Length != 0) return;
            adding = true;
            order.Clear();
            callbacks = 0;
            try
            {
                // 先占住唯一代理；高优先级任务不会打断已经执行的请求。
                hold = QGF.WebRequest.AddWebRequest(server.Url("/hold"), "Example.Queue", 0, this);
                do
                {
                    await UniTask.NextFrame(destroyCancellationToken);
                }
                while (QGF.WebRequest.GetWebRequestInfo(hold).Status != UnityQGameFramework.Core.TaskStatus.Doing);
                QGF.WebRequest.AddWebRequest(server.Url("/low"), "Example.Queue", 0, this);
                QGF.WebRequest.AddWebRequest(server.Url("/high"), "Example.Queue", 10, this);
                QGF.WebRequest.AddWebRequest(server.Url("/other"), "Example.Other", 5, this);
                resultText.text = "取消 hold 后观察：high → other → low。也可以按标签或全部取消。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
            finally { adding = false; }
        }

        private void Cancel()
        {
            // 取消不触发成功/失败事件，业务界面应在此处主动结束自己的等待。
            // 单项重载返回是否移除；标签与全部移除返回实际任务数。
            if (cancelMode.value == 0)
                resultText.text = QGF.WebRequest.RemoveWebRequest(hold) ? "hold 请求已取消。" : "hold 请求已不在队列中。";
            else if (cancelMode.value == 1)
            {
                int removed = QGF.WebRequest.RemoveWebRequests("Example.Queue");
                resultText.text = removed == 0 ? "Example.Queue 没有待取消请求。" : $"Example.Queue 已取消 {removed} 个请求。";
            }
            else
            {
                int removed = QGF.WebRequest.RemoveAllWebRequests();
                resultText.text = removed == 0 ? "没有待取消请求。" : $"全部请求已取消 {removed} 个。";
            }
        }

        private void OnStart(object sender, GameEventArgs args)
        {
            var e = (WebRequestStartEventArgs)args;
            if (ReferenceEquals(e.UserData, this)) order.Add(new Uri(e.WebRequestUri).AbsolutePath.TrimStart('/'));
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            object owner = args is WebRequestSuccessEventArgs success ? success.UserData : ((WebRequestFailureEventArgs)args).UserData;
            if (ReferenceEquals(owner, this)) callbacks++;
        }

        private void LateUpdate()
        {
            if (server == null) return;
            // 数组方便一次性查询；List 版本复用容器。两者得到的都是当时的快照。
            switch (queryMode.value)
            {
                case 0:
                    tasks.Clear();
                    tasks.AddRange(QGF.WebRequest.GetAllWebRequestInfos());
                    break;
                case 1:
                    QGF.WebRequest.GetAllWebRequestInfos(tasks);
                    break;
                case 2:
                    tasks.Clear();
                    tasks.AddRange(QGF.WebRequest.GetWebRequestInfos("Example.Queue"));
                    break;
                default:
                    QGF.WebRequest.GetAllWebRequestInfos("Example.Queue", tasks);
                    break;
            }
            var rows = new StringBuilder(
                $"代理 {QGF.WebRequest.TotalAgentCount} · 工作 {QGF.WebRequest.WorkingAgentCount} · "
                + $"空闲 {QGF.WebRequest.FreeAgentCount} · 排队 {QGF.WebRequest.WaitingTaskCount}\n");
            foreach (var task in tasks)
            {
                string status = task.Status == TaskStatus.Doing ? "执行中" : "等待中";
                rows.AppendLine($"#{task.SerialId}  {new Uri(task.Description).AbsolutePath}  {status}  优先级 {task.Priority}  {task.Tag}");
            }
            queueText.text = rows.ToString();
            orderText.text = "实际开始顺序：" + string.Join(" → ", order) + $"\n结果回调：{callbacks} 次";
            enqueueButton.interactable = !adding && QGF.WebRequest.WorkingAgentCount + QGF.WebRequest.WaitingTaskCount == 0;
            cancelButton.interactable = !adding && !enqueueButton.interactable;
        }

        private void OnDestroy()
        {
            var web = GameEntry.GetComponent<WebRequestComponent>();
            if (web != null)
            {
                // 按标签取消这个场景的任务。
                web.RemoveWebRequests("Example.Queue");
                web.RemoveWebRequests("Example.Other");
            }
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(WebRequestStartEventArgs.EventId, OnStart)) events.Unsubscribe(WebRequestStartEventArgs.EventId, OnStart);
                if (events.Check(WebRequestSuccessEventArgs.EventId, OnResult)) events.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                if (events.Check(WebRequestFailureEventArgs.EventId, OnResult)) events.Unsubscribe(WebRequestFailureEventArgs.EventId, OnResult);
            }
            server?.Dispose();
        }
    }
}
