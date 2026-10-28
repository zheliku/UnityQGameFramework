//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 资源对象。
            /// </summary>
            private sealed class AssetObject : ObjectBase
            {
                private List<object> m_DependencyAssets;
                private object m_Resource;
                private IResourceHelper m_ResourceHelper;
                private ResourceLoader m_ResourceLoader;

                /// <summary>
                /// 初始化资产的池内登记条目。
                /// </summary>
                public AssetObject()
                {
                    m_DependencyAssets = new List<object>();
                    m_Resource = null;
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                /// <summary>
                /// 获取资产是否未被其他资产依赖且允许释放。
                /// </summary>
                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        m_ResourceLoader.m_AssetDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                /// <summary>
                /// 从引用池获取资产登记条目。
                /// </summary>
                /// <param name="name">资产名称。</param>
                /// <param name="target">被池管理的实际资产对象。</param>
                /// <param name="dependencyAssets">该资产引用的依赖资产列表。</param>
                /// <param name="resource">资产所属的资源对象。</param>
                /// <param name="resourceHelper">资源辅助器。</param>
                /// <param name="resourceLoader">资源加载器。</param>
                /// <returns>资产的池内登记条目。</returns>
                public static AssetObject Create(string name, object target, List<object> dependencyAssets, object resource, IResourceHelper resourceHelper, ResourceLoader resourceLoader)
                {
                    if (dependencyAssets == null)
                    {
                        throw new GameFrameworkException("Dependency assets is invalid.");
                    }

                    if (resource == null)
                    {
                        throw new GameFrameworkException("Resource is invalid.");
                    }

                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    AssetObject assetObject = ReferencePool.Acquire<AssetObject>();
                    assetObject.Initialize(name, target);
                    assetObject.m_DependencyAssets.AddRange(dependencyAssets);
                    assetObject.m_Resource = resource;
                    assetObject.m_ResourceHelper = resourceHelper;
                    assetObject.m_ResourceLoader = resourceLoader;

                    foreach (object dependencyAsset in dependencyAssets)
                    {
                        int referenceCount = 0;
                        if (resourceLoader.m_AssetDependencyCount.TryGetValue(dependencyAsset, out referenceCount))
                        {
                            resourceLoader.m_AssetDependencyCount[dependencyAsset] = referenceCount + 1;
                        }
                        else
                        {
                            resourceLoader.m_AssetDependencyCount.Add(dependencyAsset, 1);
                        }
                    }

                    return assetObject;
                }

                /// <summary>
                /// 清理资产登记数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    m_DependencyAssets.Clear();
                    m_Resource = null;
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                /// <summary>
                /// 归还当前资产借用的依赖资产。
                /// </summary>
                protected internal override void OnUnspawn()
                {
                    base.OnUnspawn();
                    foreach (object dependencyAsset in m_DependencyAssets)
                    {
                        m_ResourceLoader.m_AssetPool.Unspawn(dependencyAsset);
                    }
                }

                /// <summary>
                /// 释放资产对象并更新依赖引用计数。
                /// </summary>
                /// <param name="isShutdown">是否正在关闭框架。</param>
                protected internal override void Release(bool isShutdown)
                {
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        if (m_ResourceLoader.m_AssetDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Asset target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount));
                        }

                        foreach (object dependencyAsset in m_DependencyAssets)
                        {
                            int referenceCount = 0;
                            if (m_ResourceLoader.m_AssetDependencyCount.TryGetValue(dependencyAsset, out referenceCount))
                            {
                                m_ResourceLoader.m_AssetDependencyCount[dependencyAsset] = referenceCount - 1;
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Asset target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }

                        m_ResourceLoader.m_ResourcePool.Unspawn(m_Resource);
                    }

                    m_ResourceLoader.m_AssetDependencyCount.Remove(Target);
                    m_ResourceLoader.m_AssetToResourceMap.Remove(Target);
                    m_ResourceHelper.Release(Target);
                }
            }
        }
    }
}
