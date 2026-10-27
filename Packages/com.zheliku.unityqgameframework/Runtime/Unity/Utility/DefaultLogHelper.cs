//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Globalization;
using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : FrameworkLog.ILogHelper
    {
#if UNITY_EDITOR
        /// <summary>
        /// 获取或设置编辑器日志的格式化方法。
        /// </summary>
        public static Func<FrameworkLogLevel, string, string> EditorLogFormatter { get; set; }
#endif

        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(FrameworkLogLevel level, object message)
        {
            if (level == FrameworkLogLevel.Fatal)
            {
                throw new FrameworkException(message == null ? "null" : message.ToString(), message as Exception);
            }
            if (level == FrameworkLogLevel.Error && message is Exception exception)
            {
                // Unity 直接记录原异常，保留异常类型、抛出位置和 InnerException。
                Debug.LogException(exception);
                return;
            }
            string content = message == null ? "null" : message.ToString();
            string text;
#if UNITY_EDITOR
            // 编辑器时间由 Console 展示；格式化器只补充等级和调用位置。
            text = EditorLogFormatter != null ? EditorLogFormatter(level, content)
                : "[QGF] " + level.ToString().ToUpperInvariant() + " | " + content;
#else
            string time = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            text = "[QGF] " + time + " | " + level.ToString().ToUpperInvariant() + " | " + content;
#endif
            switch (level)
            {
                case FrameworkLogLevel.Debug:
                case FrameworkLogLevel.Info:
                    Debug.Log(text);
                    break;

                case FrameworkLogLevel.Warning:
                    Debug.LogWarning(text);
                    break;

                case FrameworkLogLevel.Error:
                    Debug.LogError(text);
                    break;

                default:
                    throw new FrameworkException(content, message as Exception);
            }
        }
    }
}
