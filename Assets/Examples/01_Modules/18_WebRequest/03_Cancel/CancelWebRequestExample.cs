using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>一个代理执行请求，其余任务排队；优先级只影响尚未执行的任务。</summary>
    public sealed class CancelWebRequestExample : MonoBehaviour
    {
        [SerializeField] private Button enqueueButton, cancelButton;
        [SerializeField] private Text queueText, orderText, resultText;
        private readonly List<TaskInfo> tasks = new List<TaskInfo>();
        private readonly List<string> order = new List<string>();
        private LocalHttpServer server;
        private int hold, callbacks;
        private bool adding;

        private void Start()
        {
            server = new LocalHttpServer(new byte[0]);
            QGF.Event.Subscribe(WebRequestStartEventArgs.EventId, OnStart);
            QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
            enqueueButton.onClick.AddListener(Enqueue);
            cancelButton.onClick.AddListener(Cancel);
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
            resultText.text = "取消正在执行的 hold；后续任务仍按优先级执行。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
            finally { adding = false; }
        }

        private void Cancel()
        {
            // 移除任务后不会发出成功或失败事件，业务界面在这里结束自己的等待。
            resultText.text = QGF.WebRequest.RemoveWebRequest(hold)
                ? "hold 请求已取消。"
                : "hold 请求已不在队列中。";
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
            QGF.WebRequest.GetAllWebRequestInfos(tasks);
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
