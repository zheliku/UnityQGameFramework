//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 显示、筛选并查看运行日志。
        /// </summary>
        [Serializable]
        private sealed class ConsoleWindow : IDebuggerWindow
        {
            private readonly Queue<LogNode> m_LogNodes = new Queue<LogNode>();

            private SettingComponent m_SettingComponent = null;
            private Vector2 m_LogScrollPosition = Vector2.zero;
            private Vector2 m_StackScrollPosition = Vector2.zero;
            private int m_InfoCount = 0;
            private int m_WarningCount = 0;
            private int m_ErrorCount = 0;
            private int m_FatalCount = 0;
            private LogNode m_SelectedNode = null;
            private bool m_LastLockScroll = true;
            private bool m_LastInfoFilter = true;
            private bool m_LastWarningFilter = true;
            private bool m_LastErrorFilter = true;
            private bool m_LastFatalFilter = true;

            [SerializeField]
            private bool m_LockScroll = true;

            [SerializeField]
            private int m_MaxLine = 100;

            [SerializeField]
            private bool m_InfoFilter = true;

            [SerializeField]
            private bool m_WarningFilter = true;

            [SerializeField]
            private bool m_ErrorFilter = true;

            [SerializeField]
            private bool m_FatalFilter = true;

            [SerializeField]
            private Color32 m_InfoColor = Color.white;

            [SerializeField]
            private Color32 m_WarningColor = Color.yellow;

            [SerializeField]
            private Color32 m_ErrorColor = Color.red;

            [SerializeField]
            private Color32 m_FatalColor = new Color(0.7f, 0.2f, 0.2f);

            /// <summary>
            /// 获取或设置是否锁定日志滚动位置。
            /// </summary>
            public bool LockScroll
            {
                get
                {
                    return m_LockScroll;
                }
                set
                {
                    m_LockScroll = value;
                }
            }

            /// <summary>
            /// 获取或设置最多保留的日志条数。
            /// </summary>
            public int MaxLine
            {
                get
                {
                    return m_MaxLine;
                }
                set
                {
                    m_MaxLine = value;
                }
            }

            /// <summary>
            /// 获取或设置是否显示普通日志。
            /// </summary>
            public bool InfoFilter
            {
                get
                {
                    return m_InfoFilter;
                }
                set
                {
                    m_InfoFilter = value;
                }
            }

            /// <summary>
            /// 获取或设置是否显示警告日志。
            /// </summary>
            public bool WarningFilter
            {
                get
                {
                    return m_WarningFilter;
                }
                set
                {
                    m_WarningFilter = value;
                }
            }

            /// <summary>
            /// 获取或设置是否显示错误日志。
            /// </summary>
            public bool ErrorFilter
            {
                get
                {
                    return m_ErrorFilter;
                }
                set
                {
                    m_ErrorFilter = value;
                }
            }

            /// <summary>
            /// 获取或设置是否显示异常日志。
            /// </summary>
            public bool FatalFilter
            {
                get
                {
                    return m_FatalFilter;
                }
                set
                {
                    m_FatalFilter = value;
                }
            }

            /// <summary>
            /// 获取普通日志数量。
            /// </summary>
            public int InfoCount
            {
                get
                {
                    return m_InfoCount;
                }
            }

            /// <summary>
            /// 获取警告日志数量。
            /// </summary>
            public int WarningCount
            {
                get
                {
                    return m_WarningCount;
                }
            }

            /// <summary>
            /// 获取错误日志数量。
            /// </summary>
            public int ErrorCount
            {
                get
                {
                    return m_ErrorCount;
                }
            }

            /// <summary>
            /// 获取异常日志数量。
            /// </summary>
            public int FatalCount
            {
                get
                {
                    return m_FatalCount;
                }
            }

            /// <summary>
            /// 获取或设置普通日志颜色。
            /// </summary>
            public Color32 InfoColor
            {
                get
                {
                    return m_InfoColor;
                }
                set
                {
                    m_InfoColor = value;
                }
            }

            /// <summary>
            /// 获取或设置警告日志颜色。
            /// </summary>
            public Color32 WarningColor
            {
                get
                {
                    return m_WarningColor;
                }
                set
                {
                    m_WarningColor = value;
                }
            }

            /// <summary>
            /// 获取或设置错误日志颜色。
            /// </summary>
            public Color32 ErrorColor
            {
                get
                {
                    return m_ErrorColor;
                }
                set
                {
                    m_ErrorColor = value;
                }
            }

            /// <summary>
            /// 获取或设置异常日志颜色。
            /// </summary>
            public Color32 FatalColor
            {
                get
                {
                    return m_FatalColor;
                }
                set
                {
                    m_FatalColor = value;
                }
            }

            /// <summary>
            /// 订阅 Unity 日志接收事件。
            /// </summary>
            /// <param name="args">窗口初始化参数。</param>
            public void Initialize(params object[] args)
            {
                m_SettingComponent = GameEntry.GetComponent<SettingComponent>();
                if (m_SettingComponent == null)
                {
                    Log.Fatal("Setting component is invalid.");
                    return;
                }

                Application.logMessageReceived += OnLogMessageReceived;
                m_LockScroll = m_LastLockScroll = m_SettingComponent.GetBool("Debugger.Console.LockScroll", true);
                m_InfoFilter = m_LastInfoFilter = m_SettingComponent.GetBool("Debugger.Console.InfoFilter", true);
                m_WarningFilter = m_LastWarningFilter = m_SettingComponent.GetBool("Debugger.Console.WarningFilter", true);
                m_ErrorFilter = m_LastErrorFilter = m_SettingComponent.GetBool("Debugger.Console.ErrorFilter", true);
                m_FatalFilter = m_LastFatalFilter = m_SettingComponent.GetBool("Debugger.Console.FatalFilter", true);
            }

            /// <summary>
            /// 取消日志接收并清理日志记录。
            /// </summary>
            public void Shutdown()
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                Clear();
            }

            /// <summary>
            /// 进入控制台调试窗口。
            /// </summary>
            public void OnEnter()
            {
            }

            /// <summary>
            /// 离开控制台调试窗口。
            /// </summary>
            public void OnLeave()
            {
            }

            /// <summary>
            /// 更新控制台日志统计。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (m_LastLockScroll != m_LockScroll)
                {
                    m_LastLockScroll = m_LockScroll;
                    m_SettingComponent.SetBool("Debugger.Console.LockScroll", m_LockScroll);
                }

                if (m_LastInfoFilter != m_InfoFilter)
                {
                    m_LastInfoFilter = m_InfoFilter;
                    m_SettingComponent.SetBool("Debugger.Console.InfoFilter", m_InfoFilter);
                }

                if (m_LastWarningFilter != m_WarningFilter)
                {
                    m_LastWarningFilter = m_WarningFilter;
                    m_SettingComponent.SetBool("Debugger.Console.WarningFilter", m_WarningFilter);
                }

                if (m_LastErrorFilter != m_ErrorFilter)
                {
                    m_LastErrorFilter = m_ErrorFilter;
                    m_SettingComponent.SetBool("Debugger.Console.ErrorFilter", m_ErrorFilter);
                }

                if (m_LastFatalFilter != m_FatalFilter)
                {
                    m_LastFatalFilter = m_FatalFilter;
                    m_SettingComponent.SetBool("Debugger.Console.FatalFilter", m_FatalFilter);
                }
            }

            /// <summary>
            /// 绘制日志筛选、列表和堆栈区域。
            /// </summary>
            public void OnDraw()
            {
                RefreshCount();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
                    {
                        Clear();
                    }
                    m_LockScroll = GUILayout.Toggle(m_LockScroll, "Lock Scroll", GUILayout.Width(90f));
                    GUILayout.FlexibleSpace();
                    m_InfoFilter = GUILayout.Toggle(m_InfoFilter, Utility.Text.Format("Info ({0})", m_InfoCount), GUILayout.Width(90f));
                    m_WarningFilter = GUILayout.Toggle(m_WarningFilter, Utility.Text.Format("Warning ({0})", m_WarningCount), GUILayout.Width(90f));
                    m_ErrorFilter = GUILayout.Toggle(m_ErrorFilter, Utility.Text.Format("Error ({0})", m_ErrorCount), GUILayout.Width(90f));
                    m_FatalFilter = GUILayout.Toggle(m_FatalFilter, Utility.Text.Format("Fatal ({0})", m_FatalCount), GUILayout.Width(90f));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box");
                {
                    if (m_LockScroll)
                    {
                        m_LogScrollPosition.y = float.MaxValue;
                    }

                    m_LogScrollPosition = GUILayout.BeginScrollView(m_LogScrollPosition);
                    {
                        bool selected = false;
                        foreach (LogNode logNode in m_LogNodes)
                        {
                            switch (logNode.LogType)
                            {
                                case LogType.Log:
                                    if (!m_InfoFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Warning:
                                    if (!m_WarningFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Error:
                                    if (!m_ErrorFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Exception:
                                    if (!m_FatalFilter)
                                    {
                                        continue;
                                    }
                                    break;
                            }
                            if (GUILayout.Toggle(m_SelectedNode == logNode, GetLogString(logNode)))
                            {
                                selected = true;
                                if (m_SelectedNode != logNode)
                                {
                                    m_SelectedNode = logNode;
                                    m_StackScrollPosition = Vector2.zero;
                                }
                            }
                        }
                        if (!selected)
                        {
                            m_SelectedNode = null;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    m_StackScrollPosition = GUILayout.BeginScrollView(m_StackScrollPosition, GUILayout.Height(100f));
                    {
                        if (m_SelectedNode != null)
                        {
                            Color32 color = GetLogStringColor(m_SelectedNode.LogType);
                            if (GUILayout.Button(Utility.Text.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>{4}</b></color>{6}{6}{5}", color.r, color.g, color.b, color.a, m_SelectedNode.LogMessage, m_SelectedNode.StackTrack, Environment.NewLine), "label"))
                            {
                                CopyToClipboard(Utility.Text.Format("{0}{2}{2}{1}", m_SelectedNode.LogMessage, m_SelectedNode.StackTrack, Environment.NewLine));
                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 清空日志记录。
            /// </summary>
            private void Clear()
            {
                m_LogNodes.Clear();
            }

            /// <summary>
            /// 重新统计各等级的日志数量。
            /// </summary>
            public void RefreshCount()
            {
                m_InfoCount = 0;
                m_WarningCount = 0;
                m_ErrorCount = 0;
                m_FatalCount = 0;
                foreach (LogNode logNode in m_LogNodes)
                {
                    switch (logNode.LogType)
                    {
                        case LogType.Log:
                            m_InfoCount++;
                            break;

                        case LogType.Warning:
                            m_WarningCount++;
                            break;

                        case LogType.Error:
                            m_ErrorCount++;
                            break;

                        case LogType.Exception:
                            m_FatalCount++;
                            break;
                    }
                }
            }

            /// <summary>
            /// 获取最近保留的日志记录。
            /// </summary>
            /// <param name="results">用于接收日志记录的列表；填充前清空，容量不足时仍会扩容。</param>
            public void GetRecentLogsNonAlloc(List<LogNode> results)
            {
                if (results == null)
                {
                    Log.Error("Results is invalid.");
                    return;
                }

                results.Clear();
                foreach (LogNode logNode in m_LogNodes)
                {
                    results.Add(logNode);
                }
            }

            /// <summary>
            /// 获取最近保留的日志记录。
            /// </summary>
            /// <param name="results">用于接收日志记录的列表；填充前清空，容量不足时仍会扩容。</param>
            /// <param name="count">最多获取的日志条数。</param>
            public void GetRecentLogsNonAlloc(List<LogNode> results, int count)
            {
                if (results == null)
                {
                    Log.Error("Results is invalid.");
                    return;
                }

                if (count <= 0)
                {
                    Log.Error("Count is invalid.");
                    return;
                }

                int position = m_LogNodes.Count - count;
                if (position < 0)
                {
                    position = 0;
                }

                int index = 0;
                results.Clear();
                foreach (LogNode logNode in m_LogNodes)
                {
                    if (index++ < position)
                    {
                        continue;
                    }

                    results.Add(logNode);
                }
            }

            /// <summary>
            /// 记录 Unity 日志，超过上限时归还最早的记录。
            /// </summary>
            /// <param name="logMessage">日志内容。</param>
            /// <param name="stackTrace">调用堆栈。</param>
            /// <param name="logType">日志类型。</param>
            private void OnLogMessageReceived(string logMessage, string stackTrace, LogType logType)
            {
                if (logType == LogType.Assert)
                {
                    logType = LogType.Error;
                }

                m_LogNodes.Enqueue(LogNode.Create(logType, logMessage, stackTrace));
                while (m_LogNodes.Count > m_MaxLine)
                {
                    ReferencePool.Release(m_LogNodes.Dequeue());
                }
            }

            /// <summary>
            /// 生成包含时间、帧数和等级颜色的日志文本。
            /// </summary>
            /// <param name="logNode">日志记录。</param>
            /// <returns>用于列表显示的日志文本。</returns>
            private string GetLogString(LogNode logNode)
            {
                Color32 color = GetLogStringColor(logNode.LogType);
                return Utility.Text.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}>[{4:HH:mm:ss.fff}][{5}] {6}</color>", color.r, color.g, color.b, color.a, logNode.LogTime.ToLocalTime(), logNode.LogFrameCount, logNode.LogMessage);
            }

            /// <summary>
            /// 获取日志类型对应的颜色。
            /// </summary>
            /// <param name="logType">日志类型。</param>
            /// <returns>日志显示颜色。</returns>
            internal Color32 GetLogStringColor(LogType logType)
            {
                Color32 color = Color.white;
                switch (logType)
                {
                    case LogType.Log:
                        color = m_InfoColor;
                        break;

                    case LogType.Warning:
                        color = m_WarningColor;
                        break;

                    case LogType.Error:
                        color = m_ErrorColor;
                        break;

                    case LogType.Exception:
                        color = m_FatalColor;
                        break;
                }

                return color;
            }
        }
    }
}
