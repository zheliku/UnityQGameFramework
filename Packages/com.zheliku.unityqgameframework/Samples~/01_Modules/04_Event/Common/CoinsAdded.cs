using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class CoinsAdded : GameEventArgs
    {
        public static readonly int EventId = typeof(CoinsAdded).GetHashCode();
        public override int Id => EventId;
        public int Amount { get; private set; }

        public static CoinsAdded Create(int amount)
        {
            // 消息参数使用引用池；Event 派发完成后会调用 Clear 并归还。
            var args = ReferencePool.Acquire<CoinsAdded>();
            args.Amount = amount;
            return args;
        }

        public override void Clear() => Amount = 0;
    }
}
