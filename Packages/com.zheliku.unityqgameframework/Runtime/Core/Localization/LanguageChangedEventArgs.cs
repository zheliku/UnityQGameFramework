namespace UnityQGameFramework.Core.Events
{
    /// <summary>
    /// 完整字典和语言标识提交后的通知。
    /// </summary>
    public sealed class LanguageChangedEventArgs : GameFrameworkEventArgs
    {
        /// <summary>
        /// 获取切换前的语言标识；首次提交时可能为空。
        /// </summary>
        public Language PreviousLanguage
        {
            get; private set;
        }

        /// <summary>
        /// 获取已提交的语言标识。
        /// </summary>
        public Language Language
        {
            get; private set;
        }

        /// <summary>
        /// 创建语言切换通知。
        /// </summary>
        /// <param name="previousLanguage">切换前的语言标识。</param>
        /// <param name="language">已提交的语言标识。</param>
        /// <returns>由通知发送方在分发后归还的事件。</returns>
        public static LanguageChangedEventArgs Create(Language previousLanguage, Language language)
        {
            LanguageChangedEventArgs args = ReferencePool.Acquire<LanguageChangedEventArgs>();
            args.PreviousLanguage = previousLanguage;
            args.Language = language;
            return args;
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
