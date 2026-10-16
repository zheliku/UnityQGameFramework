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
            private abstract class LoadResourceTaskBase : TaskBase
            {
                private static int s_Serial = 0;

                private string[] m_DependencyAssetNames;
                private readonly List<object> m_DependencyAssets;

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

                public string AssetName { get; private set; }

                public Type AssetType { get; private set; }

                public ResourceInfo ResourceInfo { get; private set; }

                public ResourceObject ResourceObject { get; private set; }

                public abstract bool IsScene { get; }

                public DateTime StartTime { get; set; }

                public int LoadedDependencyAssetCount => m_DependencyAssets.Count;

                public int TotalDependencyAssetCount { get; set; }

                public override string Description => AssetName;

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

                public string[] GetDependencyAssetNames()
                {
                    return m_DependencyAssetNames;
                }

                public List<object> GetDependencyAssets()
                {
                    return m_DependencyAssets;
                }

                public void LoadMain(LoadResourceAgent agent, ResourceObject resourceObject)
                {
                    ResourceObject = resourceObject;
                    agent.Helper.LoadAsset(resourceObject.Target, AssetName, AssetType, IsScene);
                }

                public virtual void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                }

                public virtual void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                }

                public virtual void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                }

                public virtual void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    m_DependencyAssets.Add(dependencyAsset);
                }

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
