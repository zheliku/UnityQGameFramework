//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载场景及其依赖资源的任务。
            /// </summary>
            private sealed class LoadSceneTask : LoadResourceTaskBase
            {
                private LoadSceneCallbacks m_LoadSceneCallbacks;

                /// <summary>
                /// 初始化场景加载任务。
                /// </summary>
                public LoadSceneTask()
                {
                    m_LoadSceneCallbacks = null;
                }

                /// <summary>
                /// 获取当前任务是否加载场景。
                /// </summary>
                public override bool IsScene
                {
                    get
                    {
                        return true;
                    }
                }

                /// <summary>
                /// 从引用池获取场景加载任务。
                /// </summary>
                /// <param name="sceneAssetName">场景资产名称。</param>
                /// <param name="priority">加载优先级。</param>
                /// <param name="resourceInfo">资产所属的资源记录。</param>
                /// <param name="dependencyAssetNames">资产依赖名称数组。</param>
                /// <param name="loadSceneCallbacks">场景加载回调。</param>
                /// <param name="userData">用户自定义数据。</param>
                /// <returns>场景加载任务。</returns>
                public static LoadSceneTask Create(string sceneAssetName, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadSceneCallbacks loadSceneCallbacks, object userData)
                {
                    LoadSceneTask loadSceneTask = ReferencePool.Acquire<LoadSceneTask>();
                    loadSceneTask.Initialize(sceneAssetName, null, priority, resourceInfo, dependencyAssetNames, userData);
                    loadSceneTask.m_LoadSceneCallbacks = loadSceneCallbacks;
                    return loadSceneTask;
                }

                /// <summary>
                /// 清理场景加载任务数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    m_LoadSceneCallbacks = null;
                }

                /// <summary>
                /// 处理场景加载成功。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="asset">加载完成的资产。</param>
                /// <param name="duration">加载耗时，以秒为单位。</param>
                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    if (m_LoadSceneCallbacks.LoadSceneSuccessCallback != null)
                    {
                        m_LoadSceneCallbacks.LoadSceneSuccessCallback(AssetName, duration, UserData);
                    }
                }

                /// <summary>
                /// 处理场景加载失败。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="status">加载失败状态。</param>
                /// <param name="errorMessage">错误信息。</param>
                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    if (m_LoadSceneCallbacks.LoadSceneFailureCallback != null)
                    {
                        m_LoadSceneCallbacks.LoadSceneFailureCallback(AssetName, status, errorMessage, UserData);
                    }
                }

                /// <summary>
                /// 通知场景加载进度。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="type">当前加载阶段。</param>
                /// <param name="progress">当前阶段的加载进度。</param>
                public override void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                    base.OnLoadAssetUpdate(agent, type, progress);
                    if (type == LoadResourceProgress.LoadScene)
                    {
                        if (m_LoadSceneCallbacks.LoadSceneUpdateCallback != null)
                        {
                            m_LoadSceneCallbacks.LoadSceneUpdateCallback(AssetName, progress, UserData);
                        }
                    }
                }

                /// <summary>
                /// 记录已加载的依赖资产并通知加载进度。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="dependencyAssetName">加载完成的依赖资产名称。</param>
                /// <param name="dependencyAsset">加载完成的依赖资产。</param>
                public override void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    base.OnLoadDependencyAsset(agent, dependencyAssetName, dependencyAsset);
                    if (m_LoadSceneCallbacks.LoadSceneDependencyAssetCallback != null)
                    {
                        m_LoadSceneCallbacks.LoadSceneDependencyAssetCallback(AssetName, dependencyAssetName, LoadedDependencyAssetCount, TotalDependencyAssetCount, UserData);
                    }
                }
            }
        }
    }
}
