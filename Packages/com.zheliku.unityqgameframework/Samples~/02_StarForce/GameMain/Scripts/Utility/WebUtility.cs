//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;

namespace StarForce
{
    /// <summary>
    /// 转换 URL 中的转义文本。
    /// </summary>
    public static class WebUtility
    {
        /// <summary>
        /// 对文本进行 URL 转义。
        /// </summary>
        /// <param name="stringToEscape">原始文本。</param>
        /// <returns>转义后的文本。</returns>
        public static string EscapeString(string stringToEscape)
        {
            return Uri.EscapeDataString(stringToEscape);
        }

        /// <summary>
        /// 还原 URL 转义文本。
        /// </summary>
        /// <param name="stringToUnescape">转义后的文本。</param>
        /// <returns>还原后的文本。</returns>
        public static string UnescapeString(string stringToUnescape)
        {
            return Uri.UnescapeDataString(stringToUnescape);
        }
    }
}
