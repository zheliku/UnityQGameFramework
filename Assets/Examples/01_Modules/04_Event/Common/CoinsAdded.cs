using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 携带金币增量的示例事件。
    /// </summary>
    public sealed class CoinsAdded : GameEventArgs
    {
        /// <summary>
        /// 事件编号。
        /// </summary>
        public static readonly int EventId = typeof(CoinsAdded).GetHashCode();

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
        public int Amount { get; private set; }

        /// <summary>
        /// 从引用池获取并填写事件参数。
        /// </summary>
        /// <param name="amount">本次数量变化。</param>
        /// <returns>携带金币增量的事件参数，派发后由事件模块归还引用池。</returns>
        public static CoinsAdded Create(int amount)
        {
            // 消息参数使用引用池；Event 派发完成后会调用 Clear 并归还。
            var args = ReferencePool.Acquire<CoinsAdded>();
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
