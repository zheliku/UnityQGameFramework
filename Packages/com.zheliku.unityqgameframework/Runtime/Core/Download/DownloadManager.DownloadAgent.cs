//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;

namespace UnityQGameFramework.Core.Download
{
    internal sealed partial class DownloadManager : FrameworkModule, IDownloadManager
    {
        /// <summary>
        /// 下载代理。
        /// </summary>
        private sealed class DownloadAgent : ITaskAgent<DownloadTask>, IDisposable
        {
            private readonly IDownloadAgentHelper m_Helper;
            private FileStream m_FileStream;
            private int m_WaitFlushSize;
            private bool m_Disposed;

            /// <summary>
            /// 下载开始回调。
            /// </summary>
            public FrameworkAction<DownloadAgent> DownloadAgentStart;

            /// <summary>
            /// 下载字节增量回调。
            /// </summary>
            public FrameworkAction<DownloadAgent, int> DownloadAgentUpdate;

            /// <summary>
            /// 下载成功回调。
            /// </summary>
            public FrameworkAction<DownloadAgent, long> DownloadAgentSuccess;

            /// <summary>
            /// 下载失败回调。
            /// </summary>
            public FrameworkAction<DownloadAgent, string> DownloadAgentFailure;

            /// <summary>
            /// 初始化下载代理的新实例。
            /// </summary>
            /// <param name="downloadAgentHelper">下载代理辅助器。</param>
            public DownloadAgent(IDownloadAgentHelper downloadAgentHelper)
            {
                if (downloadAgentHelper == null)
                {
                    throw new FrameworkException("Download agent helper is invalid.");
                }

                m_Helper = downloadAgentHelper;
                Task = null;
                m_FileStream = null;
                m_WaitFlushSize = 0;
                WaitTime = 0f;
                StartLength = 0L;
                DownloadedLength = 0L;
                SavedLength = 0L;
                m_Disposed = false;

                DownloadAgentStart = null;
                DownloadAgentUpdate = null;
                DownloadAgentSuccess = null;
                DownloadAgentFailure = null;
            }

            /// <summary>
            /// 获取下载任务。
            /// </summary>
            public DownloadTask Task { get; private set; }

            /// <summary>
            /// 获取已经等待时间。
            /// </summary>
            public float WaitTime { get; private set; }

            /// <summary>
            /// 获取开始下载时已经存在的大小。
            /// </summary>
            public long StartLength { get; private set; }

            /// <summary>
            /// 获取本次已经下载的大小。
            /// </summary>
            public long DownloadedLength { get; private set; }

            /// <summary>
            /// 获取当前的大小。
            /// </summary>
            public long CurrentLength
            {
                get
                {
                    return StartLength + DownloadedLength;
                }
            }

            /// <summary>
            /// 获取已经存盘的大小。
            /// </summary>
            public long SavedLength { get; private set; }

            /// <summary>
            /// 初始化下载代理。
            /// </summary>
            public void Initialize()
            {
                m_Helper.DownloadAgentHelperUpdateBytes += OnDownloadAgentHelperUpdateBytes;
                m_Helper.DownloadAgentHelperUpdateLength += OnDownloadAgentHelperUpdateLength;
                m_Helper.DownloadAgentHelperComplete += OnDownloadAgentHelperComplete;
                m_Helper.DownloadAgentHelperError += OnDownloadAgentHelperError;
            }

            /// <summary>
            /// 下载代理轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (Task.Status == DownloadTaskStatus.Doing)
                {
                    WaitTime += realElapseSeconds;
                    if (WaitTime >= Task.Timeout)
                    {
                        DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, "Timeout");
                        OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                        ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                    }
                }
            }

            /// <summary>
            /// 关闭并清理下载代理。
            /// </summary>
            public void Shutdown()
            {
                Dispose();

                m_Helper.DownloadAgentHelperUpdateBytes -= OnDownloadAgentHelperUpdateBytes;
                m_Helper.DownloadAgentHelperUpdateLength -= OnDownloadAgentHelperUpdateLength;
                m_Helper.DownloadAgentHelperComplete -= OnDownloadAgentHelperComplete;
                m_Helper.DownloadAgentHelperError -= OnDownloadAgentHelperError;
            }

