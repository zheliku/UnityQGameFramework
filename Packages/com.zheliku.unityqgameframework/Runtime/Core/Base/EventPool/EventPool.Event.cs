//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class Event : IReference
        {
            /// <summary>
            /// 初始化待派发事件。
            /// </summary>
            public Event()
            {
                Sender = null;
                EventArgs = null;
            }

            /// <summary>
            /// 获取事件发送者。
            /// </summary>
            public object Sender { get; private set; }

            /// <summary>
            /// 获取事件参数。
            /// </summary>
            public T EventArgs { get; private set; }

            /// <summary>
            /// 从引用池获取待派发事件。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            /// <returns>封装发送者和事件参数的待派发事件。</returns>
            public static Event Create(object sender, T e)
            {
                Event eventNode = ReferencePool.Acquire<Event>();
                eventNode.Sender = sender;
                eventNode.EventArgs = e;
                return eventNode;
            }

            /// <summary>
            /// 清理事件发送者和事件参数。
            /// </summary>
            public void Clear()
            {
                Sender = null;
                EventArgs = null;
            }
        }
    }
}
