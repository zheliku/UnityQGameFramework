using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class MultipleEventHandlers : MonoBehaviour
    {
        [SerializeField] private Button sendButton;
        [SerializeField] private Toggle listenBToggle;
        [SerializeField] private Toggle onceToggle;
        [SerializeField] private Text countsText;
        private bool viewReady;
        private int receiverA, receiverB;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;

                // 事件组件持有回调委托；离开流程或销毁对象时解除订阅。
                QGF.Event.Subscribe(MultipleReceiverMessage.EventId, OnReceiverA);
                QGF.Event.Subscribe(MultipleReceiverMessage.EventId, OnReceiverB);
                // FireNow 立即派发；选中“一次”时，B 收到本条消息后会退订。
                sendButton.onClick.AddListener(() => QGF.Event.FireNow(this, MultipleReceiverMessage.Create(1)));
                listenBToggle.onValueChanged.AddListener(value =>
                {
                    bool listening = QGF.Event.Check(MultipleReceiverMessage.EventId, OnReceiverB);
                    if (value && !listening) QGF.Event.Subscribe(MultipleReceiverMessage.EventId, OnReceiverB);
                    if (!value && listening) QGF.Event.Unsubscribe(MultipleReceiverMessage.EventId, OnReceiverB);
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnReceiverA(object sender, GameEventArgs args) => receiverA++;

        private void OnReceiverB(object sender, GameEventArgs args)
        {
            receiverB++;
            // 可在回调中取消自己，当前消息的其他接收者仍会继续执行。
            if (onceToggle.isOn) QGF.Event.Unsubscribe(MultipleReceiverMessage.EventId, OnReceiverB);
        }

        private void Update()
        {
            if (!viewReady) return;
            // Check 检查这一对事件编号和委托，防止重复订阅或重复解除。
            listenBToggle.SetIsOnWithoutNotify(QGF.Event.Check(MultipleReceiverMessage.EventId, OnReceiverB));
            countsText.text = $"接收者 A：{receiverA} 次\n接收者 B：{receiverB} 次\n"
                + $"本事件处理器：{QGF.Event.Count(MultipleReceiverMessage.EventId)}\n全部处理器：{QGF.Event.EventHandlerCount}";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;
            if (events.Check(MultipleReceiverMessage.EventId, OnReceiverA)) events.Unsubscribe(MultipleReceiverMessage.EventId, OnReceiverA);
            if (events.Check(MultipleReceiverMessage.EventId, OnReceiverB)) events.Unsubscribe(MultipleReceiverMessage.EventId, OnReceiverB);
        }
    }

    public sealed class MultipleReceiverMessage : GameEventArgs
    {
        public static readonly int EventId = typeof(MultipleReceiverMessage).GetHashCode();
        public override int Id => EventId;
        public int Amount { get; private set; }

        public static MultipleReceiverMessage Create(int amount)
        {
            var args = ReferencePool.Acquire<MultipleReceiverMessage>();
            args.Amount = amount;
            return args;
        }

        public override void Clear() => Amount = 0;
    }
}