            /// <summary>
            /// 开始处理下载任务。
            /// </summary>
            /// <param name="task">要处理的下载任务。</param>
            /// <returns>开始处理任务的状态。</returns>
            public StartTaskStatus Start(DownloadTask task)
            {
                if (task == null)
                {
                    throw new FrameworkException("Task is invalid.");
                }

                Task = task;

                Task.Status = DownloadTaskStatus.Doing;
                string downloadFile = Utility.Text.Format("{0}.download", Task.DownloadPath);

                try
                {
                    if (File.Exists(downloadFile))
                    {
                        m_FileStream = File.OpenWrite(downloadFile);
                        m_FileStream.Seek(0L, SeekOrigin.End);
                        StartLength = SavedLength = m_FileStream.Length;
                        DownloadedLength = 0L;
                    }
                    else
                    {
                        string directory = Path.GetDirectoryName(Task.DownloadPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        m_FileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write);
                        StartLength = SavedLength = DownloadedLength = 0L;
                    }

                    if (DownloadAgentStart != null)
                    {
                        DownloadAgentStart(this);
                    }

                    if (StartLength > 0L)
                    {
                        m_Helper.Download(Task.DownloadUri, StartLength, Task.UserData);
                    }
                    else
                    {
                        m_Helper.Download(Task.DownloadUri, Task.UserData);
                    }

                    return StartTaskStatus.CanResume;
                }
                catch (Exception exception)
                {
                    DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                    return StartTaskStatus.UnknownError;
                }
            }

            /// <summary>
            /// 重置下载代理。
            /// </summary>
            public void Reset()
            {
                m_Helper.Reset();

                if (m_FileStream != null)
                {
                    m_FileStream.Close();
                    m_FileStream = null;
                }

                Task = null;
                m_WaitFlushSize = 0;
                WaitTime = 0f;
                StartLength = 0L;
                DownloadedLength = 0L;
                SavedLength = 0L;
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            /// <param name="disposing">释放资源标记。</param>
            private void Dispose(bool disposing)
            {
                if (m_Disposed)
                {
                    return;
                }

                if (disposing)
                {
                    if (m_FileStream != null)
                    {
                        m_FileStream.Dispose();
                        m_FileStream = null;
                    }
                }

                m_Disposed = true;
            }

            /// <summary>
            /// 将接收到的字节写入下载文件。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnDownloadAgentHelperUpdateBytes(object sender, DownloadAgentHelperUpdateBytesEventArgs e)
            {
                WaitTime = 0f;
                try
                {
                    m_FileStream.Write(e.GetBytes(), e.Offset, e.Length);
                    m_WaitFlushSize += e.Length;
                    SavedLength += e.Length;

                    if (m_WaitFlushSize >= Task.FlushSize)
                    {
                        m_FileStream.Flush();
                        m_WaitFlushSize = 0;
                    }
                }
                catch (Exception exception)
                {
                    DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                }
            }

            /// <summary>
            /// 更新已下载长度。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnDownloadAgentHelperUpdateLength(object sender, DownloadAgentHelperUpdateLengthEventArgs e)
            {
                WaitTime = 0f;
                DownloadedLength += e.DeltaLength;
                if (DownloadAgentUpdate != null)
                {
                    DownloadAgentUpdate(this, e.DeltaLength);
                }
            }

            /// <summary>
            /// 完成文件写入并通知下载结果。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnDownloadAgentHelperComplete(object sender, DownloadAgentHelperCompleteEventArgs e)
            {
                WaitTime = 0f;
                DownloadedLength = e.Length;
                if (SavedLength != CurrentLength)
                {
                    throw new FrameworkException("Internal download error.");
                }

                m_Helper.Reset();
                m_FileStream.Close();
                m_FileStream = null;

                if (File.Exists(Task.DownloadPath))
                {
                    File.Delete(Task.DownloadPath);
                }

                File.Move(Utility.Text.Format("{0}.download", Task.DownloadPath), Task.DownloadPath);

                Task.Status = DownloadTaskStatus.Done;

                if (DownloadAgentSuccess != null)
                {
                    DownloadAgentSuccess(this, e.Length);
                }

                Task.Done = true;
            }

            /// <summary>
            /// 关闭下载文件并通知下载失败。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnDownloadAgentHelperError(object sender, DownloadAgentHelperErrorEventArgs e)
            {
                m_Helper.Reset();
                if (m_FileStream != null)
                {
                    m_FileStream.Close();
                    m_FileStream = null;
                }

                if (e.DeleteDownloading)
                {
                    File.Delete(Utility.Text.Format("{0}.download", Task.DownloadPath));
                }

                Task.Status = DownloadTaskStatus.Error;

                if (DownloadAgentFailure != null)
                {
                    DownloadAgentFailure(this, e.ErrorMessage);
                }

                Task.Done = true;
            }
        }
    }
}
