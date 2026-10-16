//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Download
{
    internal sealed partial class DownloadManager : FrameworkModule, IDownloadManager
    {
        /// <summary>
        /// 下载任务。
        /// </summary>
        private sealed class DownloadTask : TaskBase
        {
            private static int s_Serial = 0;

            /// <summary>
            /// 初始化下载任务的新实例。
            /// </summary>
            public DownloadTask()
            {
                Status = DownloadTaskStatus.Todo;
                DownloadPath = null;
                DownloadUri = null;
                FlushSize = 0;
                Timeout = 0f;
            }

            /// <summary>
            /// 获取或设置下载任务的状态。
            /// </summary>
            public DownloadTaskStatus Status { get; set; }

            /// <summary>
            /// 获取下载后存放路径。
            /// </summary>
            public string DownloadPath { get; private set; }

            /// <summary>
            /// 获取原始下载地址。
            /// </summary>
            public string DownloadUri { get; private set; }

            /// <summary>
            /// 获取将缓冲区写入磁盘的临界大小。
            /// </summary>
            public int FlushSize { get; private set; }

            /// <summary>
            /// 获取下载超时时长，以秒为单位。
            /// </summary>
            public float Timeout { get; private set; }

            /// <summary>
            /// 获取下载任务的描述。
            /// </summary>
            public override string Description => DownloadPath;

            /// <summary>
            /// 创建下载任务。
            /// </summary>
            /// <param name="downloadPath">下载后存放路径。</param>
            /// <param name="downloadUri">原始下载地址。</param>
            /// <param name="tag">下载任务的标签。</param>
            /// <param name="priority">下载任务的优先级。</param>
            /// <param name="flushSize">将缓冲区写入磁盘的临界大小。</param>
            /// <param name="timeout">下载超时时长，以秒为单位。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的下载任务。</returns>
            public static DownloadTask Create(string downloadPath, string downloadUri, string tag, int priority, int flushSize, float timeout, object userData)
            {
                DownloadTask downloadTask = ReferencePool.Acquire<DownloadTask>();
                downloadTask.Initialize(++s_Serial, tag, priority, userData);
                downloadTask.DownloadPath = downloadPath;
                downloadTask.DownloadUri = downloadUri;
                downloadTask.FlushSize = flushSize;
                downloadTask.Timeout = timeout;
                return downloadTask;
            }

            /// <summary>
            /// 清理下载任务。
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                Status = DownloadTaskStatus.Todo;
                DownloadPath = null;
                DownloadUri = null;
                FlushSize = 0;
                Timeout = 0f;
            }
        }
    }
}
