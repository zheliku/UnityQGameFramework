//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Download
{
    internal sealed partial class DownloadManager : FrameworkModule, IDownloadManager
    {
        /// <summary>
        /// 统计最近一段时间的下载流量。
        /// </summary>
        private sealed partial class DownloadCounter
        {
            /// <summary>
            /// 下载流量统计中的时间片记录。
            /// </summary>
            private sealed class DownloadCounterNode : IReference
            {
                /// <summary>
                /// 初始化下载流量采样结点。
                /// </summary>
                public DownloadCounterNode()
                {
                    DeltaLength = 0L;
                    ElapseSeconds = 0f;
                }

                /// <summary>
                /// 获取采样期间新增的下载字节数。
                /// </summary>
                public long DeltaLength { get; private set; }

                /// <summary>
                /// 获取采样已流逝时间，以秒为单位。
                /// </summary>
                public float ElapseSeconds { get; private set; }

                /// <summary>
                /// 从引用池获取下载速率采样结点。
                /// </summary>
                /// <returns>已重置的下载速率采样结点。</returns>
                public static DownloadCounterNode Create()
                {
                    return ReferencePool.Acquire<DownloadCounterNode>();
                }

                /// <summary>
                /// 更新下载采样的流逝时间。
                /// </summary>
                /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
                /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                    ElapseSeconds += realElapseSeconds;
                }

                /// <summary>
                /// 累计新增下载长度。
                /// </summary>
                /// <param name="deltaLength">新增下载字节数。</param>
                public void AddDeltaLength(int deltaLength)
                {
                    DeltaLength += deltaLength;
                }

                /// <summary>
                /// 重置下载采样数据。
                /// </summary>
                public void Clear()
                {
                    DeltaLength = 0L;
                    ElapseSeconds = 0f;
                }
            }
        }
    }
}
