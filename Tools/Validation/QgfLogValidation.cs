#define ENABLE_LOG
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 检查日志格式、分级和异常信息。
/// </summary>
public static class QgfLogValidation
{
    /// <summary>
    /// 检查默认日志辅助器，不向 Console 写入测试错误。
    /// </summary>
    /// <returns>日志检查结果。</returns>
    public static object Run()
    {
        ILogHandler original = Debug.unityLogger.logHandler;
        var formatter = typeof(UnityQGameFramework.Editor.ResourceBuildService).Assembly.GetType("UnityQGameFramework.Editor.LogInitializer", true);
        FieldInfo theme = formatter.GetField("s_DarkTheme", BindingFlags.Static | BindingFlags.NonPublic);
        bool originalTheme = (bool)theme.GetValue(null);
        var handler = new CaptureHandler();
        Debug.unityLogger.logHandler = handler;
        try
        {
            // 编辑器中尚未创建 BaseComponent，FrameworkLog 也应能输出菜单消息。
            FrameworkLog.Info("editor-ready");
            Require(handler.Messages.Count == 1 && handler.Messages[0].Contains("[QGF]"), "Editor logger was not initialized.");

            var helper = new DefaultLogHelper();
            helper.Log(FrameworkLogLevel.Debug, "debug");
            helper.Log(FrameworkLogLevel.Info, null);
            helper.Log(FrameworkLogLevel.Warning, "warning");
            helper.Log(FrameworkLogLevel.Error, "error");
            Require(handler.Types[1] == LogType.Log && handler.Messages[1].Contains("DEBUG"), "Debug format failed.");
            Require(handler.Messages[2].Contains("INFO") && handler.Messages[2].EndsWith("null"), "Null message failed.");
            Require(handler.Types[3] == LogType.Warning && handler.Messages[3].Contains("WARNING"), "Warning level failed.");
            Require(handler.Types[4] == LogType.Error && handler.Messages[4].Contains("ERROR"), "Error level failed.");
            int runtimeCallLine = CurrentLine() + 1;
            UnityQGameFramework.Runtime.Log.Info("qgf-direct-caller");
            string runtimeText = handler.Messages[handler.Messages.Count - 1];
            Require(runtimeText.Contains("QgfLogValidation.Run") && runtimeText.Contains("QgfLogValidation.cs:" + runtimeCallLine), "Runtime.Log did not point to its direct caller.");
            runtimeCallLine = CurrentLine() + 1;
            UnityQGameFramework.Runtime.Log.Info("qgf-generic-{0}", 1);
            runtimeText = handler.Messages[handler.Messages.Count - 1];
            Require(runtimeText.Contains("qgf-generic-1") && runtimeText.Contains("QgfLogValidation.cs:" + runtimeCallLine), "Formatted Runtime.Log did not point to its direct caller.");
            Debug.Log("native-console-message");
            Require(handler.Messages[handler.Messages.Count - 1] == "native-console-message", "QGF changed native Debug.Log output.");
            string[] palettes = new string[2];
            for (int index = 0; index < 2; index++)
            {
                theme.SetValue(null, index == 1);
                int callLine = CurrentLine() + 1;
                FrameworkLog.Info("caller-check");
                string formatted = handler.Messages[handler.Messages.Count - 1];
                Require(formatted.Contains("QgfLogValidation.Run") && formatted.Contains("QgfLogValidation.cs:" + callLine), "The caller method and line are missing or point to a logging wrapper.");
                Require(formatted.Contains("<a href=\"") && formatted.Contains("line=\"" + callLine + "\""), "The source link does not include its exact line.");
                Require(!Regex.IsMatch(formatted, @"\d{2}:\d{2}:\d{2}"), "Editor logs repeat Console timestamps.");
                Require(Regex.Matches(formatted, "<color=").Count == 1 && Regex.IsMatch(formatted, @"<color=#[0-9A-Fa-f]{6}>● <b>[A-Z]+</b></color>"), "The level marker must use one theme-aware color.");
                palettes[index] = Regex.Match(formatted, @"#[0-9A-Fa-f]{6}").Value;
            }
            Require(palettes[0] != palettes[1], "Light and dark themes use the same marker palette.");

            Exception source = null;
            try
            {
                throw new InvalidOperationException("original-error");
            }
            catch (Exception exception)
            {
                source = exception;
                helper.Log(FrameworkLogLevel.Error, exception);
            }
            Require(ReferenceEquals(handler.Exception, source), "The original exception type or object was replaced.");
            Require(!string.IsNullOrEmpty(source.StackTrace) && handler.Exception.Message == "original-error", "The original exception stack or message was lost.");

            bool fatalThrown = false;
            try
            {
                helper.Log(FrameworkLogLevel.Fatal, "fatal");
            }
            catch (FrameworkException exception)
            {
                fatalThrown = exception.Message == "fatal";
            }
            Require(fatalThrown, "Fatal must still throw FrameworkException.");
            return new { passed = true, formattedLevels = 4, editorBootstrap = true, nullMessage = true, originalException = true, fatal = true, bothThemes = true, exactCallerLine = true, runtimeLogCaller = true, nativeLogUnchanged = true, sourceHyperlink = true, nativeTextColors = true, noEditorTimestamp = true };
        }
        finally
        {
            Debug.unityLogger.logHandler = original;
            theme.SetValue(null, originalTheme);
        }
    }

    /// <summary>
    /// 检查异步方法和工作线程的调用位置。
    /// </summary>
    /// <returns>日志格式与异步调用检查结果。</returns>
    public static async Task<object> RunAsync()
    {
        object format = Run();
        ILogHandler original = Debug.unityLogger.logHandler;
        var handler = new CaptureHandler();
        Debug.unityLogger.logHandler = handler;
        try
        {
            await Task.Yield();
            int callLine = CurrentLine() + 1;
            FrameworkLog.Info("async-caller-check");
            string text = handler.Messages[handler.Messages.Count - 1];
            Require(text.Contains("QgfLogValidation.RunAsync") && !text.Contains("MoveNext")
                && text.Contains("QgfLogValidation.cs:" + callLine), "Async caller was not restored to its source method.");

            await Task.Run(LogFromWorker);
            text = handler.Messages[handler.Messages.Count - 1];
            Require(text.Contains("QgfLogValidation.LogFromWorker") && text.Contains("worker-caller-check"), "Worker caller was lost.");
            return new { passed = true, format, asyncCaller = true, workerCaller = true };
        }
        finally
        {
            Debug.unityLogger.logHandler = original;
        }
    }

    private static void LogFromWorker()
    {
        FrameworkLog.Info("worker-caller-check");
    }

    private static int CurrentLine([CallerLineNumber] int line = 0)
    {
        return line;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class CaptureHandler : ILogHandler
    {
        internal readonly List<string> Messages = new List<string>();
        internal readonly List<LogType> Types = new List<LogType>();
        internal Exception Exception;

        /// <summary>
        /// 接收格式化日志。
        /// </summary>
        /// <param name="logType">日志类型。</param>
        /// <param name="context">日志关联对象。</param>
        /// <param name="format">日志格式。</param>
        /// <param name="args">日志参数。</param>
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            Types.Add(logType);
            Messages.Add(string.Format(format, args));
        }

        /// <summary>
        /// 接收异常日志。
        /// </summary>
        /// <param name="exception">异常。</param>
        /// <param name="context">日志关联对象。</param>
        public void LogException(Exception exception, UnityEngine.Object context)
        {
            Exception = exception;
        }
    }
}
