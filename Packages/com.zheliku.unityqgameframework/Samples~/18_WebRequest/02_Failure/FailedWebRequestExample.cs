using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    /// <summary>分别观察 HTTP 404、请求超时和恢复后的成功响应。</summary>
    public sealed class FailedWebRequestExample : MonoBehaviour
    {
        [SerializeField] private Dropdown failureMode;
        [SerializeField] private Button requestButton;
        [SerializeField] private Text stateText, resultText;
        private LocalHttpServer server;
        private int serial;
        private float started, elapsed;
        private string state = "选择一种响应后发送";

        private async void Start()
        {
            try
            {
                await UniTask.NextFrame(destroyCancellationToken);
                server = new LocalHttpServer(new byte[0]);
                QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
                requestButton.onClick.AddListener(Request);
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested) { }
        }

        private void Request()
        {
            if (server == null || serial != 0) return;
            // Timeout 在入队时保存；超时从代理开始执行请求计时，不包括排队时间。
            float previous = QGF.WebRequest.Timeout;
            try
            {
                QGF.WebRequest.Timeout = failureMode.value == 1 ? 1f : previous;
                string path = failureMode.value == 0 ? "/missing" : failureMode.value == 1 ? "/hold" : "/ok";
                serial = QGF.WebRequest.AddWebRequest(server.Url(path), this);
            }
            finally { QGF.WebRequest.Timeout = previous; } // 本次任务保留自己的超时值，恢复全局默认值。
            started = Time.realtimeSinceStartup;
            resultText.text = failureMode.value == 1 ? "服务器延迟响应；本次超时设为 1 秒。" : "等待响应……";
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            if (args is WebRequestFailureEventArgs failure && failure.SerialId == serial)
            {
                state = "失败";
                resultText.text = failure.ErrorMessage + "\n任务已结束，可以重试或改发正常请求。";
            }
            else if (args is WebRequestSuccessEventArgs success && success.SerialId == serial)
            {
                state = "完成";
                resultText.text = "服务器已响应；之前的失败不会阻塞后续请求。";
            }
            else return;
            // 失败分支也要结束业务等待，再由界面决定是否重试。
            elapsed = Time.realtimeSinceStartup - started;
            serial = 0;
        }

        private void LateUpdate()
        {
            stateText.text = serial == 0 ? $"{state} · {elapsed:F2} 秒" : $"请求 #{serial} · 等待 {Time.realtimeSinceStartup - started:F2} 秒";
            requestButton.interactable = server != null && serial == 0;
        }

        private void OnDestroy()
        {
            var web = GameEntry.GetComponent<WebRequestComponent>();
            if (web != null && serial != 0) web.RemoveWebRequest(serial);
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(WebRequestSuccessEventArgs.EventId, OnResult)) events.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                if (events.Check(WebRequestFailureEventArgs.EventId, OnResult)) events.Unsubscribe(WebRequestFailureEventArgs.EventId, OnResult);
            }
            server?.Dispose();
        }
    }
}
