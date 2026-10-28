//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class DownloadManager : GameFrameworkModule, IDownloadManager
    {
        /// <summary>
        /// 统计最近一段时间的下载流量。
        /// </summary>
        private sealed partial class DownloadCounter
        {
            private readonly GameFrameworkLinkedList<DownloadCounterNode> m_DownloadCounterNodes;
            private float m_UpdateInterval;
            private float m_RecordInterval;
            private float m_Accumulator;
            private float m_TimeLeft;

            /// <summary>
            /// 初始化下载速率统计器。
            /// </summary>
            /// <param name="updateInterval">速率刷新间隔，以秒为单位。</param>
            /// <param name="recordInterval">统计样本保留时长，以秒为单位。</param>
            public DownloadCounter(float updateInterval, float recordInterval)
            {
                if (updateInterval <= 0f)
                {
                    throw new GameFrameworkException("Update interval is invalid.");
                }

                if (recordInterval <= 0f)
                {
                    throw new GameFrameworkException("Record interval is invalid.");
                }

                m_DownloadCounterNodes = new GameFrameworkLinkedList<DownloadCounterNode>();
                m_UpdateInterval = updateInterval;
                m_RecordInterval = recordInterval;
                Reset();
            }

            /// <summary>
            /// 获取或设置速率刷新间隔，以秒为单位。
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
                        throw new GameFrameworkException("Update interval is invalid.");
                    }

                    m_UpdateInterval = value;
                    Reset();
                }
            }

            /// <summary>
            /// 获取或设置统计样本保留时长，以秒为单位。
            /// </summary>
            public float RecordInterval
            {
                get
                {
                    return m_RecordInterval;
                }
                set
                {
                    if (value <= 0f)
                    {
                        throw new GameFrameworkException("Record interval is invalid.");
                    }

                    m_RecordInterval = value;
                    Reset();
                }
            }

            /// <summary>
            /// 获取当前下载速率，以字节每秒为单位。
            /// </summary>
            public float CurrentSpeed
            {
                get; private set;
            }

            /// <summary>
            /// 清理下载速率统计样本。
            /// </summary>
            public void Shutdown()
            {
                Reset();
            }

            /// <summary>
            /// 更新采样时间并计算下载速率。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (m_DownloadCounterNodes.Count <= 0)
                {
                    return;
                }

                m_Accumulator += realElapseSeconds;
                if (m_Accumulator > m_RecordInterval)
                {
                    m_Accumulator = m_RecordInterval;
                }

                m_TimeLeft -= realElapseSeconds;
                foreach (DownloadCounterNode downloadCounterNode in m_DownloadCounterNodes)
                {
                    downloadCounterNode.Update(elapseSeconds, realElapseSeconds);
                }

                while (m_DownloadCounterNodes.Count > 0)
                {
                    DownloadCounterNode downloadCounterNode = m_DownloadCounterNodes.First.Value;
                    if (downloadCounterNode.ElapseSeconds < m_RecordInterval)
                    {
                        break;
                    }

                    ReferencePool.Release(downloadCounterNode);
                    m_DownloadCounterNodes.RemoveFirst();
                }

                if (m_DownloadCounterNodes.Count <= 0)
                {
                    Reset();
                    return;
                }

                if (m_TimeLeft <= 0f)
                {
                    long totalDeltaLength = 0L;
                    foreach (DownloadCounterNode downloadCounterNode in m_DownloadCounterNodes)
                    {
                        totalDeltaLength += downloadCounterNode.DeltaLength;
                    }

                    CurrentSpeed = m_Accumulator > 0f ? totalDeltaLength / m_Accumulator : 0f;
                    m_TimeLeft += m_UpdateInterval;
                }
            }

            /// <summary>
            /// 记录新增下载长度。
            /// </summary>
            /// <param name="deltaLength">新增下载字节数。</param>
            public void RecordDeltaLength(int deltaLength)
            {
                if (deltaLength <= 0)
                {
                    return;
                }

                DownloadCounterNode downloadCounterNode = null;
                if (m_DownloadCounterNodes.Count > 0)
                {
                    downloadCounterNode = m_DownloadCounterNodes.Last.Value;
                    if (downloadCounterNode.ElapseSeconds < m_UpdateInterval)
                    {
                        downloadCounterNode.AddDeltaLength(deltaLength);
                        return;
                    }
                }

                downloadCounterNode = DownloadCounterNode.Create();
                downloadCounterNode.AddDeltaLength(deltaLength);
                m_DownloadCounterNodes.AddLast(downloadCounterNode);
            }

            /// <summary>
            /// 重置统计计时并归还采样结点。
            /// </summary>
            private void Reset()
            {
                m_DownloadCounterNodes.Clear();
                CurrentSpeed = 0f;
                m_Accumulator = 0f;
                m_TimeLeft = 0f;
            }
        }
    }
}
