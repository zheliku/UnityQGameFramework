using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    /// <summary>
    /// 点点点的命中或失误事件。
    /// </summary>
    public sealed class PointGameClick : GameEventArgs
    {
        /// <summary>
        /// 事件编号。
        /// </summary>
        public static readonly int EventId = typeof(PointGameClick).GetHashCode();

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
        /// 获取本次是否命中目标。
        /// </summary>
        public bool Hit
        {
            get; private set;
        }

        /// <summary>
        /// 从引用池获取并填写事件参数。
        /// </summary>
        /// <param name="hit">本次是否命中。</param>
        /// <returns>携带命中结果的事件参数，派发后由事件模块归还引用池。</returns>
        public static PointGameClick Create(bool hit)
        {
            var args = ReferencePool.Acquire<PointGameClick>();
            args.Hit = hit;
            return args;
        }

        // Fire 后由事件模块归还参数，发送方不能再 Release 或继续使用它。
        /// <summary>
        /// 重置数据，供引用池复用。
        /// </summary>
        public override void Clear()
        {
            Hit = false;
        }
    }
}
