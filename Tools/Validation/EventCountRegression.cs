using System;
using System.Collections.Generic;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

// 纯 Core 回归，不依赖 Unity 或游戏场景。
public static class EventCountRegression
{
    public static void Main()
    {
        var events = FrameworkEntry.GetModule<IEventManager>();
        var order = new List<string>();
        EventHandler<GameEventArgs> first = (_, _) => order.Add("A");
        EventHandler<GameEventArgs> second = null;
        second = (_, _) =>
        {
            order.Add("B");
            events.Unsubscribe(1, second);
        };
        EventHandler<GameEventArgs> third = (_, _) => order.Add("C");
        try
        {
            events.Subscribe(1, first);
            events.Subscribe(1, second);
            events.Subscribe(2, third);
            Require(events.Count(1) == 2 && events.EventHandlerCount == 3, "Count handlers across event IDs");

            bool duplicateRejected = false;
            try { events.Subscribe(1, first); }
            catch (FrameworkException) { duplicateRejected = true; }
            Require(duplicateRejected && events.EventHandlerCount == 3, "Rejected duplicate preserves count");

            events.FireNow(null, Message.Create(1));
            Require(string.Join("", order) == "AB" && events.Count(1) == 1 && events.EventHandlerCount == 2,
                "Self-unsubscribe preserves dispatch and updates count");
            Require(Message.Recycled == 1, "Immediate message returned once");

            events.Fire(null, Message.Create(1));
            Require(events.EventCount == 1 && order.Count == 2, "Queued message is deferred");
            FrameworkEntry.Update(.1f, .1f);
            Require(events.EventCount == 0 && string.Join("", order) == "ABA" && Message.Recycled == 2, "Queued dispatch and recycling");

            int fallback = 0;
            events.SetDefaultHandler((_, _) => fallback++);
            events.FireNow(null, Message.Create(99));
            Require(fallback == 1 && events.EventHandlerCount == 2 && Message.Recycled == 3, "Default is not a normal subscriber");
            events.SetDefaultHandler(null);
            events.FireNow(null, Message.Create(99));
            Require(fallback == 1 && Message.Recycled == 4, "Unmatched message still recycled");

            events.Unsubscribe(1, first);
            events.Unsubscribe(2, third);
            Require(events.EventHandlerCount == 0 && events.Count(1) == 0 && events.Count(2) == 0, "Final unsubscribe clears count");
            Console.WriteLine("PASS: total handlers, duplicate rejection, self-unsubscribe, queue, fallback and recycling");
        }
        finally { FrameworkEntry.Shutdown(); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private sealed class Message : GameEventArgs
    {
        private int id;
        public static int Recycled;
        public override int Id => id;
        public static Message Create(int id)
        {
            var value = ReferencePool.Acquire<Message>();
            value.id = id;
            return value;
        }
        public override void Clear() { id = 0; Recycled++; }
    }
}
