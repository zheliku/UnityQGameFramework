//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        private sealed class FpsCounter
        {
            private float m_UpdateInterval;
            private int m_Frames;
            private float m_Accumulator;
            private float m_TimeLeft;

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

            public float CurrentFps { get; private set; }

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
