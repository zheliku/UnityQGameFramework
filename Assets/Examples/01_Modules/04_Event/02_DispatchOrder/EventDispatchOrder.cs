using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示立即派发与排队。
    /// </summary>
    public sealed class EventDispatchOrder : MonoBehaviour
    {
        [SerializeField]
        private Button compareButton;
        [SerializeField]
        private Text orderText;
        [SerializeField]
        private Text queueText;
        private bool viewReady;
        private int sequence, immediate, afterImmediate, queued, afterQueued, waiting;
        private void Start()
        {
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

        private void OnAdded(object sender, GameEventArgs args)
        {
            if (((EventDispatchOrderArgs)args).Amount == 1)
            {
                immediate = ++sequence;
            }
            else
            {
                queued = ++sequence;
            }
        }

        private void Update()
        {
            if (!viewReady)
            {
                return;
            }

            orderText.text = $"FireNow callback: step {immediate}\nAfter FireNow returns: step {afterImmediate}\n" + $"After Fire returns: step {afterQueued}\nQueued callback: step {queued}";
            queueText.text = $"Queued when submitted: {waiting} entries\nCurrently queued: {QGF.Event.EventCount} entries";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(EventDispatchOrderArgs.EventId, OnAdded))
            {
                events.Unsubscribe(EventDispatchOrderArgs.EventId, OnAdded);
            }
        }
    }

    /// <summary>
    /// 携带数量的事件派发顺序测试数据。
    /// </summary>
    public sealed class EventDispatchOrderArgs : GameEventArgs
    {
        /// <summary>
        /// 事件编号。
        /// </summary>
        public static readonly int EventId = typeof(EventDispatchOrderArgs).GetHashCode();

        /// <summary>
        /// 获取事件编号。
        /// </summary>
        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        /// <summary>
        /// 获取本次事件携带的数量。
        /// </summary>
        public int Amount
        {
            get; private set;
        }

        /// <summary>
        /// 从引用池获取并填写事件参数。
        /// </summary>
        /// <param name="amount">本次数量变化。</param>
        /// <returns>携带数量的事件参数，派发后由事件模块归还引用池。</returns>
        public static EventDispatchOrderArgs Create(int amount)
        {
            var args = ReferencePool.Acquire<EventDispatchOrderArgs>();
            args.Amount = amount;
            return args;
        }

        /// <summary>
        /// 重置数据，供引用池复用。
        /// </summary>
        public override void Clear()
        {
            Amount = 0;
        }
    }
}
