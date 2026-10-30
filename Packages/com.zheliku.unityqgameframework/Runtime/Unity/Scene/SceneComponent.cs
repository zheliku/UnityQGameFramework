//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 场景组件。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    [AddComponentMenu("Unity Q Game Framework/Scene")]
    public sealed class SceneComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private ISceneManager m_SceneManager = null;
        private EventComponent m_EventComponent = null;
        private readonly SortedDictionary<string, int> m_SceneOrder = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private Scene m_GameFrameworkScene = default(Scene);

        [SerializeField]
        private bool m_EnableLoadSceneUpdateEvent = true;

        [SerializeField]
        private bool m_EnableLoadSceneDependencyAssetEvent = true;

        /// <summary>
        /// 获取当前场景主摄像机。
        /// </summary>
        public Camera MainCamera
        {
            get; private set;
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_SceneManager = GameFrameworkEntry.GetModule<ISceneManager>();
            if (m_SceneManager == null)
            {
                Log.Fatal("Scene manager is invalid.");
                return;
            }

            m_SceneManager.LoadSceneSuccess += OnLoadSceneSuccess;
            m_SceneManager.LoadSceneFailure += OnLoadSceneFailure;

            if (m_EnableLoadSceneUpdateEvent)
            {
                m_SceneManager.LoadSceneUpdate += OnLoadSceneUpdate;
            }

            if (m_EnableLoadSceneDependencyAssetEvent)
            {
                m_SceneManager.LoadSceneDependencyAsset += OnLoadSceneDependencyAsset;
            }

            m_SceneManager.UnloadSceneSuccess += OnUnloadSceneSuccess;
            m_SceneManager.UnloadSceneFailure += OnUnloadSceneFailure;

            m_GameFrameworkScene = SceneManager.GetSceneAt(GameEntry.GameFrameworkSceneId);
            if (!m_GameFrameworkScene.IsValid())
            {
                Log.Fatal("Unity Q Game Framework scene is invalid.");
                return;
            }
        }

        /// <summary>
        /// 连接场景管理器与资源组件。
        /// </summary>
        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            m_SceneManager.SetResourceManager(GameEntry.GetComponent<ResourceComponent>().ResourceManager);
        }

        /// <summary>
        /// 获取场景名称。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景名称。</returns>
        public static string GetSceneName(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return null;
            }

            int sceneNamePosition = sceneAssetName.LastIndexOf('/');
            if (sceneNamePosition + 1 >= sceneAssetName.Length)
            {
                Log.Error("Scene asset name '{0}' is invalid.", sceneAssetName);
                return null;
            }

            string sceneName = sceneAssetName.Substring(sceneNamePosition + 1);
            sceneNamePosition = sceneName.LastIndexOf(".unity");
            if (sceneNamePosition > 0)
            {
                sceneName = sceneName.Substring(0, sceneNamePosition);
            }

            return sceneName;
        }

        /// <summary>
        /// 获取场景是否已加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否已加载。</returns>
        public bool SceneIsLoaded(string sceneAssetName)
        {
            return m_SceneManager.SceneIsLoaded(sceneAssetName);
        }

        /// <summary>
        /// 组件销毁后停止转发场景通知，未完成请求仍由各自的 Core 回调收尾。
        /// </summary>
        private void OnDestroy()
        {
            if (m_SceneManager == null)
            {
                return;
            }

            m_SceneManager.LoadSceneSuccess -= OnLoadSceneSuccess;
            m_SceneManager.LoadSceneFailure -= OnLoadSceneFailure;
            m_SceneManager.LoadSceneUpdate -= OnLoadSceneUpdate;
            m_SceneManager.LoadSceneDependencyAsset -= OnLoadSceneDependencyAsset;
            m_SceneManager.UnloadSceneSuccess -= OnUnloadSceneSuccess;
            m_SceneManager.UnloadSceneFailure -= OnUnloadSceneFailure;
        }

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <returns>已加载场景的资源名称。</returns>
        public string[] GetLoadedSceneAssetNames()
        {
            return m_SceneManager.GetLoadedSceneAssetNames();
        }

        /// <summary>
        /// 获取已加载场景的资源名称。
        /// </summary>
        /// <param name="results">已加载场景的资源名称；填充前清空，容量不足时仍会扩容。</param>
        public void GetLoadedSceneAssetNamesNonAlloc(List<string> results)
        {
            m_SceneManager.GetLoadedSceneAssetNamesNonAlloc(results);
        }

        /// <summary>
        /// 获取场景是否正在加载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在加载。</returns>
        public bool SceneIsLoading(string sceneAssetName)
        {
            return m_SceneManager.SceneIsLoading(sceneAssetName);
        }

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <returns>正在加载场景的资源名称。</returns>
        public string[] GetLoadingSceneAssetNames()
        {
            return m_SceneManager.GetLoadingSceneAssetNames();
        }

        /// <summary>
        /// 获取正在加载场景的资源名称。
        /// </summary>
        /// <param name="results">正在加载场景的资源名称；填充前清空，容量不足时仍会扩容。</param>
        public void GetLoadingSceneAssetNamesNonAlloc(List<string> results)
        {
            m_SceneManager.GetLoadingSceneAssetNamesNonAlloc(results);
        }

        /// <summary>
        /// 获取场景是否正在卸载。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <returns>场景是否正在卸载。</returns>
        public bool SceneIsUnloading(string sceneAssetName)
        {
            return m_SceneManager.SceneIsUnloading(sceneAssetName);
        }

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <returns>正在卸载场景的资源名称。</returns>
        public string[] GetUnloadingSceneAssetNames()
        {
            return m_SceneManager.GetUnloadingSceneAssetNames();
        }

        /// <summary>
        /// 获取正在卸载场景的资源名称。
        /// </summary>
        /// <param name="results">正在卸载场景的资源名称；填充前清空，容量不足时仍会扩容。</param>
        public void GetUnloadingSceneAssetNamesNonAlloc(List<string> results)
        {
            m_SceneManager.GetUnloadingSceneAssetNamesNonAlloc(results);
        }

        /// <summary>
        /// 检查场景资源是否存在。
        /// </summary>
        /// <param name="sceneAssetName">要检查场景资源的名称。</param>
        /// <returns>场景资源是否存在。</returns>
        public bool HasScene(string sceneAssetName)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return false;
            }

            return m_SceneManager.HasScene(sceneAssetName);
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        public void LoadScene(string sceneAssetName)
        {
            LoadScene(sceneAssetName, DefaultPriority, null);
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="priority">加载场景资源的优先级。</param>
        public void LoadScene(string sceneAssetName, int priority)
        {
            LoadScene(sceneAssetName, priority, null);
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadScene(string sceneAssetName, object userData)
        {
            LoadScene(sceneAssetName, DefaultPriority, userData);
        }

        /// <summary>
        /// 加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="priority">加载场景资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadScene(string sceneAssetName, int priority, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            m_SceneManager.LoadScene(sceneAssetName, priority, userData);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="cancellationToken">取消标记。场景加载不能立即中止，完成后会补偿卸载。</param>
        /// <returns>场景加载任务；组件销毁时取消等待。</returns>
        public UniTask LoadSceneAsync(string sceneAssetName, CancellationToken cancellationToken = default)
        {
            return LoadSceneAsync(sceneAssetName, DefaultPriority, cancellationToken);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="priority">加载场景资源的优先级。</param>
        /// <param name="cancellationToken">取消标记。场景加载不能立即中止，完成后会补偿卸载。</param>
        /// <returns>场景加载任务；组件销毁时取消等待。</returns>
        public async UniTask LoadSceneAsync(string sceneAssetName, int priority, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISceneManager manager = m_SceneManager;
            var request = new SceneAsyncRequest();
            var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            EventHandler<Core.Events.LoadSceneSuccessEventArgs> successHandler = null;
            EventHandler<Core.Events.LoadSceneFailureEventArgs> failureHandler = null;
            CancellationTokenRegistration cancellation = default;

            void Unsubscribe()
            {
                if (!request.Pending)
                {
                    return;
                }

                request.Pending = false;
                manager.LoadSceneSuccess -= successHandler;
                manager.LoadSceneFailure -= failureHandler;
                cancellation.Dispose();
                lifetime.Dispose();
            }

            successHandler = (sender, args) =>
            {
                if (!ReferenceEquals(args.UserData, request))
                {
                    return;
                }

                Unsubscribe();
                if (request.Cancelled)
                {
                    // Core 回调独立于组件寿命，销毁后到达的场景仍由原管理器补偿卸载。
                    manager.UnloadScene(args.SceneAssetName);
                    return;
                }

                request.Completion.TrySetResult();
            };
            failureHandler = (sender, args) =>
            {
                if (!ReferenceEquals(args.UserData, request))
                {
                    return;
                }

                Unsubscribe();
                if (!request.Cancelled)
                {
                    request.Completion.TrySetException(new GameFrameworkException(args.ErrorMessage));
                }
            };

            manager.LoadSceneSuccess += successHandler;
            manager.LoadSceneFailure += failureHandler;
            cancellation = lifetime.Token.Register(() =>
            {
                request.Cancelled = true;
                request.Completion.TrySetCanceled(lifetime.Token);
            });

            try
            {
                lifetime.Token.ThrowIfCancellationRequested();
                manager.LoadScene(sceneAssetName, priority, request);
                request.Started = true;
                await request.Completion.Task;
            }
            catch
            {
                if (request.Pending && (!request.Cancelled || !request.Started))
                {
                    Unsubscribe();
                }
                throw;
            }
        }

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        public void UnloadScene(string sceneAssetName)
        {
            UnloadScene(sceneAssetName, null);
        }

        /// <summary>
        /// 卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetName, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            m_SceneManager.UnloadScene(sceneAssetName, userData);
            m_SceneOrder.Remove(sceneAssetName);
        }

        /// <summary>
        /// 异步卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="cancellationToken">取消标记。取消只取消等待，不会中止已发出的卸载。</param>
        /// <returns>场景卸载任务；组件销毁时取消等待。</returns>
        public UniTask UnloadSceneAsync(string sceneAssetName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SceneAsyncRequest request = new SceneAsyncRequest();
            EventHandler<GameEventArgs> successHandler = null;
            EventHandler<GameEventArgs> failureHandler = null;
            CancellationTokenRegistration cancellation = default;

            void Unsubscribe()
            {
                if (!request.Pending)
                {
                    return;
                }

                request.Pending = false;
                m_EventComponent.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, successHandler);
                m_EventComponent.Unsubscribe(UnloadSceneFailureEventArgs.EventId, failureHandler);
            }

            successHandler = (sender, eventArgs) =>
            {
                UnloadSceneSuccessEventArgs args = (UnloadSceneSuccessEventArgs)eventArgs;
                if (!ReferenceEquals(args.UserData, request))
                {
                    return;
                }

                Unsubscribe();
                cancellation.Dispose();
                request.Completion.TrySetResult();
            };
            failureHandler = (sender, eventArgs) =>
            {
                UnloadSceneFailureEventArgs args = (UnloadSceneFailureEventArgs)eventArgs;
                if (!ReferenceEquals(args.UserData, request))
                {
                    return;
                }

                Unsubscribe();
                cancellation.Dispose();
                if (!request.Cancelled)
                {
                    request.Completion.TrySetException(new GameFrameworkException("Unload scene failure: " + args.SceneAssetName));
                }
            };

            m_EventComponent.Subscribe(UnloadSceneSuccessEventArgs.EventId, successHandler);
            m_EventComponent.Subscribe(UnloadSceneFailureEventArgs.EventId, failureHandler);
            cancellation = cancellationToken.Register(() =>
            {
                request.Cancelled = true;
                request.Completion.TrySetCanceled(cancellationToken);
            });

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                UnloadScene(sceneAssetName, request);
                request.Started = true;
                return AwaitSceneRequestAsync(request, cancellation, successHandler, failureHandler);
            }
            catch
            {
                if (request.Pending && (!request.Cancelled || !request.Started))
                {
                    Unsubscribe();
                    cancellation.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// 等待卸载请求完成，并在未启动失败时清理事件订阅。
        /// </summary>
        /// <param name="request">场景异步请求。</param>
        /// <param name="cancellation">取消回调的登记。</param>
        /// <param name="successHandler">卸载成功事件处理器。</param>
        /// <param name="failureHandler">卸载失败事件处理器。</param>
        /// <returns>表示请求完成的异步任务。</returns>
        private async UniTask AwaitSceneRequestAsync(SceneAsyncRequest request, CancellationTokenRegistration cancellation,
            EventHandler<GameEventArgs> successHandler, EventHandler<GameEventArgs> failureHandler)
        {
            CancellationToken destroyCancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                await request.Completion.Task.AttachExternalCancellation(destroyCancellationToken);
            }
            catch
            {
                if (request.Pending && (destroyCancellationToken.IsCancellationRequested || !request.Cancelled || !request.Started))
                {
                    request.Pending = false;
                    if (m_EventComponent != null)
                    {
                        if (m_EventComponent.Check(UnloadSceneSuccessEventArgs.EventId, successHandler))
                        {
                            m_EventComponent.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, successHandler);
                        }
                        if (m_EventComponent.Check(UnloadSceneFailureEventArgs.EventId, failureHandler))
                        {
                            m_EventComponent.Unsubscribe(UnloadSceneFailureEventArgs.EventId, failureHandler);
                        }
                    }
                    cancellation.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// 记录场景异步请求的完成、取消和启动状态。
        /// </summary>
        private sealed class SceneAsyncRequest
        {
            /// <summary>
            /// 向异步调用方通知场景操作结果。
            /// </summary>
            internal readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();

            /// <summary>
            /// 是否仍在等待场景事件。
            /// </summary>
            internal bool Pending = true;

            /// <summary>
            /// 调用方是否已取消等待。
            /// </summary>
            internal bool Cancelled;

            /// <summary>
            /// 场景操作是否已成功提交。
            /// </summary>
            internal bool Started;
        }

        /// <summary>
        /// 设置场景顺序。
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称。</param>
        /// <param name="sceneOrder">要设置的场景顺序。</param>
        public void SetSceneOrder(string sceneAssetName, int sceneOrder)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                Log.Error("Scene asset name is invalid.");
                return;
            }

            if (SceneIsLoading(sceneAssetName))
            {
                m_SceneOrder[sceneAssetName] = sceneOrder;
                return;
            }

            if (SceneIsLoaded(sceneAssetName))
            {
                m_SceneOrder[sceneAssetName] = sceneOrder;
                RefreshSceneOrder();
                return;
            }

            Log.Error("Scene '{0}' is not loaded or loading.", sceneAssetName);
        }

        /// <summary>
        /// 刷新当前场景主摄像机。
        /// </summary>
        public void RefreshMainCamera()
        {
            MainCamera = Camera.main;
        }

        /// <summary>
        /// 将已加载场景中顺序最高的场景设为活动场景。
        /// </summary>
        private void RefreshSceneOrder()
        {
            if (m_SceneOrder.Count > 0)
            {
                string maxSceneName = null;
                int maxSceneOrder = 0;
                foreach (KeyValuePair<string, int> sceneOrder in m_SceneOrder)
                {
                    if (SceneIsLoading(sceneOrder.Key))
                    {
                        continue;
                    }

                    if (maxSceneName == null)
                    {
                        maxSceneName = sceneOrder.Key;
                        maxSceneOrder = sceneOrder.Value;
                        continue;
                    }

                    if (sceneOrder.Value > maxSceneOrder)
                    {
                        maxSceneName = sceneOrder.Key;
                        maxSceneOrder = sceneOrder.Value;
                    }
                }

                if (maxSceneName == null)
                {
                    SetActiveScene(m_GameFrameworkScene);
                    return;
                }

                Scene scene = SceneManager.GetSceneByName(GetSceneName(maxSceneName));
                if (!scene.IsValid())
                {
                    string loadedSceneName = GameEntry.GetComponent<ResourceComponent>().GetLoadedSceneName(maxSceneName);
                    if (!string.IsNullOrEmpty(loadedSceneName))
                    {
                        scene = SceneManager.GetSceneByName(loadedSceneName);
                    }
                }
                if (!scene.IsValid())
                {
                    Log.Error("Active scene '{0}' is invalid.", maxSceneName);
                    return;
                }

                SetActiveScene(scene);
            }
            else
            {
                SetActiveScene(m_GameFrameworkScene);
            }
        }

        /// <summary>
        /// 切换活动场景并刷新主摄像机。
        /// </summary>
        /// <param name="activeScene">要设置的活动场景。</param>
        private void SetActiveScene(Scene activeScene)
        {
            Scene lastActiveScene = SceneManager.GetActiveScene();
            if (lastActiveScene != activeScene)
            {
                SceneManager.SetActiveScene(activeScene);
                m_EventComponent.Fire(this, ActiveSceneChangedEventArgs.Create(lastActiveScene, activeScene));
            }

            RefreshMainCamera();
        }

        /// <summary>
        /// 登记场景顺序并派发加载成功事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnLoadSceneSuccess(object sender, UnityQGameFramework.Core.Events.LoadSceneSuccessEventArgs e)
        {
            if (!m_SceneOrder.ContainsKey(e.SceneAssetName))
            {
                m_SceneOrder.Add(e.SceneAssetName, 0);
            }

            m_EventComponent.Fire(this, LoadSceneSuccessEventArgs.Create(e));
            RefreshSceneOrder();
        }

        /// <summary>
        /// 记录场景加载错误并派发失败事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnLoadSceneFailure(object sender, UnityQGameFramework.Core.Events.LoadSceneFailureEventArgs e)
        {
            Log.Warning("Load scene failure, scene asset name '{0}', error message '{1}'.", e.SceneAssetName, e.ErrorMessage);
            m_EventComponent.Fire(this, LoadSceneFailureEventArgs.Create(e));
        }

        /// <summary>
        /// 派发场景加载进度事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnLoadSceneUpdate(object sender, UnityQGameFramework.Core.Events.LoadSceneUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, LoadSceneUpdateEventArgs.Create(e));
        }

        /// <summary>
        /// 派发场景依赖资源加载事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnLoadSceneDependencyAsset(object sender, UnityQGameFramework.Core.Events.LoadSceneDependencyAssetEventArgs e)
        {
            m_EventComponent.Fire(this, LoadSceneDependencyAssetEventArgs.Create(e));
        }

        /// <summary>
        /// 派发卸载成功事件并刷新活动场景。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnUnloadSceneSuccess(object sender, UnityQGameFramework.Core.Events.UnloadSceneSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, UnloadSceneSuccessEventArgs.Create(e));
            m_SceneOrder.Remove(e.SceneAssetName);
            RefreshSceneOrder();
        }

        /// <summary>
        /// 记录场景卸载错误并派发失败事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnUnloadSceneFailure(object sender, UnityQGameFramework.Core.Events.UnloadSceneFailureEventArgs e)
        {
            Log.Warning("Unload scene failure, scene asset name '{0}'.", e.SceneAssetName);
            m_EventComponent.Fire(this, UnloadSceneFailureEventArgs.Create(e));
        }
    }
}
