using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 字典和语言资源均已就绪的语言切换事件。
    /// </summary>
    public sealed class LanguageChangedEventArgs : GameEventArgs
    {
        /// <summary>
        /// 语言切换事件编号。
        /// </summary>
        public static readonly int EventId = typeof(LanguageChangedEventArgs).GetHashCode();

        /// <summary>
        /// 获取语言切换事件编号。
        /// </summary>
        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        /// <summary>
        /// 获取切换前的语言标识。
        /// </summary>
        public Language PreviousLanguage
        {
            get; private set;
        }

        /// <summary>
        /// 获取当前语言标识。
        /// </summary>
        public Language Language
        {
            get; private set;
        }

        /// <summary>
        /// 从 Core 通知创建运行时事件。
        /// </summary>
        /// <param name="args">Core 语言切换通知。</param>
        /// <returns>分发后由事件模块归还的事件。</returns>
        public static LanguageChangedEventArgs Create(Core.Events.LanguageChangedEventArgs args)
        {
            LanguageChangedEventArgs result = ReferencePool.Acquire<LanguageChangedEventArgs>();
            result.PreviousLanguage = args.PreviousLanguage;
            result.Language = args.Language;
            return result;
        }

        /// <summary>
        /// 清理语言标识。
        /// </summary>
        public override void Clear()
        {
            PreviousLanguage = Language.Unspecified;
            Language = Language.Unspecified;
        }
    }
}
