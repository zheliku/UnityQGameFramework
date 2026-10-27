using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示POST 表单。
    /// </summary>
    public sealed class PostWebRequest : MonoBehaviour
    {
        [SerializeField]
        private InputField messageInput;
        [SerializeField]
        private Button formButton;
        [SerializeField]
        private Text responseText;
        private LocalHttpServer server;
        private int serial;
        private void Start()
        {
            server = new LocalHttpServer(new byte[0]); // 本机服务回显 message 字段。
            QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnResult);
            formButton.onClick.AddListener(() =>
            {
                var form = new WWWForm();
                form.AddField("message", messageInput.text, Encoding.UTF8);
                // WWWForm 处理字段编码；AddWebRequest 提交 POST 并返回任务序号。
                serial = QGF.WebRequest.AddWebRequest(server.Url("/echo"), form);
                responseText.text = "等待服务器回显";
            });
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            if (args is WebRequestSuccessEventArgs success && success.SerialId == serial)
            {
                responseText.text = Encoding.UTF8.GetString(success.GetWebResponseBytes());
            }
            else if (args is WebRequestFailureEventArgs failure && failure.SerialId == serial)
            {
                responseText.text = "失败：" + failure.ErrorMessage;
            }
            else
            {
                return;
            }

            serial = 0; // 只取出结果，不保存随后回池的事件参数。
        }

        private void Update()
        {
            formButton.interactable = serial == 0;
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
