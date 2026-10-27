//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 资源加载任务的公共状态与回调。
            /// </summary>
            private abstract class LoadResourceTaskBase : TaskBase
            {
                private static int s_Serial = 0;

                private string[] m_DependencyAssetNames;
                private readonly List<object> m_DependencyAssets;

                /// <summary>
                /// 初始化资源加载任务的公共状态。
                /// </summary>
                public LoadResourceTaskBase()
                {
                    AssetName = null;
                    AssetType = null;
                    ResourceInfo = null;
                    m_DependencyAssetNames = null;
                    m_DependencyAssets = new List<object>();
                    ResourceObject = null;
                    StartTime = default(DateTime);
                    TotalDependencyAssetCount = 0;
                }

                /// <summary>
                /// 获取资产名称。
                /// </summary>
                public string AssetName { get; private set; }

                /// <summary>
                /// 获取资产类型。
                /// </summary>
                public Type AssetType { get; private set; }

                /// <summary>
                /// 获取资产所属的资源记录。
                /// </summary>
                public ResourceInfo ResourceInfo { get; private set; }

                /// <summary>
                /// 获取已加载的资源登记条目。
                /// </summary>
                public ResourceObject ResourceObject { get; private set; }

                /// <summary>
                /// 获取当前任务是否加载场景。
                /// </summary>
                public abstract bool IsScene { get; }

                /// <summary>
                /// 获取或设置任务开始时间。
                /// </summary>
                public DateTime StartTime { get; set; }

                /// <summary>
                /// 获取已加载依赖资产数量。
                /// </summary>
                public int LoadedDependencyAssetCount
                {
                    get
                    {
                        return m_DependencyAssets.Count;
                    }
                }

                /// <summary>
                /// 获取或设置依赖资产总数量。
                /// </summary>
                public int TotalDependencyAssetCount { get; set; }

                /// <summary>
                /// 获取任务描述。
                /// </summary>
                public override string Description
                {
                    get
                    {
                        return AssetName;
                    }
                }

                /// <summary>
                /// 清理资源加载任务数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    AssetName = null;
                    AssetType = null;
                    ResourceInfo = null;
                    m_DependencyAssetNames = null;
                    m_DependencyAssets.Clear();
                    ResourceObject = null;
                    StartTime = default(DateTime);
                    TotalDependencyAssetCount = 0;
                }

                /// <summary>
                /// 获取依赖资产名称数组。
                /// </summary>
                /// <returns>依赖资产名称数组。</returns>
                public string[] GetDependencyAssetNames()
                {
                    return m_DependencyAssetNames;
                }

                /// <summary>
                /// 获取已加载的依赖资产列表。
                /// </summary>
                /// <returns>当前任务持有的依赖资产列表。</returns>
                public List<object> GetDependencyAssets()
                {
                    return m_DependencyAssets;
                }

                /// <summary>
                /// 使用已就绪的资源对象加载主资产。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="resourceObject">已加载的资源登记条目。</param>
                public void LoadMain(LoadResourceAgent agent, ResourceObject resourceObject)
                {
                    ResourceObject = resourceObject;
                    agent.Helper.LoadAsset(resourceObject.Target, AssetName, AssetType, IsScene);
                }

                /// <summary>
                /// 处理资源加载成功。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="asset">加载完成的资产。</param>
                /// <param name="duration">加载耗时，以秒为单位。</param>
                public virtual void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                }

                /// <summary>
                /// 处理资源加载失败。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="status">加载失败状态。</param>
                /// <param name="errorMessage">错误信息。</param>
                public virtual void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                }

                /// <summary>
                /// 通知资源加载进度。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="type">当前加载阶段。</param>
                /// <param name="progress">当前阶段的加载进度。</param>
                public virtual void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                }

                /// <summary>
                /// 记录已加载的依赖资产并通知加载进度。
                /// </summary>
                /// <param name="agent">执行当前任务的资源加载代理。</param>
                /// <param name="dependencyAssetName">加载完成的依赖资产名称。</param>
                /// <param name="dependencyAsset">加载完成的依赖资产。</param>
                public virtual void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    m_DependencyAssets.Add(dependencyAsset);
                }

                /// <summary>
                /// 初始化资源加载任务。
                /// </summary>
                /// <param name="assetName">资产名称。</param>
                /// <param name="assetType">资产类型。</param>
                /// <param name="priority">加载优先级。</param>
                /// <param name="resourceInfo">资产所属的资源记录。</param>
                /// <param name="dependencyAssetNames">资产依赖名称数组。</param>
                /// <param name="userData">用户自定义数据。</param>
                protected void Initialize(string assetName, Type assetType, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, object userData)
                {
                    Initialize(++s_Serial, null, priority, userData);
                    AssetName = assetName;
                    AssetType = assetType;
                    ResourceInfo = resourceInfo;
                    m_DependencyAssetNames = dependencyAssetNames;
                }
            }
        }
    }
}
