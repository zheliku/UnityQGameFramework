//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 日志记录结点。
        /// </summary>
        public sealed class LogNode : IReference
        {
            /// <summary>
            /// 初始化日志记录结点的新实例。
            /// </summary>
            public LogNode()
            {
                LogTime = default(DateTime);
                LogFrameCount = 0;
                LogType = LogType.Error;
                LogMessage = null;
                StackTrack = null;
            }

            /// <summary>
            /// 获取日志时间。
            /// </summary>
            public DateTime LogTime { get; private set; }

            /// <summary>
            /// 获取日志帧计数。
            /// </summary>
            public int LogFrameCount { get; private set; }

            /// <summary>
            /// 获取日志类型。
            /// </summary>
            public LogType LogType { get; private set; }

            /// <summary>
            /// 获取日志内容。
            /// </summary>
            public string LogMessage { get; private set; }

            /// <summary>
            /// 获取日志堆栈信息。
            /// </summary>
            public string StackTrack { get; private set; }

            /// <summary>
            /// 创建日志记录结点。
            /// </summary>
            /// <param name="logType">日志类型。</param>
            /// <param name="logMessage">日志内容。</param>
            /// <param name="stackTrack">日志堆栈信息。</param>
            /// <returns>创建的日志记录结点。</returns>
            public static LogNode Create(LogType logType, string logMessage, string stackTrack)
            {
                LogNode logNode = ReferencePool.Acquire<LogNode>();
                logNode.LogTime = DateTime.UtcNow;
                logNode.LogFrameCount = Time.frameCount;
                logNode.LogType = logType;
                logNode.LogMessage = logMessage;
                logNode.StackTrack = stackTrack;
                return logNode;
            }

            /// <summary>
            /// 清理日志记录结点。
            /// </summary>
            public void Clear()
            {
                LogTime = default(DateTime);
                LogFrameCount = 0;
                LogType = LogType.Error;
                LogMessage = null;
                StackTrack = null;
            }
        }
    }
}
