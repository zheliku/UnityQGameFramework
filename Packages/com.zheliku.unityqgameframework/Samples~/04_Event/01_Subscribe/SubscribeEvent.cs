using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SubscribeEvent : MonoBehaviour
    {
        [SerializeField] private Text coinsText;
        [SerializeField] private InputField amountInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Text listenersText;
        [SerializeField] private Toggle listenToggle;
        private bool viewReady;
        private int total, sent, received;
        private bool fromThisExample;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;

                // 事件组件持有回调委托；离开流程或销毁对象时解除订阅。
                QGF.Event.Subscribe(CoinsAdded.EventId, OnAdded);
                listenToggle.SetIsOnWithoutNotify(true);
                listenToggle.onValueChanged.AddListener(listening =>
                {
                    // Check 检查同一个委托。停止监听后仍可发送，但不会再累计金币。
                    bool subscribed = QGF.Event.Check(CoinsAdded.EventId, OnAdded);
                    if (listening && !subscribed) QGF.Event.Subscribe(CoinsAdded.EventId, OnAdded);
                    if (!listening && subscribed) QGF.Event.Unsubscribe(CoinsAdded.EventId, OnAdded);
                });
                sendButton.onClick.AddListener(() =>
                {
                    int.TryParse(amountInput.text, out int amount);
                    amount = Mathf.Clamp(amount, 1, 100);
                    amountInput.SetTextWithoutNotify(amount.ToString());
                    sent++;
                    // this 是发送者，事件编号在参数的 Id 中；派发后由模块回收参数。
                    QGF.Event.Fire(this, CoinsAdded.Create(amount));
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnAdded(object sender, GameEventArgs args)
        {
            // 只保存业务数据；回调结束后不能继续持有这份池化参数。
            fromThisExample = ReferenceEquals(sender, this);
            total += ((CoinsAdded)args).Amount;
            received++;
        }

        private void Update()
        {
            if (!viewReady) return;
            coinsText.text = $"金币：{total}";
            // Count 按事件编号统计接收函数。
            listenersText.text = $"发送：{sent} 次    接收：{received} 次\n接收函数：{QGF.Event.Count(CoinsAdded.EventId)}\nSender 是当前对象：{fromThisExample}";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(CoinsAdded.EventId, OnAdded)) events.Unsubscribe(CoinsAdded.EventId, OnAdded);
        }
    }

    public sealed class CoinsAdded : GameEventArgs
    {
        public static readonly int EventId = typeof(CoinsAdded).GetHashCode();
        public override int Id => EventId;
        public int Amount { get; private set; }

        public static CoinsAdded Create(int amount)
        {
            var args = ReferencePool.Acquire<CoinsAdded>();
            args.Amount = amount;
            return args;
        }

        public override void Clear() => Amount = 0;
    }
}
