//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 按时间窗口统计平均帧率。
        /// </summary>
        private sealed class FpsCounter
        {
            private float m_UpdateInterval;
            private int m_Frames;
            private float m_Accumulator;
            private float m_TimeLeft;

            /// <summary>
            /// 初始化帧率计数器。
            /// </summary>
            /// <param name="updateInterval">帧率刷新间隔，以秒为单位。</param>
            public FpsCounter(float updateInterval)
            {
                if (updateInterval <= 0f)
                {
                    Log.Error("Update interval is invalid.");
                    return;
                }

                m_UpdateInterval = updateInterval;
                Reset();
            }

            /// <summary>
            /// 获取或设置帧率刷新间隔，以秒为单位。
            /// </summary>
            public float UpdateInterval
            {
                get
                {
                    return m_UpdateInterval;
                }
                set
                {
                    if (value <= 0f)
                    {
                        Log.Error("Update interval is invalid.");
                        return;
                    }

                    m_UpdateInterval = value;
                    Reset();
                }
            }

            /// <summary>
            /// 获取当前采样区间的平均帧率。
            /// </summary>
            public float CurrentFps
            {
                get; private set;
            }

            /// <summary>
            /// 累计帧数并按间隔刷新帧率。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                m_Frames++;
                m_Accumulator += realElapseSeconds;
                m_TimeLeft -= realElapseSeconds;

                if (m_TimeLeft <= 0f)
                {
                    CurrentFps = m_Accumulator > 0f ? m_Frames / m_Accumulator : 0f;
                    m_Frames = 0;
                    m_Accumulator = 0f;
                    m_TimeLeft += m_UpdateInterval;
                }
            }

            /// <summary>
            /// 重置帧率采样计数和计时。
            /// </summary>
            private void Reset()
            {
                CurrentFps = 0f;
                m_Frames = 0;
                m_Accumulator = 0f;
                m_TimeLeft = 0f;
            }
        }
    }
}
