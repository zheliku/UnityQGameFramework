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
            public Event()
            {
                Sender = null;
                EventArgs = null;
            }

            public object Sender { get; private set; }

            public T EventArgs { get; private set; }

            public static Event Create(object sender, T e)
            {
                Event eventNode = ReferencePool.Acquire<Event>();
                eventNode.Sender = sender;
                eventNode.EventArgs = e;
                return eventNode;
            }

            public void Clear()
            {
                Sender = null;
                EventArgs = null;
            }
        }
    }
}
