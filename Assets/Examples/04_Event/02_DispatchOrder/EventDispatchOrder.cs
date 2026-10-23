using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class EventDispatchOrder : MonoBehaviour
    {
        [SerializeField] private Button compareButton;
        [SerializeField] private Text orderText;
        [SerializeField] private Text queueText;
        private bool viewReady;
        private int sequence, immediate, afterImmediate, queued, afterQueued, waiting;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;

                // 事件组件持有回调委托；离开流程或销毁对象时解除订阅。
                QGF.Event.Subscribe(EventDispatchOrderArgs.EventId, OnAdded);
                compareButton.onClick.AddListener(() =>
                {
                    sequence = immediate = queued = 0;
                    // FireNow 在当前调用中完成，返回后序号已经增加。
                    QGF.Event.FireNow(this, EventDispatchOrderArgs.Create(1));
                    afterImmediate = ++sequence;
                    // Fire 等模块轮询队列；是否跨帧取决于本帧的调用顺序。
                    QGF.Event.Fire(this, EventDispatchOrderArgs.Create(2));
                    afterQueued = ++sequence;
                    // 队列数量随框架 Update 改变；需要比较提交瞬间时应保存快照。
                    waiting = QGF.Event.EventCount;
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnAdded(object sender, GameEventArgs args)
        {
            if (((EventDispatchOrderArgs)args).Amount == 1) immediate = ++sequence;
            else queued = ++sequence;
        }

        private void Update()
        {
            if (!viewReady) return;
            orderText.text = $"FireNow 的回调：第 {immediate} 步\nFireNow 返回后：第 {afterImmediate} 步\n"
                + $"Fire 返回后：第 {afterQueued} 步\n排队消息的回调：第 {queued} 步";
            queueText.text = $"提交时排队：{waiting} 条\n当前排队：{QGF.Event.EventCount} 条";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(EventDispatchOrderArgs.EventId, OnAdded)) events.Unsubscribe(EventDispatchOrderArgs.EventId, OnAdded);
        }
    }

    public sealed class EventDispatchOrderArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(EventDispatchOrderArgs).GetHashCode();
        public override int Id => EventId;
        public int Amount { get; private set; }

        public static EventDispatchOrderArgs Create(int amount)
        {
            var args = ReferencePool.Acquire<EventDispatchOrderArgs>();
            args.Amount = amount;
            return args;
        }

        public override void Clear() => Amount = 0;
    }
}
