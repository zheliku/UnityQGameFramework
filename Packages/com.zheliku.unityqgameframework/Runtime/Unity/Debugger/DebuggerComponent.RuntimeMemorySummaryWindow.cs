//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 按类型汇总运行时对象内存。
        /// </summary>
        private sealed partial class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            private readonly List<Record> m_Records = new List<Record>();
            private readonly Comparison<Record> m_RecordComparer = RecordComparer;
            private DateTime m_SampleTime = DateTime.MinValue;
            private int m_SampleCount = 0;
            private long m_SampleSize = 0L;

            /// <summary>
            /// 绘制运行时内存汇总。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Runtime Memory Summary</b>");
                GUILayout.BeginVertical("box");
                {
                    if (GUILayout.Button("Take Sample", GUILayout.Height(30f)))
                    {
                        TakeSample();
                    }

                    if (m_SampleTime <= DateTime.MinValue)
                    {
                        GUILayout.Label("<b>Please take sample first.</b>");
                    }
                    else
                    {
                        GUILayout.Label(Utility.Text.Format("<b>{0} Objects ({1}) obtained at {2:yyyy-MM-dd HH:mm:ss}.</b>", m_SampleCount, GetByteLengthString(m_SampleSize), m_SampleTime.ToLocalTime()));

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("<b>Type</b>");
                            GUILayout.Label("<b>Count</b>", GUILayout.Width(120f));
                            GUILayout.Label("<b>Size</b>", GUILayout.Width(120f));
                        }
                        GUILayout.EndHorizontal();

                        for (int i = 0; i < m_Records.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(m_Records[i].Name);
                                GUILayout.Label(m_Records[i].Count.ToString(), GUILayout.Width(120f));
                                GUILayout.Label(GetByteLengthString(m_Records[i].Size), GUILayout.Width(120f));
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 按类型汇总已加载对象数量和内存。
            /// </summary>
            private void TakeSample()
            {
                m_Records.Clear();
                m_SampleTime = DateTime.UtcNow;
                m_SampleCount = 0;
                m_SampleSize = 0L;

                UnityEngine.Object[] samples = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
                    string name = samples[i].GetType().Name;
                    m_SampleCount++;
                    m_SampleSize += sampleSize;

                    Record record = null;
                    foreach (Record r in m_Records)
                    {
                        if (r.Name == name)
                        {
                            record = r;
                            break;
                        }
                    }

                    if (record == null)
                    {
                        record = new Record(name);
                        m_Records.Add(record);
                    }

                    record.Count++;
                    record.Size += sampleSize;
                }

                m_Records.Sort(m_RecordComparer);
            }

            /// <summary>
            /// 按内存降序，再按数量和名称比较汇总记录。
            /// </summary>
            /// <param name="a">第一条汇总记录。</param>
            /// <param name="b">第二条汇总记录。</param>
            /// <returns>表示记录排序关系的有符号整数。</returns>
            private static int RecordComparer(Record a, Record b)
            {
                int result = b.Size.CompareTo(a.Size);
                if (result != 0)
                {
                    return result;
                }

                result = a.Count.CompareTo(b.Count);
                if (result != 0)
                {
                    return result;
                }

                return a.Name.CompareTo(b.Name);
            }
        }
    }
}
