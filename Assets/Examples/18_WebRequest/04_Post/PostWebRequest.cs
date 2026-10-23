using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    /// <summary>比较原始字节和 WWWForm；服务器回显实际收到的请求体。</summary>
    public sealed class PostWebRequest : MonoBehaviour
    {
        [SerializeField] private InputField messageInput;
        [SerializeField] private Dropdown requestMode;
        [SerializeField] private Toggle slowToggle;
        [SerializeField] private Button bytesButton, formButton, cancelButton;
        [SerializeField] private Text stateText, responseText, requestText, contextText;
        private LocalHttpServer server;
        private int serial, completed;
        private string state = "编辑内容，再选择一种 POST";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                server = new LocalHttpServer(new byte[0]);
                QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
                QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
                bytesButton.onClick.AddListener(PostBytes);
                formButton.onClick.AddListener(PostForm);
                cancelButton.onClick.AddListener(() =>
                {
                    // 用序号取消自己的请求；没有 tag 的重载也适用。取消不会产生结果回调。
                    bool removed = QGF.WebRequest.RemoveWebRequest(serial);
                    serial = 0;
                    state = removed ? "已取消当前请求" : "请求已结束";
                    responseText.text = string.Empty;
                    contextText.text = $"结果回调 {completed} 次，本次取消不增加次数。";
                });
                messageInput.text = "你好 QGF + &= ?";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }

        private void PostBytes()
        {
            if (serial != 0 || server == null) return;
            // byte[] 原样作为请求体，默认 Content-Type 为 application/octet-stream。
            byte[] body = Encoding.UTF8.GetBytes(messageInput.text);
            string uri = server.Url(slowToggle.isOn ? "/echo-slow" : "/echo");
            // 与 GET 一样，tag 用于分组，priority 影响排队顺序，UserData 原样带回业务上下文。
            switch (requestMode.value)
            {
                case 0: serial = QGF.WebRequest.AddWebRequest(uri, body); break;
                case 1: serial = QGF.WebRequest.AddWebRequest(uri, body, "Example.Post"); break;
                case 2: serial = QGF.WebRequest.AddWebRequest(uri, body, 10); break;
                case 3: serial = QGF.WebRequest.AddWebRequest(uri, body, (object)this); break;
                case 4: serial = QGF.WebRequest.AddWebRequest(uri, body, "Example.Post", 10); break;
                case 5: serial = QGF.WebRequest.AddWebRequest(uri, body, "Example.Post", this); break;
                case 6: serial = QGF.WebRequest.AddWebRequest(uri, body, 10, this); break;
                default: serial = QGF.WebRequest.AddWebRequest(uri, body, "Example.Post", 10, this); break;
            }
            var info = QGF.WebRequest.GetWebRequestInfo(serial);
            requestText.text = $"请求 #{serial}\n标签：{info.Tag ?? "未设置"}    优先级：{info.Priority}";
            state = $"发送原始字节 · {body.Length} 字节";
            responseText.text = "等待服务器回显……";
            contextText.text = string.Empty;
        }

        private void PostForm()
        {
            if (serial != 0 || server == null) return;
            // WWWForm 负责字段编码；服务器解码后得到 message 字段的原值。
            var form = new WWWForm();
            form.AddField("message", messageInput.text, Encoding.UTF8);
            string uri = server.Url(slowToggle.isOn ? "/echo-slow" : "/echo");
            // 按同样的八组参数发送表单。
            switch (requestMode.value)
            {
                case 0: serial = QGF.WebRequest.AddWebRequest(uri, form); break;
                case 1: serial = QGF.WebRequest.AddWebRequest(uri, form, "Example.Post"); break;
                case 2: serial = QGF.WebRequest.AddWebRequest(uri, form, 10); break;
                case 3: serial = QGF.WebRequest.AddWebRequest(uri, form, (object)this); break;
                case 4: serial = QGF.WebRequest.AddWebRequest(uri, form, "Example.Post", 10); break;
                case 5: serial = QGF.WebRequest.AddWebRequest(uri, form, "Example.Post", this); break;
                case 6: serial = QGF.WebRequest.AddWebRequest(uri, form, 10, this); break;
                default: serial = QGF.WebRequest.AddWebRequest(uri, form, "Example.Post", 10, this); break;
            }
            var info = QGF.WebRequest.GetWebRequestInfo(serial);
            requestText.text = $"请求 #{serial}\n标签：{info.Tag ?? "未设置"}    优先级：{info.Priority}";
            state = "发送表单字段 message";
            responseText.text = "等待服务器回显……";
            contextText.text = string.Empty;
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            object context;
            // 全局事件先按任务序号筛选；没有传 UserData 时也能识别自己的结果。
            if (args is WebRequestSuccessEventArgs success && success.SerialId == serial)
            {
                responseText.text = Encoding.UTF8.GetString(success.GetWebResponseBytes());
                context = success.UserData;
                state = "已收到服务器回显";
            }
            else if (args is WebRequestFailureEventArgs failure && failure.SerialId == serial)
            {
                context = failure.UserData;
                state = "失败：" + failure.ErrorMessage;
            }
            else return;
            // 事件参数随后回池，只取出界面需要的字符串，不保存 args。
            completed++;
            contextText.text = $"结果回调 {completed} 次\n上下文：{(ReferenceEquals(context, this) ? "当前组件" : context == null ? "null（未传入）" : context.GetType().Name)}";
            serial = 0;
        }

        private void LateUpdate()
        {
            stateText.text = state;
            bytesButton.interactable = formButton.interactable = server != null && serial == 0;
            requestMode.interactable = slowToggle.interactable = messageInput.interactable = serial == 0;
            cancelButton.interactable = serial != 0;
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
