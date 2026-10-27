using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 初始化编辑器日志辅助器。
    /// </summary>
    [InitializeOnLoad]
    internal static class LogInitializer
    {
        private static volatile bool s_DarkTheme;

        /// <summary>
        /// 安装 QGF 编辑器日志格式化器。
        /// </summary>
        static LogInitializer()
        {
            // 编辑器菜单可以早于 BaseComponent 执行；运行时仍由 BaseComponent 应用所选辅助器。
            s_DarkTheme = EditorGUIUtility.isProSkin;
            DefaultLogHelper.EditorLogFormatter = Format;
            FrameworkLog.SetLogHelper(new DefaultLogHelper());
            EditorApplication.update += UpdateTheme;
        }

        /// <summary>
        /// 缓存主题状态，供工作线程格式化日志时读取。
        /// </summary>
        private static void UpdateTheme()
        {
            // 工作线程输出日志时只读缓存，不调用编辑器 GUI 接口。
            s_DarkTheme = EditorGUIUtility.isProSkin;
        }

        /// <summary>
        /// 补充等级标记和业务调用位置。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志正文。</param>
        /// <returns>供 Unity Console 显示的富文本日志。</returns>
        private static string Format(FrameworkLogLevel level, string message)
        {
            string color;
            switch (level)
            {
                case FrameworkLogLevel.Debug:
                    color = s_DarkTheme ? "#D6E7F5" : "#5B7185";
                    break;
                case FrameworkLogLevel.Info:
                    color = s_DarkTheme ? "#64F0D8" : "#008F83";
                    break;
                case FrameworkLogLevel.Warning:
                    color = s_DarkTheme ? "#FFD166" : "#A86A00";
                    break;
                default:
                    color = s_DarkTheme ? "#FF8178" : "#D13E4F";
                    break;
            }

            // 等级标记使用主题适配色；调用位置和正文继续沿用 Console 的文字及选中状态颜色。
            string prefix = "[QGF] <color=" + color + ">● <b>" + level.ToString().ToUpperInvariant() + "</b></color>";
            StackFrame caller = GetCaller();
            if (caller != null)
            {
                MethodBase method = caller.GetMethod();
                string name = GetCallerName(method);
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
        /// 跳过 QGF 日志封装，查找实际调用帧。
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
                System.Type type = frame.GetMethod()?.DeclaringType;
                if (type == null || type == typeof(LogInitializer) || type == typeof(FrameworkLog)
                    || type == typeof(Runtime.Log) || typeof(DefaultLogHelper).IsAssignableFrom(type))
                {
                    continue;
                }
                return frame;
            }
            return null;
        }

        /// <summary>
        /// 还原异步状态机和闭包所属的调用方法。
        /// </summary>
        /// <param name="method">调用帧对应的方法。</param>
        /// <returns>类型名和方法名。</returns>
        private static string GetCallerName(MethodBase method)
        {
            System.Type type = method.DeclaringType;
            string name = method.Name;
            // 异步状态机和闭包恢复为业务方法名，避免显示 MoveNext 或编译器生成的类型名。
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
    }
}
