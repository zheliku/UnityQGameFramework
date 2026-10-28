//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Events;

namespace UnityQGameFramework.Core
{
    internal sealed partial class WebRequestManager : GameFrameworkModule, IWebRequestManager
    {
        /// <summary>
        /// Web 请求代理。
        /// </summary>
        private sealed class WebRequestAgent : ITaskAgent<WebRequestTask>
        {
            private readonly IWebRequestAgentHelper m_Helper;

            /// <summary>
            /// Web 请求开始回调。
            /// </summary>
            public GameFrameworkAction<WebRequestAgent> WebRequestAgentStart;

            /// <summary>
            /// Web 请求成功回调。
            /// </summary>
            public GameFrameworkAction<WebRequestAgent, byte[]> WebRequestAgentSuccess;

            /// <summary>
            /// Web 请求失败回调。
            /// </summary>
            public GameFrameworkAction<WebRequestAgent, string> WebRequestAgentFailure;

            /// <summary>
            /// 初始化 Web 请求代理的新实例。
            /// </summary>
            /// <param name="webRequestAgentHelper">Web 请求代理辅助器。</param>
            public WebRequestAgent(IWebRequestAgentHelper webRequestAgentHelper)
            {
                if (webRequestAgentHelper == null)
                {
                    throw new GameFrameworkException("Web request agent helper is invalid.");
                }

                m_Helper = webRequestAgentHelper;
                Task = null;
                WaitTime = 0f;

                WebRequestAgentStart = null;
                WebRequestAgentSuccess = null;
                WebRequestAgentFailure = null;
            }

            /// <summary>
            /// 获取 Web 请求任务。
            /// </summary>
            public WebRequestTask Task
            {
                get; private set;
            }

            /// <summary>
            /// 获取已经等待时间。
            /// </summary>
            public float WaitTime
            {
                get; private set;
            }

            /// <summary>
            /// 初始化 Web 请求代理。
            /// </summary>
            public void Initialize()
            {
                m_Helper.WebRequestAgentHelperComplete += OnWebRequestAgentHelperComplete;
                m_Helper.WebRequestAgentHelperError += OnWebRequestAgentHelperError;
            }

            /// <summary>
            /// Web 请求代理轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (Task.Status == WebRequestTaskStatus.Doing)
                {
                    WaitTime += realElapseSeconds;
                    if (WaitTime >= Task.Timeout)
                    {
                        WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create("Timeout");
                        OnWebRequestAgentHelperError(this, webRequestAgentHelperErrorEventArgs);
                        ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
                    }
                }
            }

            /// <summary>
            /// 关闭并清理 Web 请求代理。
            /// </summary>
            public void Shutdown()
            {
                Reset();
                m_Helper.WebRequestAgentHelperComplete -= OnWebRequestAgentHelperComplete;
                m_Helper.WebRequestAgentHelperError -= OnWebRequestAgentHelperError;
            }

            /// <summary>
            /// 开始处理 Web 请求任务。
            /// </summary>
            /// <param name="task">要处理的 Web 请求任务。</param>
            /// <returns>开始处理任务的状态。</returns>
            public StartTaskStatus Start(WebRequestTask task)
            {
                if (task == null)
                {
                    throw new GameFrameworkException("Task is invalid.");
                }

                Task = task;
                Task.Status = WebRequestTaskStatus.Doing;

                if (WebRequestAgentStart != null)
                {
                    WebRequestAgentStart(this);
                }

                byte[] postData = Task.GetPostData();
                if (postData == null)
                {
                    m_Helper.Request(Task.WebRequestUri, Task.UserData);
                }
                else
                {
                    m_Helper.Request(Task.WebRequestUri, postData, Task.UserData);
                }

                WaitTime = 0f;
                return StartTaskStatus.CanResume;
            }

            /// <summary>
            /// 重置 Web 请求代理。
            /// </summary>
            public void Reset()
            {
                m_Helper.Reset();
                Task = null;
                WaitTime = 0f;
            }

            /// <summary>
            /// 通知 Web 请求成功。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnWebRequestAgentHelperComplete(object sender, WebRequestAgentHelperCompleteEventArgs e)
            {
                m_Helper.Reset();
                Task.Status = WebRequestTaskStatus.Done;

                if (WebRequestAgentSuccess != null)
                {
                    WebRequestAgentSuccess(this, e.GetWebResponseBytes());
                }

                Task.Done = true;
            }

            /// <summary>
            /// 通知 Web 请求失败。
            /// </summary>
            /// <param name="sender">事件发送者。</param>
            /// <param name="e">事件参数。</param>
            private void OnWebRequestAgentHelperError(object sender, WebRequestAgentHelperErrorEventArgs e)
            {
                m_Helper.Reset();
                Task.Status = WebRequestTaskStatus.Error;

                if (WebRequestAgentFailure != null)
                {
                    WebRequestAgentFailure(this, e.ErrorMessage);
                }

                Task.Done = true;
            }
        }
    }
}
