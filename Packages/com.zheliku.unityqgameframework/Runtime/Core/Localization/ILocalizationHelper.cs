//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 本地化辅助器接口。
    /// </summary>
    public interface ILocalizationHelper
    {
        /// <summary>
        /// 解析指定语言的字典，不修改当前语言。
        /// </summary>
        /// <param name="text">字典文本。</param>
        /// <param name="language">标准语言标识。</param>
        /// <returns>解析后的字典；调用方不修改辅助器返回的集合。</returns>
        IReadOnlyDictionary<string, string> ParseDictionary(string text, Language language);

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        Language SystemLanguage
        {
            get;
        }
    }
}
