//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Download
{
    internal sealed partial class DownloadManager : FrameworkModule, IDownloadManager
    {
        private sealed partial class DownloadCounter
        {
            private sealed class DownloadCounterNode : IReference
            {
                public DownloadCounterNode()
                {
                    DeltaLength = 0L;
                    ElapseSeconds = 0f;
                }

                public long DeltaLength { get; private set; }

                public float ElapseSeconds { get; private set; }

                public static DownloadCounterNode Create()
                {
                    return ReferencePool.Acquire<DownloadCounterNode>();
                }

                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                    ElapseSeconds += realElapseSeconds;
                }

                public void AddDeltaLength(int deltaLength)
                {
                    DeltaLength += deltaLength;
                }

                public void Clear()
                {
                    DeltaLength = 0L;
                    ElapseSeconds = 0f;
                }
            }
        }
    }
}
