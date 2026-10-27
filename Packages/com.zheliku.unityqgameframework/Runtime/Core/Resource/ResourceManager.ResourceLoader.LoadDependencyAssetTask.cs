//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载依赖资产的任务。
            /// </summary>
            private sealed class LoadDependencyAssetTask : LoadResourceTaskBase
            {
                private LoadResourceTaskBase m_MainTask;

                /// <summary>
                /// 初始化依赖资产加载任务。
                /// </summary>
                public LoadDependencyAssetTask()
                {
                    m_MainTask = null;
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
                /// 从引用池获取依赖资产加载任务。
                /// </summary>
                /// <param name="assetName">资产名称。</param>
                /// <param name="priority">加载优先级。</param>
                /// <param name="resourceInfo">资产所属的资源记录。</param>
                /// <param name="dependencyAssetNames">资产依赖名称数组。</param>
                /// <param name="mainTask">等待该依赖资产的主任务。</param>
                /// <param name="userData">用户自定义数据。</param>
                /// <returns>依赖资产加载任务。</returns>
                public static LoadDependencyAssetTask Create(string assetName, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadResourceTaskBase mainTask, object userData)
                {
                    LoadDependencyAssetTask loadDependencyAssetTask = ReferencePool.Acquire<LoadDependencyAssetTask>();
                    loadDependencyAssetTask.Initialize(assetName, null, priority, resourceInfo, dependencyAssetNames, userData);
                    loadDependencyAssetTask.m_MainTask = mainTask;
                    loadDependencyAssetTask.m_MainTask.TotalDependencyAssetCount++;
                    return loadDependencyAssetTask;
                }

                /// <summary>
                /// 清理依赖资产加载任务数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    m_MainTask = null;
                }

                /// <summary>
                /// 处理依赖资产加载成功。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="asset">加载完成的资产。</param>
                /// <param name="duration">加载耗时，以秒为单位。</param>
                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    m_MainTask.OnLoadDependencyAsset(agent, AssetName, asset);
                }

                /// <summary>
                /// 处理依赖资产加载失败。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="status">加载失败状态。</param>
                /// <param name="errorMessage">错误信息。</param>
                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    m_MainTask.OnLoadAssetFailure(agent, LoadResourceStatus.DependencyError, Utility.Text.Format("Can not load dependency asset '{0}', internal status '{1}', internal error message '{2}'.", AssetName, status, errorMessage));
                }
            }
        }
    }
}
