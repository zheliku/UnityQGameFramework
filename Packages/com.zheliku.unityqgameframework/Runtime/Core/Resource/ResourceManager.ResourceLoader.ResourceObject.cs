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
            private sealed class ResourceObject : ObjectBase
            {
                private List<object> m_DependencyResources;
                private IResourceHelper m_ResourceHelper;
                private ResourceLoader m_ResourceLoader;

                /// <summary>
                /// 初始化资源的池内登记条目。
                /// </summary>
                public ResourceObject()
                {
                    m_DependencyResources = new List<object>();
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                /// <summary>
                /// 获取资源是否未被其他资源依赖且允许释放。
                /// </summary>
                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                /// <summary>
                /// 从引用池获取资源登记条目。
                /// </summary>
                /// <param name="name">资源名称。</param>
                /// <param name="target">被池管理的实际资源对象。</param>
                /// <param name="resourceHelper">资源辅助器。</param>
                /// <param name="resourceLoader">资源加载器。</param>
                /// <returns>资源的池内登记条目。</returns>
                public static ResourceObject Create(string name, object target, IResourceHelper resourceHelper, ResourceLoader resourceLoader)
                {
                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    ResourceObject resourceObject = ReferencePool.Acquire<ResourceObject>();
                    resourceObject.Initialize(name, target);
                    resourceObject.m_ResourceHelper = resourceHelper;
                    resourceObject.m_ResourceLoader = resourceLoader;
                    return resourceObject;
                }

                /// <summary>
                /// 清理资源登记数据。
                /// </summary>
                public override void Clear()
                {
                    base.Clear();
                    m_DependencyResources.Clear();
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                /// <summary>
                /// 登记资源依赖并增加依赖引用计数。
                /// </summary>
                /// <param name="dependencyResource">当前资源依赖的资源对象。</param>
                public void AddDependencyResource(object dependencyResource)
                {
                    if (Target == dependencyResource)
                    {
                        return;
                    }

                    if (m_DependencyResources.Contains(dependencyResource))
                    {
                        return;
                    }

                    m_DependencyResources.Add(dependencyResource);

                    int referenceCount = 0;
                    if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                    {
                        m_ResourceLoader.m_ResourceDependencyCount[dependencyResource] = referenceCount + 1;
                    }
                    else
                    {
                        m_ResourceLoader.m_ResourceDependencyCount.Add(dependencyResource, 1);
                    }
                }

                /// <summary>
                /// 释放资源对象并更新依赖引用计数。
                /// </summary>
                /// <param name="isShutdown">是否正在关闭框架。</param>
                protected internal override void Release(bool isShutdown)
                {
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount));
                        }

                        foreach (object dependencyResource in m_DependencyResources)
                        {
                            int referenceCount = 0;
                            if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                            {
                                m_ResourceLoader.m_ResourceDependencyCount[dependencyResource] = referenceCount - 1;
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }
                    }

                    m_ResourceLoader.m_ResourceDependencyCount.Remove(Target);
                    m_ResourceHelper.Release(Target);
                }
            }
        }
    }
}
