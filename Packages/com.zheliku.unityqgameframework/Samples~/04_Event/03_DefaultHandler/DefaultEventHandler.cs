using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class DefaultEventHandler : MonoBehaviour
    {
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button sendButton;
        [SerializeField] private Text countsText;
        private bool viewReady;
        private int sent, received;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;

                // 只有没有匹配订阅者的消息才交给默认处理器。
                QGF.Event.SetDefaultHandler(OnUnhandled);
                enabledToggle.onValueChanged.AddListener(value =>
                    QGF.Event.SetDefaultHandler(value ? OnUnhandled : null));
                sendButton.onClick.AddListener(() =>
                {
                    sent++;
                    // 同步派发：调用返回前，接收函数已经执行。参数随后由事件模块回收。
                    QGF.Event.FireNow(this, DefaultNotification.Create(sent));
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnUnhandled(object sender, GameEventArgs args) => received++;

        private void Update()
        {
            if (!viewReady) return;
            countsText.text = $"发送：{sent} 次\n默认处理器接收：{received} 次\n"
                // Count 按事件编号统计接收函数。
                + $"该事件的普通订阅者：{QGF.Event.Count(DefaultNotification.EventId)}";
        }

        private void OnDestroy()
        {
            // 退出时清空默认处理器，避免 EventComponent 保留本场景实例。
            if (viewReady) GameEntry.GetComponent<EventComponent>()?.SetDefaultHandler(null);
        }
    }

    public sealed class DefaultNotification : GameEventArgs
    {
        public static readonly int EventId = typeof(DefaultNotification).GetHashCode();
        public override int Id => EventId;
        public int Amount { get; private set; }

        public static DefaultNotification Create(int amount)
        {
            var args = ReferencePool.Acquire<DefaultNotification>();
            args.Amount = amount;
            return args;
        }

        public override void Clear() => Amount = 0;
    }
}
