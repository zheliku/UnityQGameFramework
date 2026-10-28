//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class WebRequestManager : GameFrameworkModule, IWebRequestManager
    {
        /// <summary>
        /// Web 请求任务。
        /// </summary>
        private sealed class WebRequestTask : TaskBase
        {
            private static int s_Serial = 0;

            private byte[] m_PostData;

            /// <summary>
            /// 初始化 Web 请求任务。
            /// </summary>
            public WebRequestTask()
            {
                Status = WebRequestTaskStatus.Todo;
                WebRequestUri = null;
                m_PostData = null;
                Timeout = 0f;
            }

            /// <summary>
            /// 获取或设置 Web 请求任务的状态。
            /// </summary>
            public WebRequestTaskStatus Status
            {
                get; set;
            }

            /// <summary>
            /// 获取要发送的远程地址。
            /// </summary>
            public string WebRequestUri
            {
                get; private set;
            }

            /// <summary>
            /// 获取 Web 请求超时时长，以秒为单位。
            /// </summary>
            public float Timeout
            {
                get; private set;
            }

            /// <summary>
            /// 获取 Web 请求任务的描述。
            /// </summary>
            public override string Description
            {
                get
                {
                    return WebRequestUri;
                }
            }

            /// <summary>
            /// 创建 Web 请求任务。
            /// </summary>
            /// <param name="webRequestUri">要发送的远程地址。</param>
            /// <param name="postData">要发送的数据流。</param>
            /// <param name="tag">Web 请求任务的标签。</param>
            /// <param name="priority">Web 请求任务的优先级。</param>
            /// <param name="timeout">下载超时时长，以秒为单位。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的 Web 请求任务。</returns>
            public static WebRequestTask Create(string webRequestUri, byte[] postData, string tag, int priority,
                float timeout, object userData)
            {
                WebRequestTask webRequestTask = ReferencePool.Acquire<WebRequestTask>();
                webRequestTask.Initialize(++s_Serial, tag, priority, userData);
                webRequestTask.WebRequestUri = webRequestUri;
                webRequestTask.m_PostData = postData;
                webRequestTask.Timeout = timeout;
                return webRequestTask;
            }

            /// <summary>
            /// 清理 Web 请求任务。
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                Status = WebRequestTaskStatus.Todo;
                WebRequestUri = null;
                m_PostData = null;
                Timeout = 0f;
            }

            /// <summary>
            /// 获取要发送的数据流。
            /// </summary>
            /// <returns>POST 请求数据；未提供时返回空。</returns>
            public byte[] GetPostData()
            {
                return m_PostData;
            }
        }
    }
}
