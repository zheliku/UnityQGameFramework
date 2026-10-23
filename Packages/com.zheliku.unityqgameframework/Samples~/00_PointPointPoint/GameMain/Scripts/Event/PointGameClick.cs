using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    public sealed class PointGameClick : GameEventArgs
    {
        public static readonly int EventId = typeof(PointGameClick).GetHashCode();
        public override int Id => EventId;
        public bool Hit { get; private set; }

        public static PointGameClick Create(bool hit)
        {
            var args = ReferencePool.Acquire<PointGameClick>();
            args.Hit = hit;
            return args;
        }

        // Fire 后由事件模块归还参数，发送方不能再 Release 或继续使用它。
        public override void Clear() => Hit = false;
    }
}
