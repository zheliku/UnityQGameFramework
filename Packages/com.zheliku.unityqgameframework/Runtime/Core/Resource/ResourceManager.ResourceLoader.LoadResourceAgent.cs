//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载资源代理。
            /// </summary>
            private sealed partial class LoadResourceAgent : ITaskAgent<LoadResourceTaskBase>
            {
                private static readonly Dictionary<string, string> s_CachedResourceNames = new Dictionary<string, string>(StringComparer.Ordinal);
                private static readonly HashSet<string> s_LoadingAssetNames = new HashSet<string>(StringComparer.Ordinal);
                private static readonly HashSet<string> s_LoadingResourceNames = new HashSet<string>(StringComparer.Ordinal);

                private readonly IResourceHelper m_ResourceHelper;
                private readonly ResourceLoader m_ResourceLoader;
                private readonly string m_ReadOnlyPath;
                private readonly string m_ReadWritePath;
                private readonly DecryptResourceCallback m_DecryptResourceCallback;

                /// <summary>
                /// 初始化加载资源代理的新实例。
                /// </summary>
                /// <param name="loadResourceAgentHelper">加载资源代理辅助器。</param>
                /// <param name="resourceHelper">资源辅助器。</param>
                /// <param name="resourceLoader">加载资源器。</param>
                /// <param name="readOnlyPath">资源只读区路径。</param>
                /// <param name="readWritePath">资源读写区路径。</param>
                /// <param name="decryptResourceCallback">解密资源回调函数。</param>
                public LoadResourceAgent(ILoadResourceAgentHelper loadResourceAgentHelper, IResourceHelper resourceHelper, ResourceLoader resourceLoader, string readOnlyPath, string readWritePath, DecryptResourceCallback decryptResourceCallback)
                {
                    if (loadResourceAgentHelper == null)
                    {
                        throw new GameFrameworkException("Load resource agent helper is invalid.");
                    }

                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    if (decryptResourceCallback == null)
                    {
                        throw new GameFrameworkException("Decrypt resource callback is invalid.");
                    }

                    Helper = loadResourceAgentHelper;
                    m_ResourceHelper = resourceHelper;
                    m_ResourceLoader = resourceLoader;
                    m_ReadOnlyPath = readOnlyPath;
                    m_ReadWritePath = readWritePath;
                    m_DecryptResourceCallback = decryptResourceCallback;
                    Task = null;
                }

                /// <summary>
                /// 获取资源加载代理辅助器。
                /// </summary>
                public ILoadResourceAgentHelper Helper
                {
                    get;
                }

                /// <summary>
                /// 获取加载资源任务。
                /// </summary>
                public LoadResourceTaskBase Task
                {
                    get; private set;
                }

                /// <summary>
                /// 初始化加载资源代理。
                /// </summary>
                public void Initialize()
                {
                    Helper.LoadResourceAgentHelperUpdate += OnLoadResourceAgentHelperUpdate;
                    Helper.LoadResourceAgentHelperReadFileComplete += OnLoadResourceAgentHelperReadFileComplete;
                    Helper.LoadResourceAgentHelperReadBytesComplete += OnLoadResourceAgentHelperReadBytesComplete;
                    Helper.LoadResourceAgentHelperParseBytesComplete += OnLoadResourceAgentHelperParseBytesComplete;
                    Helper.LoadResourceAgentHelperLoadComplete += OnLoadResourceAgentHelperLoadComplete;
                    Helper.LoadResourceAgentHelperError += OnLoadResourceAgentHelperError;
                }

                /// <summary>
                /// 加载资源代理轮询。
                /// </summary>
                /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
                /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                }

                /// <summary>
                /// 关闭并清理加载资源代理。
                /// </summary>
                public void Shutdown()
                {
                    Reset();
                    Helper.LoadResourceAgentHelperUpdate -= OnLoadResourceAgentHelperUpdate;
                    Helper.LoadResourceAgentHelperReadFileComplete -= OnLoadResourceAgentHelperReadFileComplete;
                    Helper.LoadResourceAgentHelperReadBytesComplete -= OnLoadResourceAgentHelperReadBytesComplete;
                    Helper.LoadResourceAgentHelperParseBytesComplete -= OnLoadResourceAgentHelperParseBytesComplete;
                    Helper.LoadResourceAgentHelperLoadComplete -= OnLoadResourceAgentHelperLoadComplete;
                    Helper.LoadResourceAgentHelperError -= OnLoadResourceAgentHelperError;
                }

                /// <summary>
                /// 清理正在加载的资产和资源登记。
                /// </summary>
                public static void Clear()
                {
                    s_CachedResourceNames.Clear();
                    s_LoadingAssetNames.Clear();
                    s_LoadingResourceNames.Clear();
                }

                /// <summary>
                /// 开始处理加载资源任务。
                /// </summary>
                /// <param name="task">要处理的加载资源任务。</param>
                /// <returns>开始处理任务的状态。</returns>
                public StartTaskStatus Start(LoadResourceTaskBase task)
                {
                    if (task == null)
                    {
                        throw new GameFrameworkException("Task is invalid.");
                    }

                    Task = task;
                    Task.StartTime = DateTime.UtcNow;
                    ResourceInfo resourceInfo = Task.ResourceInfo;

                    if (!resourceInfo.Ready)
                    {
                        Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    if (IsAssetLoading(Task.AssetName))
                    {
                        Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    if (!Task.IsScene)
                    {
                        AssetObject assetObject = m_ResourceLoader.m_AssetPool.Spawn(Task.AssetName);
                        if (assetObject != null)
                        {
                            OnAssetObjectReady(assetObject);
                            return StartTaskStatus.Done;
                        }
                    }

                    foreach (string dependencyAssetName in Task.GetDependencyAssetNames())
                    {
                        if (!m_ResourceLoader.m_AssetPool.CanSpawn(dependencyAssetName))
                        {
                            Task.StartTime = default(DateTime);
                            return StartTaskStatus.HasToWait;
                        }
                    }

                    string resourceName = resourceInfo.ResourceName.Name;
                    if (IsResourceLoading(resourceName))
                    {
                        Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    s_LoadingAssetNames.Add(Task.AssetName);

                    ResourceObject resourceObject = m_ResourceLoader.m_ResourcePool.Spawn(resourceName);
                    if (resourceObject != null)
                    {
                        OnResourceObjectReady(resourceObject);
                        return StartTaskStatus.CanResume;
                    }

                    s_LoadingResourceNames.Add(resourceName);

                    string fullPath = null;
                    if (!s_CachedResourceNames.TryGetValue(resourceName, out fullPath))
                    {
                        fullPath = Utility.Path.GetRegularPath(Path.Combine(resourceInfo.StorageInReadOnly ? m_ReadOnlyPath : m_ReadWritePath, resourceInfo.UseFileSystem ? resourceInfo.FileSystemName : resourceInfo.ResourceName.FullName));
                        s_CachedResourceNames.Add(resourceName, fullPath);
                    }

                    if (resourceInfo.LoadType == LoadType.LoadFromFile)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = m_ResourceLoader.m_ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            Helper.ReadFile(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            Helper.ReadFile(fullPath);
                        }
                    }
                    else if (resourceInfo.LoadType == LoadType.LoadFromMemory || resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = m_ResourceLoader.m_ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            Helper.ReadBytes(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            Helper.ReadBytes(fullPath);
                        }
                    }
                    else
                    {
                        throw new GameFrameworkException(Utility.Text.Format("Resource load type '{0}' is not supported.", resourceInfo.LoadType));
                    }

                    return StartTaskStatus.CanResume;
                }

                /// <summary>
                /// 重置加载资源代理。
                /// </summary>
                public void Reset()
                {
                    Helper.Reset();
                    Task = null;
                }

                /// <summary>
                /// 检查资产是否正在被其他代理加载。
                /// </summary>
                /// <param name="assetName">资产名称。</param>
                /// <returns>资产是否正在加载。</returns>
                private static bool IsAssetLoading(string assetName)
                {
                    return s_LoadingAssetNames.Contains(assetName);
                }

                /// <summary>
                /// 检查资源是否正在被其他代理加载。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                /// <returns>资源是否正在加载。</returns>
                private static bool IsResourceLoading(string resourceName)
                {
                    return s_LoadingResourceNames.Contains(resourceName);
                }

                /// <summary>
                /// 复用已就绪的资产并完成当前加载任务。
                /// </summary>
                /// <param name="assetObject">已就绪的资产登记条目。</param>
                private void OnAssetObjectReady(AssetObject assetObject)
                {
                    Helper.Reset();

                    object asset = assetObject.Target;
                    if (Task.IsScene)
                    {
                        m_ResourceLoader.m_SceneToAssetMap.Add(Task.AssetName, asset);
                    }

                    Task.OnLoadAssetSuccess(this, asset, (float)(DateTime.UtcNow - Task.StartTime).TotalSeconds);
                    Task.Done = true;
                }

                /// <summary>
                /// 使用已就绪的资源加载主资产。
                /// </summary>
                /// <param name="resourceObject">已就绪的资源登记条目。</param>
                private void OnResourceObjectReady(ResourceObject resourceObject)
                {
                    Task.LoadMain(this, resourceObject);
                }

                /// <summary>
                /// 终止当前加载任务并通知失败。
                /// </summary>
                /// <param name="status">加载失败状态。</param>
                /// <param name="errorMessage">错误信息。</param>
                private void OnError(LoadResourceStatus status, string errorMessage)
                {
                    Helper.Reset();
                    Task.OnLoadAssetFailure(this, status, errorMessage);
                    s_LoadingAssetNames.Remove(Task.AssetName);
                    s_LoadingResourceNames.Remove(Task.ResourceInfo.ResourceName.Name);
                    Task.Done = true;
                }

                /// <summary>
                /// 通知当前任务的加载进度。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperUpdate(object sender, LoadResourceAgentHelperUpdateEventArgs e)
                {
                    Task.OnLoadAssetUpdate(this, e.Type, e.Progress);
                }

                /// <summary>
                /// 解析已读取的资源文件。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperReadFileComplete(object sender, LoadResourceAgentHelperReadFileCompleteEventArgs e)
                {
                    ResourceObject resourceObject = ResourceObject.Create(Task.ResourceInfo.ResourceName.Name, e.Resource, m_ResourceHelper, m_ResourceLoader);
                    m_ResourceLoader.m_ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                /// <summary>
                /// 解密并解析已读取的资源字节。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperReadBytesComplete(object sender, LoadResourceAgentHelperReadBytesCompleteEventArgs e)
                {
                    byte[] bytes = e.GetBytes();
                    ResourceInfo resourceInfo = Task.ResourceInfo;
                    if (resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        m_DecryptResourceCallback(bytes, 0, bytes.Length, resourceInfo.ResourceName.Name, resourceInfo.ResourceName.Variant, resourceInfo.ResourceName.Extension, resourceInfo.StorageInReadOnly, resourceInfo.FileSystemName, (byte)resourceInfo.LoadType, resourceInfo.Length, resourceInfo.HashCode);
                    }

                    Helper.ParseBytes(bytes);
                }

                /// <summary>
                /// 登记解析完成的资源并加载主资产。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperParseBytesComplete(object sender, LoadResourceAgentHelperParseBytesCompleteEventArgs e)
                {
                    ResourceObject resourceObject = ResourceObject.Create(Task.ResourceInfo.ResourceName.Name, e.Resource, m_ResourceHelper, m_ResourceLoader);
                    m_ResourceLoader.m_ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                /// <summary>
                /// 登记加载完成的资产并通知当前任务。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperLoadComplete(object sender, LoadResourceAgentHelperLoadCompleteEventArgs e)
                {
                    AssetObject assetObject = null;
                    if (Task.IsScene)
                    {
                        assetObject = m_ResourceLoader.m_AssetPool.Spawn(Task.AssetName);
                    }

                    if (assetObject == null)
                    {
                        List<object> dependencyAssets = Task.GetDependencyAssets();
                        assetObject = AssetObject.Create(Task.AssetName, e.Asset, dependencyAssets, Task.ResourceObject.Target, m_ResourceHelper, m_ResourceLoader);
                        m_ResourceLoader.m_AssetPool.Register(assetObject, true);
                        m_ResourceLoader.m_AssetToResourceMap.Add(e.Asset, Task.ResourceObject.Target);
                        foreach (object dependencyAsset in dependencyAssets)
                        {
                            object dependencyResource = null;
                            if (m_ResourceLoader.m_AssetToResourceMap.TryGetValue(dependencyAsset, out dependencyResource))
                            {
                                Task.ResourceObject.AddDependencyResource(dependencyResource);
                            }
                            else
                            {
                                throw new GameFrameworkException("Can not find dependency resource.");
                            }
                        }
                    }

                    s_LoadingAssetNames.Remove(Task.AssetName);
                    OnAssetObjectReady(assetObject);
                }

                /// <summary>
                /// 处理资源加载辅助器错误。
                /// </summary>
                /// <param name="sender">事件发送者。</param>
                /// <param name="e">事件参数。</param>
                private void OnLoadResourceAgentHelperError(object sender, LoadResourceAgentHelperErrorEventArgs e)
                {
                    OnError(e.Status, e.ErrorMessage);
                }
            }
        }
    }
}
