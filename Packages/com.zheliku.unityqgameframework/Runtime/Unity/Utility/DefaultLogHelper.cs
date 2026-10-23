//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : FrameworkLog.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(FrameworkLogLevel level, object message)
        {
            switch (level)
            {
                case FrameworkLogLevel.Debug:
                    Debug.Log(Utility.Text.Format("<color=#888888>{0}</color>", message));
                    break;

                case FrameworkLogLevel.Info:
                    Debug.Log(message.ToString());
                    break;

                case FrameworkLogLevel.Warning:
                    Debug.LogWarning(message.ToString());
                    break;

                case FrameworkLogLevel.Error:
                    if (message is Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                    else
                    {
                        Debug.LogError(message.ToString());
                    }
                    break;

                default:
                    throw new FrameworkException(message.ToString());
            }
        }
    }
}
