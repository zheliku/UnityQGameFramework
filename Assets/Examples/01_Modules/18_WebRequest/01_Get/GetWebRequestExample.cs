using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// GET 返回字节数据，在成功回调中解码为 JSON 文本。
    /// </summary>
    public sealed class GetWebRequestExample : MonoBehaviour
    {
        [SerializeField]
        private Button requestButton;
        [SerializeField]
        private Text stateText, responseText;
        [SerializeField]
        private TextAsset response;
        private LocalHttpServer server;
        private int serial;
        private float started, elapsed;
        private string state = "尚未请求";
        private void Start()
        {
            server = new LocalHttpServer(response.bytes);
            QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
            requestButton.onClick.AddListener(Request);
        }

        private void Request()
        {
            if (server == null || serial != 0)
            {
                return;
            }

            string uri = server.Url("/status.json");
            // 返回任务序号；结果事件用它确认本次请求。
            serial = QGF.WebRequest.AddWebRequest(uri);
            started = Time.realtimeSinceStartup;
            responseText.text = "等待响应正文……";
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            // 事件是全局的；没传 UserData 时，也能用任务序号识别自己的结果。
            if (args is WebRequestSuccessEventArgs success && success.SerialId == serial)
            {
                byte[] bytes = success.GetWebResponseBytes();
                responseText.text = Encoding.UTF8.GetString(bytes).Trim();
                state = $"完成 · {bytes.Length} 字节";
            }
            else if (args is WebRequestFailureEventArgs failure && failure.SerialId == serial)
            {
                state = "失败：" + failure.ErrorMessage;
            }
            else
            {
                return;
            }

            // 回调中取出所需值，事件参数随后回池。
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
            if (web != null && serial != 0)
            {
                web.RemoveWebRequest(serial);
            }

            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
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
