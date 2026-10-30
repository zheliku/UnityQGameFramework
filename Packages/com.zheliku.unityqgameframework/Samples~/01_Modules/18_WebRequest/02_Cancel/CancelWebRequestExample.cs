using TMPro;
using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 一个代理执行请求，其余任务排队；优先级只影响尚未执行的任务。
    /// </summary>
    public sealed class CancelWebRequestExample : MonoBehaviour
    {
        [SerializeField]
        private Button enqueueButton, cancelButton;
        [SerializeField]
        private TMP_Text queueText, orderText, resultText;
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
            if (server == null || adding || QGF.WebRequest.GetAllWebRequestInfos().Length != 0)
            {
                return;
            }

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
                resultText.text = "Cancel the active hold request. Remaining tasks run in priority order.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                adding = false;
            }
        }

        private void Cancel()
        {
            // 移除任务后不会发出成功或失败事件，业务界面在这里结束自己的等待。
            resultText.text = QGF.WebRequest.RemoveWebRequest(hold) ? "The hold request was canceled." : "The hold request is no longer queued.";
        }

        private void OnStart(object sender, GameEventArgs args)
        {
            var e = (WebRequestStartEventArgs)args;
            if (ReferenceEquals(e.UserData, this))
            {
                order.Add(new Uri(e.WebRequestUri).AbsolutePath.TrimStart('/'));
            }
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            object owner = args is WebRequestSuccessEventArgs success ? success.UserData : ((WebRequestFailureEventArgs)args).UserData;
            if (ReferenceEquals(owner, this))
            {
                callbacks++;
            }
        }

        private void LateUpdate()
        {
            if (server == null)
            {
                return;
            }

            QGF.WebRequest.GetAllWebRequestInfosNonAlloc(tasks);
            var rows = new StringBuilder($"Agents {QGF.WebRequest.TotalAgentCount} · Working {QGF.WebRequest.WorkingAgentCount} · " + $"Idle {QGF.WebRequest.FreeAgentCount} · Queued {QGF.WebRequest.WaitingTaskCount}\n");
            foreach (var task in tasks)
            {
                string status = task.Status == TaskStatus.Doing ? "Running" : "Waiting";
                rows.AppendLine($"#{task.SerialId}  {new Uri(task.Description).AbsolutePath}  {status}  Priority {task.Priority}  {task.Tag}");
            }

            queueText.text = rows.ToString();
            orderText.text = "Actual start order: " + string.Join(" → ", order) + $"\nResult callbacks: {callbacks} times";
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
                if (events.Check(WebRequestStartEventArgs.EventId, OnStart))
                {
                    events.Unsubscribe(WebRequestStartEventArgs.EventId, OnStart);
                }

                if (events.Check(WebRequestSuccessEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                }

                if (events.Check(WebRequestFailureEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(WebRequestFailureEventArgs.EventId, OnResult);
                }
            }

            server?.Dispose();
        }
    }
}
