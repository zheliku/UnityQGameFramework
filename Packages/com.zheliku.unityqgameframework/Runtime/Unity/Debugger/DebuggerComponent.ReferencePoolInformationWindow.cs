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
        /// 显示引用池的使用与回收情况。
        /// </summary>
        private sealed class ReferencePoolInformationWindow : ScrollableDebuggerWindowBase
        {
            private readonly Dictionary<string, List<ReferencePoolInfo>> m_ReferencePoolInfos = new Dictionary<string, List<ReferencePoolInfo>>(StringComparer.Ordinal);
            private readonly Comparison<ReferencePoolInfo> m_NormalClassNameComparer = NormalClassNameComparer;
            private readonly Comparison<ReferencePoolInfo> m_FullClassNameComparer = FullClassNameComparer;
            private bool m_ShowFullClassName = false;

            /// <summary>
            /// 准备引用池排序委托。
            /// </summary>
            /// <param name="args">窗口初始化参数。</param>
            public override void Initialize(params object[] args)
            {
            }

            /// <summary>
            /// 绘制引用池信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Reference Pool Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Enable Strict Check", ReferencePool.EnableStrictCheck.ToString());
                    DrawItem("Reference Pool Count", ReferencePool.Count.ToString());
                }
                GUILayout.EndVertical();

                m_ShowFullClassName = GUILayout.Toggle(m_ShowFullClassName, "Show Full Class Name");
                m_ReferencePoolInfos.Clear();
                ReferencePoolInfo[] referencePoolInfos = ReferencePool.GetAllReferencePoolInfos();
                foreach (ReferencePoolInfo referencePoolInfo in referencePoolInfos)
                {
                    string assemblyName = referencePoolInfo.Type.Assembly.GetName().Name;
                    List<ReferencePoolInfo> results = null;
                    if (!m_ReferencePoolInfos.TryGetValue(assemblyName, out results))
                    {
                        results = new List<ReferencePoolInfo>();
                        m_ReferencePoolInfos.Add(assemblyName, results);
                    }

                    results.Add(referencePoolInfo);
                }

                foreach (KeyValuePair<string, List<ReferencePoolInfo>> assemblyReferencePoolInfo in m_ReferencePoolInfos)
                {
                    GUILayout.Label(Utility.Text.Format("<b>Assembly: {0}</b>", assemblyReferencePoolInfo.Key));
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(m_ShowFullClassName ? "<b>Full Class Name</b>" : "<b>Class Name</b>");
                            GUILayout.Label("<b>Unused</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Using</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Acquire</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Release</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Add</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Remove</b>", GUILayout.Width(60f));
                        }
                        GUILayout.EndHorizontal();

                        if (assemblyReferencePoolInfo.Value.Count > 0)
                        {
                            assemblyReferencePoolInfo.Value.Sort(m_ShowFullClassName ? m_FullClassNameComparer : m_NormalClassNameComparer);
                            foreach (ReferencePoolInfo referencePoolInfo in assemblyReferencePoolInfo.Value)
                            {
                                DrawReferencePoolInfo(referencePoolInfo);
                            }
                        }
                        else
                        {
                            GUILayout.Label("<i>Reference Pool is Empty ...</i>");
                        }
                    }
                    GUILayout.EndVertical();
                }
            }

            /// <summary>
            /// 绘制单个类型的引用池统计。
            /// </summary>
            /// <param name="referencePoolInfo">引用池统计信息。</param>
            private void DrawReferencePoolInfo(ReferencePoolInfo referencePoolInfo)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(m_ShowFullClassName ? referencePoolInfo.Type.FullName : referencePoolInfo.Type.Name);
                    GUILayout.Label(referencePoolInfo.UnusedReferenceCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(referencePoolInfo.UsingReferenceCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(referencePoolInfo.AcquireReferenceCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(referencePoolInfo.ReleaseReferenceCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(referencePoolInfo.AddReferenceCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(referencePoolInfo.RemoveReferenceCount.ToString(), GUILayout.Width(60f));
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// 按类型简名比较引用池记录。
            /// </summary>
            /// <param name="a">第一条引用池记录。</param>
            /// <param name="b">第二条引用池记录。</param>
            /// <returns>表示类型名称排序关系的有符号整数。</returns>
            private static int NormalClassNameComparer(ReferencePoolInfo a, ReferencePoolInfo b)
            {
                return a.Type.Name.CompareTo(b.Type.Name);
            }

            /// <summary>
            /// 按类型全名比较引用池记录。
            /// </summary>
            /// <param name="a">第一条引用池记录。</param>
            /// <param name="b">第二条引用池记录。</param>
            /// <returns>表示类型名称排序关系的有符号整数。</returns>
            private static int FullClassNameComparer(ReferencePoolInfo a, ReferencePoolInfo b)
            {
                return a.Type.FullName.CompareTo(b.Type.FullName);
            }
        }
    }
}
