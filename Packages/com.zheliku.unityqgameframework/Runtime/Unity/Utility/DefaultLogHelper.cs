//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Globalization;
using UnityQGameFramework.Core;
using UnityEngine;
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using UnityEditor;
using Debug = UnityEngine.Debug;
#endif

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 默认游戏框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : GameFrameworkLog.ILogHelper
    {
#if UNITY_EDITOR
        private static volatile bool s_DarkTheme;

        /// <summary>
        /// 安装编辑器日志辅助器。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            // 编辑器菜单早于场景运行；BaseComponent 启动时仍可设置自己的辅助器。
            s_DarkTheme = EditorGUIUtility.isProSkin;
            GameFrameworkLog.SetLogHelper(new DefaultLogHelper());
            EditorApplication.update += UpdateTheme;
        }

        /// <summary>
        /// 缓存主题状态，供工作线程读取。
        /// </summary>
        private static void UpdateTheme()
        {
            s_DarkTheme = EditorGUIUtility.isProSkin;
        }

        /// <summary>
        /// 补充等级标记和业务调用位置。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志正文。</param>
        /// <returns>供 Unity Console 显示的富文本日志。</returns>
        private static string FormatEditorLog(GameFrameworkLogLevel level, string message)
        {
            string color;
            switch (level)
            {
                case GameFrameworkLogLevel.Debug:
                    color = s_DarkTheme ? "#D6E7F5" : "#5B7185";
                    break;
                case GameFrameworkLogLevel.Info:
                    color = s_DarkTheme ? "#64F0D8" : "#008F83";
                    break;
                case GameFrameworkLogLevel.Warning:
                    color = s_DarkTheme ? "#FFD166" : "#A86A00";
                    break;
                default:
                    color = s_DarkTheme ? "#FF8178" : "#D13E4F";
                    break;
            }

            // 仅等级标记使用主题色；调用位置和正文沿用 Console 的文字及选中颜色。
            string prefix = "[QGF] <color=" + color + ">● <b>" + level.ToString().ToUpperInvariant() + "</b></color>";
            StackFrame caller = GetCaller();
            if (caller != null)
            {
                string name = GetCallerName(caller.GetMethod());
                string path = caller.GetFileName();
                int line = caller.GetFileLineNumber();
                if (!string.IsNullOrEmpty(path) && line > 0)
                {
                    prefix += " | " + SecurityElement.Escape(name) + " <a href=\"" + path.Replace('\\', '/')
                        + "\" line=\"" + line + "\">" + SecurityElement.Escape(Path.GetFileName(path)) + ":" + line + "</a>";
                }
                else
                {
                    prefix += " | " + SecurityElement.Escape(name);
                }
            }

            return prefix + " | " + message;
        }

        /// <summary>
        /// 跳过日志封装，查找业务调用帧。
        /// </summary>
        /// <returns>业务调用帧；无法取得调用栈时返回空。</returns>
        private static StackFrame GetCaller()
        {
            StackFrame[] frames = new StackTrace(2, true).GetFrames();
            if (frames == null)
            {
                return null;
            }

            foreach (StackFrame frame in frames)
            {
                Type type = frame.GetMethod()?.DeclaringType;
                if (type == null || type == typeof(GameFrameworkLog) || type == typeof(Log) || typeof(DefaultLogHelper).IsAssignableFrom(type))
                {
                    continue;
                }

                return frame;
            }

            return null;
        }

        /// <summary>
        /// 还原异步状态机和闭包所属的方法。
        /// </summary>
        /// <param name="method">调用帧对应的方法。</param>
        /// <returns>类型名和方法名。</returns>
        private static string GetCallerName(MethodBase method)
        {
            Type type = method.DeclaringType;
            string name = method.Name;
            if (type.Name.StartsWith("<", StringComparison.Ordinal) && type.DeclaringType != null)
            {
                int end = type.Name.IndexOf('>');
                if (end > 1)
                {
                    name = type.Name.Substring(1, end - 1);
                }

                type = type.DeclaringType;
            }

            if (name.StartsWith("<", StringComparison.Ordinal))
            {
                int end = name.IndexOf('>');
                if (end > 1)
                {
                    name = name.Substring(1, end - 1);
                }
            }

            return type.Name + "." + name;
        }
#endif

        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(GameFrameworkLogLevel level, object message)
        {
            if (level == GameFrameworkLogLevel.Fatal)
            {
                throw new GameFrameworkException(message == null ? "null" : message.ToString(), message as Exception);
            }
            if (level == GameFrameworkLogLevel.Error && message is Exception exception)
            {
                // Unity 直接记录原异常，保留异常类型、抛出位置和 InnerException。
                Debug.LogException(exception);
                return;
            }
            string content = message == null ? "null" : message.ToString();
            string text;
#if UNITY_EDITOR
            // 编辑器时间由 Console 展示；格式化器只补充等级和调用位置。
            text = FormatEditorLog(level, content);
#else
            string time = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            text = "[QGF] " + time + " | " + level.ToString().ToUpperInvariant() + " | " + content;
#endif
            switch (level)
            {
                case GameFrameworkLogLevel.Debug:
                case GameFrameworkLogLevel.Info:
                    Debug.Log(text);
                    break;

                case GameFrameworkLogLevel.Warning:
                    Debug.LogWarning(text);
                    break;

                case GameFrameworkLogLevel.Error:
                    Debug.LogError(text);
                    break;

                default:
                    throw new GameFrameworkException(content, message as Exception);
            }
        }
    }
}
