//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载主资产及其依赖的任务。
            /// </summary>
            private sealed class LoadAssetTask : LoadResourceTaskBase
            {
                private LoadAssetCallbacks m_LoadAssetCallbacks;

                /// <summary>
                /// 初始化资产加载任务。
                /// </summary>
                public LoadAssetTask()
                {
                    m_LoadAssetCallbacks = null;
                }

                /// <summary>
                /// 获取当前任务是否加载场景。
                /// </summary>
                public override bool IsScene
                {
                    get
                    {
                        return false;
                    }
                }

                /// <summary>
                /// 从引用池获取资产加载任务。
                /// </summary>
                /// <param name="assetName">资产名称。</param>
                /// <param name="assetType">资产类型。</param>
                /// <param name="priority">加载优先级。</param>
                /// <param name="resourceInfo">资产所属的资源记录。</param>
                /// <param name="dependencyAssetNames">资产依赖名称数组。</param>
                /// <param name="loadAssetCallbacks">资产加载回调。</param>
                /// <param name="userData">用户自定义数据。</param>
                /// <returns>资产加载任务。</returns>
                public static LoadAssetTask Create(string assetName, Type assetType, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadAssetCallbacks loadAssetCallbacks, object userData)
                {
                    LoadAssetTask loadAssetTask = ReferencePool.Acquire<LoadAssetTask>();
                    loadAssetTask.Initialize(assetName, assetType, priority, resourceInfo, dependencyAssetNames, userData);
                    loadAssetTask.m_LoadAssetCallbacks = loadAssetCallbacks;
                    return loadAssetTask;
                }

                /// <summary>
                /// 清理资产加载任务数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    m_LoadAssetCallbacks = null;
                }

                /// <summary>
                /// 处理资产加载成功。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="asset">加载完成的资产。</param>
                /// <param name="duration">加载耗时，以秒为单位。</param>
                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    if (m_LoadAssetCallbacks.LoadAssetSuccessCallback != null)
                    {
                        m_LoadAssetCallbacks.LoadAssetSuccessCallback(AssetName, asset, duration, UserData);
                    }
                }

                /// <summary>
                /// 处理资产加载失败。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="status">加载失败状态。</param>
                /// <param name="errorMessage">错误信息。</param>
                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    if (m_LoadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        m_LoadAssetCallbacks.LoadAssetFailureCallback(AssetName, status, errorMessage, UserData);
                    }
                }

                /// <summary>
                /// 通知资产加载进度。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="type">当前加载阶段。</param>
                /// <param name="progress">当前阶段的加载进度。</param>
                public override void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                    base.OnLoadAssetUpdate(agent, type, progress);
                    if (type == LoadResourceProgress.LoadAsset)
                    {
                        if (m_LoadAssetCallbacks.LoadAssetUpdateCallback != null)
                        {
                            m_LoadAssetCallbacks.LoadAssetUpdateCallback(AssetName, progress, UserData);
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
                    if (m_LoadAssetCallbacks.LoadAssetDependencyAssetCallback != null)
                    {
                        m_LoadAssetCallbacks.LoadAssetDependencyAssetCallback(AssetName, dependencyAssetName, LoadedDependencyAssetCount, TotalDependencyAssetCount, UserData);
                    }
                }
            }
        }
    }
}
