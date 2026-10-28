//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源组件。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    [AddComponentMenu("Unity Q Game Framework/Resource")]
    public sealed partial class ResourceComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;
        private const int OneMegaBytes = 1024 * 1024;

        private IResourceManager m_ResourceManager = null;
        private EventComponent m_EventComponent = null;
        private bool m_ForceUnloadUnusedAssets = false;
        private bool m_PreorderUnloadUnusedAssets = false;
        private bool m_PerformGCCollect = false;
        private AsyncOperation m_AsyncOperation = null;
        private float m_MinUnloadUnusedAssetsInterval = 60f;
        private float m_MaxUnloadUnusedAssetsInterval = 300f;
        private float m_AssetAutoReleaseInterval = 60f;
        private int m_AssetCapacity = 64;
        private float m_AssetExpireTime = 60f;
        private int m_AssetPriority;
        private float m_ResourceAutoReleaseInterval = 60f;
        private int m_ResourceCapacity = 16;
        private float m_ResourceExpireTime = 60f;
        private int m_ResourcePriority;
        private string m_UpdatePrefixUri;
        private int m_GenerateReadWriteVersionListLength = OneMegaBytes;
        private int m_UpdateRetryCount = 3;

        /// <summary>
        /// 获取资源只读路径。
        /// </summary>
        public string ReadOnlyPath
        {
            get
            {
                return m_ResourceManager.ReadOnlyPath;
            }
        }

        /// <summary>
        /// 获取资源读写路径。
        /// </summary>
        public string ReadWritePath
        {
            get
            {
                return m_ResourceManager.ReadWritePath;
            }
        }

        /// <summary>
        /// 获取资源模式。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ResourceMode ResourceMode
        {
            get
            {
                return m_ResourceManager.ResourceMode;
            }
        }

        /// <summary>
        /// 设置当前变体。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string CurrentVariant
        {
            get
            {
                return m_ResourceManager.CurrentVariant;
            }
        }

        /// <summary>
        /// 获取单机模式版本资源列表序列化器。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public PackageVersionListSerializer PackageVersionListSerializer
        {
            get
            {
                return m_ResourceManager.PackageVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取可更新模式版本资源列表序列化器。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public UpdatableVersionListSerializer UpdatableVersionListSerializer
        {
            get
            {
                return m_ResourceManager.UpdatableVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取本地只读区版本资源列表序列化器。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ReadOnlyVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取本地读写区版本资源列表序列化器。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ReadWriteVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取资源包版本资源列表序列化器。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer
        {
            get
            {
                return m_ResourceManager.ResourcePackVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取无用资源释放的等待时长，以秒为单位。
        /// </summary>
        public float LastUnloadUnusedAssetsOperationElapseSeconds
        {
            get; private set;
        }

        /// <summary>
        /// 获取或设置无用资源释放的最小间隔时间，以秒为单位。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float MinUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MinUnloadUnusedAssetsInterval;
            }
            set
            {
                m_MinUnloadUnusedAssetsInterval = value;
            }
        }

        /// <summary>
        /// 获取或设置无用资源释放的最大间隔时间，以秒为单位。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float MaxUnloadUnusedAssetsInterval
        {
            get
            {
                return m_MaxUnloadUnusedAssetsInterval;
            }
            set
            {
                m_MaxUnloadUnusedAssetsInterval = value;
            }
        }

        /// <summary>
        /// 获取当前资源适用的游戏版本号。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string ApplicableGameVersion
        {
            get
            {
                return m_ResourceManager.ApplicableGameVersion;
            }
        }

        /// <summary>
        /// 获取当前内部资源版本号。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int InternalResourceVersion
        {
            get
            {
                return m_ResourceManager.InternalResourceVersion;
            }
        }

        /// <summary>
        /// 获取资源数量。
        /// </summary>
        public int AssetCount
        {
            get
            {
                return m_ResourceManager.AssetCount;
            }
        }

        /// <summary>
        /// 获取资源数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int ResourceCount
        {
            get
            {
                return m_ResourceManager.ResourceCount;
            }
        }

        /// <summary>
        /// 获取资源组数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int ResourceGroupCount
        {
            get
            {
                return m_ResourceManager.ResourceGroupCount;
            }
        }

        /// <summary>
        /// 获取或设置资源更新下载地址。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string UpdatePrefixUri
        {
            get
            {
                return m_ResourceManager.UpdatePrefixUri;
            }
            set
            {
                m_ResourceManager.UpdatePrefixUri = m_UpdatePrefixUri = value;
            }
        }

        /// <summary>
        /// 获取或设置每更新多少字节的资源，重新生成一次版本资源列表。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GenerateReadWriteVersionListLength
        {
            get
            {
                return m_ResourceManager.GenerateReadWriteVersionListLength;
            }
            set
            {
                m_ResourceManager.GenerateReadWriteVersionListLength = m_GenerateReadWriteVersionListLength = value;
            }
        }

        /// <summary>
        /// 获取正在应用的资源包路径。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string ApplyingResourcePackPath
        {
            get
            {
                return m_ResourceManager.ApplyingResourcePackPath;
            }
        }

        /// <summary>
        /// 获取等待应用资源数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int ApplyWaitingCount
        {
            get
            {
                return m_ResourceManager.ApplyWaitingCount;
            }
        }

        /// <summary>
        /// 获取或设置资源更新重试次数。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int UpdateRetryCount
        {
            get
            {
                return m_ResourceManager.UpdateRetryCount;
            }
            set
            {
                m_ResourceManager.UpdateRetryCount = m_UpdateRetryCount = value;
            }
        }

        /// <summary>
        /// 获取正在更新的资源组。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroup UpdatingResourceGroup
        {
            get
            {
                return m_ResourceManager.UpdatingResourceGroup;
            }
        }

        /// <summary>
        /// 获取等待更新资源数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int UpdateWaitingCount
        {
            get
            {
                return m_ResourceManager.UpdateWaitingCount;
            }
        }

        /// <summary>
        /// 获取使用时下载的等待更新资源数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int UpdateWaitingWhilePlayingCount
        {
            get
            {
                return m_ResourceManager.UpdateWaitingWhilePlayingCount;
            }
        }

        /// <summary>
        /// 获取候选更新资源数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int UpdateCandidateCount
        {
            get
            {
                return m_ResourceManager.UpdateCandidateCount;
            }
        }

        /// <summary>
        /// 获取加载资源代理总数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadTotalAgentCount
        {
            get
            {
                return m_ResourceManager.LoadTotalAgentCount;
            }
        }

        /// <summary>
        /// 获取可用加载资源代理数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadFreeAgentCount
        {
            get
            {
                return m_ResourceManager.LoadFreeAgentCount;
            }
        }

        /// <summary>
        /// 获取工作中加载资源代理数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadWorkingAgentCount
        {
            get
            {
                return m_ResourceManager.LoadWorkingAgentCount;
            }
        }

        /// <summary>
        /// 获取等待加载资源任务数量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadWaitingTaskCount
        {
            get
            {
                return m_ResourceManager.LoadWaitingTaskCount;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float AssetAutoReleaseInterval
        {
            get
            {
                return m_ResourceManager.AssetAutoReleaseInterval;
            }
            set
            {
                m_ResourceManager.AssetAutoReleaseInterval = m_AssetAutoReleaseInterval = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的容量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int AssetCapacity
        {
            get
            {
                return m_ResourceManager.AssetCapacity;
            }
            set
            {
                m_ResourceManager.AssetCapacity = m_AssetCapacity = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池对象过期秒数。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float AssetExpireTime
        {
            get
            {
                return m_ResourceManager.AssetExpireTime;
            }
            set
            {
                m_ResourceManager.AssetExpireTime = m_AssetExpireTime = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的优先级。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int AssetPriority
        {
            get
            {
                return m_ResourceManager.AssetPriority;
            }
            set
            {
                m_ResourceManager.AssetPriority = m_AssetPriority = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float ResourceAutoReleaseInterval
        {
            get
            {
                return m_ResourceManager.ResourceAutoReleaseInterval;
            }
            set
            {
                m_ResourceManager.ResourceAutoReleaseInterval = m_ResourceAutoReleaseInterval = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的容量。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int ResourceCapacity
        {
            get
            {
                return m_ResourceManager.ResourceCapacity;
            }
            set
            {
                m_ResourceManager.ResourceCapacity = m_ResourceCapacity = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池对象过期秒数。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float ResourceExpireTime
        {
            get
            {
                return m_ResourceManager.ResourceExpireTime;
            }
            set
            {
                m_ResourceManager.ResourceExpireTime = m_ResourceExpireTime = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的优先级。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int ResourcePriority
        {
            get
            {
                return m_ResourceManager.ResourcePriority;
            }
            set
            {
                m_ResourceManager.ResourcePriority = m_ResourcePriority = value;
            }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_ResourceManager = GameFrameworkEntry.GetModule<IResourceManager>();
            if (m_Settings == null)
            {
                return;
            }

            string readOnlyPath = string.IsNullOrEmpty(m_ResourceManager.ReadOnlyPath) ? Application.streamingAssetsPath : m_ResourceManager.ReadOnlyPath;
            string readWritePath = string.IsNullOrEmpty(m_ResourceManager.ReadWritePath)
                ? m_Settings.GetCacheRoot()
                : m_ResourceManager.ReadWritePath;
            m_MinUnloadUnusedAssetsInterval = m_Settings.MinUnloadUnusedAssetsInterval;
            m_MaxUnloadUnusedAssetsInterval = m_Settings.MaxUnloadUnusedAssetsInterval;
            m_YooAssetBackend = new YooAssetResourceBackend(m_Settings, readOnlyPath, readWritePath);
            ((IResourceBackendHost)m_ResourceManager).SetResourceBackend(m_YooAssetBackend);
        }

        /// <summary>
        /// 连接资源事件并校验运行配置。
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

            m_ResourceManager = ResourceManager;
            if (m_ResourceManager == null)
            {
                Log.Fatal("Resource manager is invalid.");
                return;
            }

            if (m_Settings == null)
            {
                Log.Warning("Resource Settings is not assigned. Resource loading is unavailable until this component is configured.");
                return;
            }

            m_ResourceManager.ResourceVerifyStart += OnResourceVerifyStart;
            m_ResourceManager.ResourceVerifySuccess += OnResourceVerifySuccess;
            m_ResourceManager.ResourceVerifyFailure += OnResourceVerifyFailure;
            m_ResourceManager.ResourceApplyStart += OnResourceApplyStart;
            m_ResourceManager.ResourceApplySuccess += OnResourceApplySuccess;
            m_ResourceManager.ResourceApplyFailure += OnResourceApplyFailure;
            m_ResourceManager.ResourceUpdateStart += OnResourceUpdateStart;
            m_ResourceManager.ResourceUpdateChanged += OnResourceUpdateChanged;
            m_ResourceManager.ResourceUpdateSuccess += OnResourceUpdateSuccess;
            m_ResourceManager.ResourceUpdateFailure += OnResourceUpdateFailure;
            m_ResourceManager.ResourceUpdateAllComplete += OnResourceUpdateAllComplete;

            // 路径已在 Awake 按 ResourceSettings 初始化，Core 接口转发给同一个 YooAsset 后端。
        }

        /// <summary>
        /// 按卸载间隔清理未使用的资源。
        /// </summary>
        private void Update()
        {
            LastUnloadUnusedAssetsOperationElapseSeconds += Time.unscaledDeltaTime;
            if (m_AsyncOperation == null && (m_ForceUnloadUnusedAssets || LastUnloadUnusedAssetsOperationElapseSeconds >= m_MaxUnloadUnusedAssetsInterval || m_PreorderUnloadUnusedAssets && LastUnloadUnusedAssetsOperationElapseSeconds >= m_MinUnloadUnusedAssetsInterval))
            {
                Log.Info("Unload unused assets...");
                m_ForceUnloadUnusedAssets = false;
                m_PreorderUnloadUnusedAssets = false;
                LastUnloadUnusedAssetsOperationElapseSeconds = 0f;
                if (m_YooAssetBackend != null)
                {
                    m_YooAssetBackend.UnloadUnusedAssets();
                }
                m_AsyncOperation = Resources.UnloadUnusedAssets();
            }

            if (m_AsyncOperation != null && m_AsyncOperation.isDone)
            {
                m_AsyncOperation = null;
                if (m_PerformGCCollect)
                {
                    Log.Info("GC.Collect...");
                    m_PerformGCCollect = false;
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// 设置资源模式。
        /// </summary>
        /// <param name="resourceMode">资源模式。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetResourceMode(ResourceMode resourceMode)
        {
            m_ResourceManager.SetResourceMode(resourceMode);
            switch (resourceMode)
            {
                case ResourceMode.Package:
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V0);
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V1);
                    m_ResourceManager.PackageVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.PackageVersionListDeserializeCallback_V2);
                    break;

                case ResourceMode.Updatable:
                case ResourceMode.UpdatableWhilePlaying:
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V0);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V1);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.UpdatableVersionListDeserializeCallback_V2);

                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(0, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V0);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(1, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);
                    m_ResourceManager.UpdatableVersionListSerializer.RegisterTryGetValueCallback(2, BuiltinVersionListSerializer.UpdatableVersionListTryGetValueCallback_V1_V2);

                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    m_ResourceManager.ReadOnlyVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V0);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V1);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V2);

                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V0);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(1, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V1);
                    m_ResourceManager.ReadWriteVersionListSerializer.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);

                    m_ResourceManager.ResourcePackVersionListSerializer.RegisterDeserializeCallback(0, BuiltinVersionListSerializer.ResourcePackVersionListDeserializeCallback_V0);
                    break;
            }
        }

        /// <summary>
        /// 设置当前变体。
        /// </summary>
        /// <param name="currentVariant">当前变体。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetCurrentVariant(string currentVariant)
        {
            m_ResourceManager.SetCurrentVariant(!string.IsNullOrEmpty(currentVariant) ? currentVariant : null);
        }

        /// <summary>
        /// 设置解密资源回调函数。
        /// </summary>
        /// <param name="decryptResourceCallback">要设置的解密资源回调函数。</param>
        /// <remarks>旧 UGF 资源管线的扩展点。YooAsset 解密请在 IResourcePackageInitializer 中配置官方解密服务。</remarks>
        [Obsolete("Configure YooAsset decryption through IResourcePackageInitializer or the native-options overload of InitializePackageAsync.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            // 仅保留旧 Core 管线的调用约定，不参与 YooAsset 文件系统初始化。
            var resourceManager = m_ResourceManager ?? GameFrameworkEntry.GetModule<IResourceManager>();
            resourceManager.SetDecryptResourceCallback(decryptResourceCallback);
        }

        /// <summary>
        /// 预订执行释放未被使用的资源。
        /// </summary>
        /// <param name="performGCCollect">是否使用垃圾回收。</param>
        public void UnloadUnusedAssets(bool performGCCollect)
        {
            m_PreorderUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }

        /// <summary>
        /// 强制执行释放未被使用的资源。
        /// </summary>
        /// <param name="performGCCollect">是否使用垃圾回收。</param>
        public void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            m_ForceUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_PerformGCCollect = performGCCollect;
            }
        }

        /// <summary>
        /// 使用单机模式并初始化资源。
        /// </summary>
        /// <param name="initResourcesCompleteCallback">使用单机模式并初始化资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            m_ResourceManager.InitResources(initResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并检查版本资源列表。
        /// </summary>
        /// <param name="latestInternalResourceVersion">最新的内部资源版本号。</param>
        /// <returns>检查版本资源列表结果。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            return m_ResourceManager.CheckVersionList(latestInternalResourceVersion);
        }

        /// <summary>
        /// 使用可更新模式并更新版本资源列表。
        /// </summary>
        /// <param name="versionListLength">版本资源列表大小。</param>
        /// <param name="versionListHashCode">版本资源列表哈希值。</param>
        /// <param name="versionListCompressedLength">版本资源列表压缩后大小。</param>
        /// <param name="versionListCompressedHashCode">版本资源列表压缩后哈希值。</param>
        /// <param name="updateVersionListCallbacks">版本资源列表更新回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            m_ResourceManager.UpdateVersionList(versionListLength, versionListHashCode, versionListCompressedLength, versionListCompressedHashCode, updateVersionListCallbacks);
        }

        /// <summary>
        /// 使用可更新模式并校验资源。
        /// </summary>
        /// <param name="verifyResourcesCompleteCallback">使用可更新模式并校验资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyResources(VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            m_ResourceManager.VerifyResources(0, verifyResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并校验资源。
        /// </summary>
        /// <param name="verifyResourceLengthPerFrame">每帧至少校验资源的大小，以字节为单位。</param>
        /// <param name="verifyResourcesCompleteCallback">使用可更新模式并校验资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void VerifyResources(int verifyResourceLengthPerFrame, VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            m_ResourceManager.VerifyResources(verifyResourceLengthPerFrame, verifyResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并检查资源。
        /// </summary>
        /// <param name="checkResourcesCompleteCallback">使用可更新模式并检查资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void CheckResources(CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            m_ResourceManager.CheckResources(false, checkResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并检查资源。
        /// </summary>
        /// <param name="ignoreOtherVariant">是否忽略处理其它变体的资源，若不忽略，将会移除其它变体的资源。</param>
        /// <param name="checkResourcesCompleteCallback">使用可更新模式并检查资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void CheckResources(bool ignoreOtherVariant, CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            m_ResourceManager.CheckResources(ignoreOtherVariant, checkResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并应用资源包资源。
        /// </summary>
        /// <param name="resourcePackPath">要应用的资源包路径。</param>
        /// <param name="applyResourcesCompleteCallback">使用可更新模式并应用资源包资源完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void ApplyResources(string resourcePackPath, ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            m_ResourceManager.ApplyResources(resourcePackPath, applyResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并更新所有资源。
        /// </summary>
        /// <param name="updateResourcesCompleteCallback">使用可更新模式并更新默认资源组完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            m_ResourceManager.UpdateResources(updateResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并更新指定资源组的资源。
        /// </summary>
        /// <param name="resourceGroupName">要更新的资源组名称。</param>
        /// <param name="updateResourcesCompleteCallback">使用可更新模式并更新指定资源组完成时的回调函数。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void UpdateResources(string resourceGroupName, UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            m_ResourceManager.UpdateResources(resourceGroupName, updateResourcesCompleteCallback);
        }

        /// <summary>
        /// 停止更新资源。
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void StopUpdateResources()
        {
            m_ResourceManager.StopUpdateResources();
        }

        /// <summary>
        /// 校验资源包。
        /// </summary>
        /// <param name="resourcePackPath">要校验的资源包路径。</param>
        /// <returns>是否校验资源包成功。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool VerifyResourcePack(string resourcePackPath)
        {
            return m_ResourceManager.VerifyResourcePack(resourcePackPath);
        }

        /// <summary>
        /// 获取所有加载资源任务的信息。
        /// </summary>
        /// <returns>所有加载资源任务的信息。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public TaskInfo[] GetAllLoadAssetInfos()
        {
            return m_ResourceManager.GetAllLoadAssetInfos();
        }

        /// <summary>
        /// 获取所有加载资源任务的信息。
        /// </summary>
        /// <param name="results">所有加载资源任务的信息；填充前清空，容量不足时仍会扩容。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void GetAllLoadAssetInfosNonAlloc(List<TaskInfo> results)
        {
            m_ResourceManager.GetAllLoadAssetInfosNonAlloc(results);
        }

        /// <summary>
        /// 检查资源是否存在。
        /// </summary>
        /// <param name="assetName">要检查资源的名称。</param>
        /// <returns>检查资源是否存在的结果。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public HasAssetResult HasAsset(string assetName)
        {
            return m_ResourceManager.HasAsset(assetName);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, null, DefaultPriority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, assetType, DefaultPriority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, null, priority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, null, DefaultPriority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            LoadAsset(assetName, assetType, priority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, assetType, DefaultPriority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            LoadAsset(assetName, null, priority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error("Asset name is invalid.");
                return;
            }

            if (m_YooAssetBackend == null && !assetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Asset name '{0}' is invalid.", assetName);
                return;
            }

            m_ResourceManager.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 卸载资源。
        /// </summary>
        /// <param name="asset">要卸载的资源。</param>
        public void UnloadAsset(object asset)
        {
            m_ResourceManager.UnloadAsset(asset);
        }

        /// <summary>
        /// 获取二进制资源的实际路径。
        /// </summary>
        /// <param name="binaryAssetName">要获取实际路径的二进制资源的名称。</param>
        /// <returns>二进制资源的实际路径。</returns>
        /// <remarks>此方法仅适用于二进制资源存储在磁盘（而非文件系统）中的情况。若二进制资源存储在文件系统中时，返回值将始终为空。</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string GetBinaryPath(string binaryAssetName)
        {
            return m_ResourceManager.GetBinaryPath(binaryAssetName);
        }

        /// <summary>
        /// 获取二进制资源的实际路径。
        /// </summary>
        /// <param name="binaryAssetName">要获取实际路径的二进制资源的名称。</param>
        /// <param name="storageInReadOnly">二进制资源是否存储在只读区中。</param>
        /// <param name="storageInFileSystem">二进制资源是否存储在文件系统中。</param>
        /// <param name="relativePath">二进制资源或存储二进制资源的文件系统，相对于只读区或者读写区的相对路径。</param>
        /// <param name="fileName">若二进制资源存储在文件系统中，则指示二进制资源在文件系统中的名称，否则此参数返回空。</param>
        /// <returns>是否获取二进制资源的实际路径成功。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem, out string relativePath, out string fileName)
        {
            return m_ResourceManager.GetBinaryPath(binaryAssetName, out storageInReadOnly, out storageInFileSystem, out relativePath, out fileName);
        }

        /// <summary>
        /// 获取二进制资源的长度。
        /// </summary>
        /// <param name="binaryAssetName">要获取长度的二进制资源的名称。</param>
        /// <returns>二进制资源的长度。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetBinaryLength(string binaryAssetName)
        {
            return m_ResourceManager.GetBinaryLength(binaryAssetName);
        }

        /// <summary>
        /// 异步加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="loadBinaryCallbacks">加载二进制资源回调函数集。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks)
        {
            LoadBinary(binaryAssetName, loadBinaryCallbacks, null);
        }

        /// <summary>
        /// 异步加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="loadBinaryCallbacks">加载二进制资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return;
            }

            if (m_YooAssetBackend == null && !binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return;
            }

            m_ResourceManager.LoadBinary(binaryAssetName, loadBinaryCallbacks, userData);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <returns>存储加载二进制资源的二进制流。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] LoadBinaryFromFileSystem(string binaryAssetName)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return null;
            }

            if (m_YooAssetBackend == null && !binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return null;
            }

            return m_ResourceManager.LoadBinaryFromFileSystem(binaryAssetName);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinaryFromFileSystem(binaryAssetName, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源的二进制流的起始位置。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, buffer.Length - startIndex);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源的二进制流的起始位置。</param>
        /// <param name="length">存储加载二进制资源的二进制流的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return 0;
            }

            if (m_YooAssetBackend == null && !binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return 0;
            }

            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return m_ResourceManager.LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>存储加载二进制资源片段内容的二进制流。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>存储加载二进制资源片段内容的二进制流。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return null;
            }

            if (m_YooAssetBackend == null && !binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return null;
            }

            return m_ResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源片段内容的二进制流的起始位置。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, startIndex, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer)
        {
            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int length)
        {
            return LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源片段内容的二进制流的起始位置。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                Log.Error("Binary asset name is invalid.");
                return 0;
            }

            if (m_YooAssetBackend == null && !binaryAssetName.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Log.Error("Binary asset name '{0}' is invalid.", binaryAssetName);
                return 0;
            }

            if (buffer == null)
            {
                Log.Error("Buffer is invalid.");
                return 0;
            }

            return m_ResourceManager.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, startIndex, length);
        }

        /// <summary>
        /// 检查资源组是否存在。
        /// </summary>
        /// <param name="resourceGroupName">要检查资源组的名称。</param>
        /// <returns>资源组是否存在。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool HasResourceGroup(string resourceGroupName)
        {
            return m_ResourceManager.HasResourceGroup(resourceGroupName);
        }

        /// <summary>
        /// 获取默认资源组。
        /// </summary>
        /// <returns>默认资源组。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroup GetResourceGroup()
        {
            return m_ResourceManager.GetResourceGroup();
        }

        /// <summary>
        /// 获取资源组。
        /// </summary>
        /// <param name="resourceGroupName">要获取的资源组名称。</param>
        /// <returns>要获取的资源组。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            return m_ResourceManager.GetResourceGroup(resourceGroupName);
        }

        /// <summary>
        /// 获取所有资源组。
        /// </summary>
        /// <returns>所有资源组。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroup[] GetAllResourceGroups()
        {
            return m_ResourceManager.GetAllResourceGroups();
        }

        /// <summary>
        /// 获取所有资源组。
        /// </summary>
        /// <param name="results">所有资源组；填充前清空，容量不足时仍会扩容。</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void GetAllResourceGroupsNonAlloc(List<IResourceGroup> results)
        {
            m_ResourceManager.GetAllResourceGroupsNonAlloc(results);
        }

        /// <summary>
        /// 获取资源组集合。
        /// </summary>
        /// <param name="resourceGroupNames">要获取的资源组名称的集合。</param>
        /// <returns>要获取的资源组集合。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            return m_ResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        /// <summary>
        /// 获取资源组集合。
        /// </summary>
        /// <param name="resourceGroupNames">要获取的资源组名称的集合。</param>
        /// <returns>要获取的资源组集合。</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            return m_ResourceManager.GetResourceGroupCollection(resourceGroupNames);
        }

        /// <summary>
        /// 派发资源校验开始事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceVerifyStart(object sender, UnityQGameFramework.Core.Events.ResourceVerifyStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifyStartEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源校验成功事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceVerifySuccess(object sender, UnityQGameFramework.Core.Events.ResourceVerifySuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifySuccessEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源校验失败事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceVerifyFailure(object sender, UnityQGameFramework.Core.Events.ResourceVerifyFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceVerifyFailureEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源包应用开始事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceApplyStart(object sender, UnityQGameFramework.Core.Events.ResourceApplyStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplyStartEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源应用成功事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceApplySuccess(object sender, UnityQGameFramework.Core.Events.ResourceApplySuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplySuccessEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源应用失败事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceApplyFailure(object sender, UnityQGameFramework.Core.Events.ResourceApplyFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceApplyFailureEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源下载开始事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceUpdateStart(object sender, UnityQGameFramework.Core.Events.ResourceUpdateStartEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateStartEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源下载进度事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceUpdateChanged(object sender, UnityQGameFramework.Core.Events.ResourceUpdateChangedEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateChangedEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源下载成功事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceUpdateSuccess(object sender, UnityQGameFramework.Core.Events.ResourceUpdateSuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateSuccessEventArgs.Create(e));
        }

        /// <summary>
        /// 派发资源下载失败事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceUpdateFailure(object sender, UnityQGameFramework.Core.Events.ResourceUpdateFailureEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateFailureEventArgs.Create(e));
        }

        /// <summary>
        /// 卸载闲置资源并派发全部更新完成事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnResourceUpdateAllComplete(object sender, UnityQGameFramework.Core.Events.ResourceUpdateAllCompleteEventArgs e)
        {
            m_EventComponent.Fire(this, ResourceUpdateAllCompleteEventArgs.Create(e));
        }
    }
}
