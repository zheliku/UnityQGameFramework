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
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 显示运行时内存采样结果。
        /// </summary>
        /// <typeparam name="T">需要采样的 Unity 对象类型。</typeparam>
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase where T : UnityEngine.Object
        {
            private const int ShowSampleCount = 300;

            private readonly List<Sample> m_Samples = new List<Sample>();
            private readonly Comparison<Sample> m_SampleComparer = SampleComparer;
            private DateTime m_SampleTime = DateTime.MinValue;
            private long m_SampleSize = 0L;
            private long m_DuplicateSampleSize = 0L;
            private int m_DuplicateSimpleCount = 0;

            /// <summary>
            /// 绘制运行时内存采样。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                string typeName = typeof(T).Name;
                GUILayout.Label(Utility.Text.Format("<b>{0} Runtime Memory Information</b>", typeName));
                GUILayout.BeginVertical("box");
                {
                    if (GUILayout.Button(Utility.Text.Format("Take Sample for {0}", typeName), GUILayout.Height(30f)))
                    {
                        TakeSample();
                    }

                    if (m_SampleTime <= DateTime.MinValue)
                    {
                        GUILayout.Label(Utility.Text.Format("<b>Please take sample for {0} first.</b>", typeName));
                    }
                    else
                    {
                        if (m_DuplicateSimpleCount > 0)
                        {
                            GUILayout.Label(Utility.Text.Format("<b>{0} {1}s ({2}) obtained at {3:yyyy-MM-dd HH:mm:ss}, while {4} {1}s ({5}) might be duplicated.</b>", m_Samples.Count, typeName, GetByteLengthString(m_SampleSize), m_SampleTime.ToLocalTime(), m_DuplicateSimpleCount, GetByteLengthString(m_DuplicateSampleSize)));
                        }
                        else
                        {
                            GUILayout.Label(Utility.Text.Format("<b>{0} {1}s ({2}) obtained at {3:yyyy-MM-dd HH:mm:ss}.</b>", m_Samples.Count, typeName, GetByteLengthString(m_SampleSize), m_SampleTime.ToLocalTime()));
                        }

                        if (m_Samples.Count > 0)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(Utility.Text.Format("<b>{0} Name</b>", typeName));
                                GUILayout.Label("<b>Type</b>", GUILayout.Width(240f));
                                GUILayout.Label("<b>Size</b>", GUILayout.Width(80f));
                            }
                            GUILayout.EndHorizontal();
                        }

                        int count = 0;
                        for (int i = 0; i < m_Samples.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(m_Samples[i].Highlight ? Utility.Text.Format("<color=yellow>{0}</color>", m_Samples[i].Name) : m_Samples[i].Name);
                                GUILayout.Label(m_Samples[i].Highlight ? Utility.Text.Format("<color=yellow>{0}</color>", m_Samples[i].Type) : m_Samples[i].Type, GUILayout.Width(240f));
                                GUILayout.Label(m_Samples[i].Highlight ? Utility.Text.Format("<color=yellow>{0}</color>", GetByteLengthString(m_Samples[i].Size)) : GetByteLengthString(m_Samples[i].Size), GUILayout.Width(80f));
                            }
                            GUILayout.EndHorizontal();

                            count++;
                            if (count >= ShowSampleCount)
                            {
                                break;
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 采样已加载对象的内存并标记重复记录。
            /// </summary>
            private void TakeSample()
            {
                m_SampleTime = DateTime.UtcNow;
                m_SampleSize = 0L;
                m_DuplicateSampleSize = 0L;
                m_DuplicateSimpleCount = 0;
                m_Samples.Clear();

                T[] samples = Resources.FindObjectsOfTypeAll<T>();
                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
                    m_SampleSize += sampleSize;
                    m_Samples.Add(new Sample(samples[i].name, samples[i].GetType().Name, sampleSize));
                }

                m_Samples.Sort(m_SampleComparer);

                for (int i = 1; i < m_Samples.Count; i++)
                {
                    if (m_Samples[i].Name == m_Samples[i - 1].Name && m_Samples[i].Type == m_Samples[i - 1].Type && m_Samples[i].Size == m_Samples[i - 1].Size)
                    {
                        m_Samples[i].Highlight = true;
                        m_DuplicateSampleSize += m_Samples[i].Size;
                        m_DuplicateSimpleCount++;
                    }
                }
            }

            /// <summary>
            /// 按内存大小降序，再按类型和名称比较采样记录。
            /// </summary>
            /// <param name="a">第一条采样记录。</param>
            /// <param name="b">第二条采样记录。</param>
            /// <returns>表示采样排序关系的有符号整数。</returns>
            private static int SampleComparer(Sample a, Sample b)
            {
                int result = b.Size.CompareTo(a.Size);
                if (result != 0)
                {
                    return result;
                }

                result = a.Type.CompareTo(b.Type);
                if (result != 0)
                {
                    return result;
                }

                return a.Name.CompareTo(b.Name);
            }
        }
    }
}
