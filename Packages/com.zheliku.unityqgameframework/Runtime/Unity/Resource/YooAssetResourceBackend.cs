using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// YooAsset 资源后端。
    /// </summary>
    internal sealed partial class YooAssetResourceBackend : IResourceBackend
    {
        private static readonly Dictionary<string, YooAssetResourceBackend> s_ShuttingDownPackages = new Dictionary<string, YooAssetResourceBackend>(StringComparer.Ordinal);
        private readonly ResourceSettings m_Settings;
        private readonly Dictionary<string, PackageInfo> m_Packages = new Dictionary<string, PackageInfo>(StringComparer.Ordinal);
        private readonly CancellationTokenSource m_ShutdownCancellation = new CancellationTokenSource();
        private readonly CancellationToken m_ShutdownToken;
        private readonly UniTaskCompletionSource m_ShutdownCompletion = new UniTaskCompletionSource();
        private bool m_Shutdown;
        private int m_LoadingCount;

        internal YooAssetResourceBackend(ResourceSettings settings, string readOnlyPath, string readWritePath)
        {
            if (settings == null)
            {
                throw new FrameworkException("Resource settings is invalid.");
            }

            settings.GetPackage();
            m_Settings = settings;
            m_ShutdownToken = m_ShutdownCancellation.Token;
            SetPaths(readOnlyPath, readWritePath);
        }

        /// <summary>
        /// 获取资源只读路径。
        /// </summary>
        public string ReadOnlyPath { get; private set; }

        /// <summary>
        /// 获取资源读写路径。
        /// </summary>
        public string ReadWritePath { get; private set; }

        /// <summary>
        /// 获取默认资源包的运行模式。
        /// </summary>
        public ResourceMode ResourceMode
        {
            get
            {
                ResourcePlayMode mode = GetPlayMode(m_Settings.GetPackage());
                return mode == ResourcePlayMode.Host || mode == ResourcePlayMode.Web ? ResourceMode.Updatable : ResourceMode.Package;
            }
        }

        /// <summary>
        /// 获取已加载清单中的资产总数。
        /// </summary>
        public int AssetCount
        {
            get
            {
                int count = 0;
                foreach (PackageInfo package in m_Packages.Values)
                {
                    if (package.Ready)
                    {
                        count += package.Package.GetAllAssetInfos().Length;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 获取正在加载的请求数量。
        /// </summary>
        public int LoadWorkingAgentCount
        {
            get
            {
                return m_LoadingCount;
            }
        }

        /// <summary>
        /// 设置资源目录。
        /// </summary>
        /// <param name="readOnlyPath">资源只读目录。</param>
        /// <param name="readWritePath">资源读写目录。</param>
        public void SetPaths(string readOnlyPath, string readWritePath)
        {
            ThrowIfShutdown();
            if (m_Packages.Count > 0)
            {
                throw new FrameworkException("Set resource paths before creating packages.");
            }
            if (string.IsNullOrEmpty(readOnlyPath) || string.IsNullOrEmpty(readWritePath))
            {
                throw new FrameworkException("Resource paths are invalid.");
            }
            ReadOnlyPath = readOnlyPath;
            ReadWritePath = readWritePath;
        }

        /// <summary>
        /// 初始化默认资源包。
        /// </summary>
        /// <param name="callback">初始化完成回调。</param>
        public void Initialize(InitResourcesCompleteCallback callback)
        {
            if (callback == null)
            {
                throw new FrameworkException("Init resources complete callback is invalid.");
            }
            InitializeCallbackAsync(callback).Forget();
        }

        internal UniTask<ResourcePackage> InitializePackageAsync(string packageName, CancellationToken cancellationToken)
        {
            return InitializePackageAsync(packageName, null, null, cancellationToken);
        }

        internal async UniTask<ResourcePackage> InitializePackageAsync(string packageName, InitializePackageOptions options, string version, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (s_ShuttingDownPackages.TryGetValue(name, out YooAssetResourceBackend previous))
            {
                // 重载启动场景时，等待上一实例完成异步销毁，再创建同名包。
                await ResourceOperation.WithCancellation(previous.m_ShutdownCompletion.Task, cancellationToken);
                ThrowIfShutdown();
            }
            PackageInfo package = CreatePackage(name);
            if (package.Destroying)
            {
                throw new FrameworkException("Resource package is being destroyed: " + package.Package.PackageName);
            }
            if (options != null)
            {
                if (package.Initializing || package.Ready || package.Package.InitializeStatus == EOperationStatus.Succeeded)
                {
                    if (!ReferenceEquals(package.Options, options) || package.Version != version)
                    {
                        throw new FrameworkException("The resource package is already initialized with other options: " + package.Package.PackageName);
                    }
                }
                else
                {
                    package.Options = options;
                    package.Version = version;
                }
            }
            if (!package.Ready)
            {
                if (!package.Initializing)
                {
                    package.Initializing = true;
                    // 初始化属于包，调用者取消等待不会中止其他请求共用的初始化。
                    package.Initialization = new UniTaskCompletionSource();
                    InitializePackageAsync(package, package.Initialization).Forget();
                }
                await ResourceOperation.WithCancellation(package.Initialization.Task, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            return package.Package;
        }

        internal async UniTask DestroyPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package))
            {
                throw new FrameworkException("Resource package was not created: " + name);
            }
            RemoveReleasedHandles(package);
            if (package.Destroying || package.Initializing || package.Requests > 0 || package.Assets.Count > 0 || package.Scenes.Count > 0 || package.Operations.Count > 0)
            {
                throw new FrameworkException("Resource package still has active operations or resource handles: " + name);
            }

            package.Destroying = true;
            try
            {
                // 销毁发出后必须完成注销，不能因取消等待留下半销毁的包。
                await ResourceOperation.WaitAsync(package.Package.DestroyPackageAsync(), CancellationToken.None);
                YooAssets.RemovePackage(name);
                m_Packages.Remove(name);
            }
            finally
            {
                package.Destroying = false;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        internal ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options, string packageName)
        {
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                throw new FrameworkException("Initialize the resource package before creating a downloader: " + name);
            }
            ResourceDownloaderOperation operation = package.Package.CreateResourceDownloader(options);
            package.Operations.Add(operation);
            return operation;
        }

        internal async UniTask DownloadResourcesAsync(ResourceDownloaderOperation downloader, CancellationToken cancellationToken)
        {
            if (downloader == null)
            {
                throw new ArgumentNullException(nameof(downloader));
            }
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();

            PackageInfo owner = null;
            foreach (PackageInfo package in m_Packages.Values)
            {
                if (package.Operations.Contains(downloader))
                {
                    owner = package;
                    break;
                }
            }
            if (owner == null || owner.Destroying)
            {
                throw new FrameworkException("The downloader does not belong to an active resource package.");
            }

            owner.Requests++;
            m_LoadingCount++;
            try
            {
                using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_ShutdownToken))
                {
                    downloader.StartDownload();
                    await ResourceOperation.WaitAsync(downloader, cancellation.Token);
                }
            }
            catch
            {
                if (!downloader.IsDone)
                {
                    downloader.CancelDownload();
                }
                throw;
            }
            finally
            {
                owner.Requests--;
                m_LoadingCount--;
            }
        }

        internal async UniTask ClearCacheAsync(string packageName, string clearMethod, object clearParameter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                throw new FrameworkException("Initialize the resource package before clearing its cache: " + name);
            }
            RemoveReleasedHandles(package);
            if (package.Assets.Count > 0 || package.Scenes.Count > 0 || package.Requests > 0 || package.Operations.Count > 0)
            {
                throw new FrameworkException("Finish resource operations and return all resource borrows before clearing the cache: " + name);
            }
            ClearCacheOperation operation = package.Package.ClearCacheAsync(new ClearCacheOptions(clearMethod, clearParameter));
            package.Operations.Add(operation);
            await ResourceOperation.WaitAsync(operation, cancellationToken);
        }

        /// <summary>
        /// 释放未使用的资产。
        /// </summary>
        public void UnloadUnusedAssets()
        {
            UnloadUnusedAssetsAsync(m_ShutdownToken).Forget();
        }

        internal async UniTask UnloadUnusedAssetsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            var operations = new List<UnloadUnusedAssetsOperation>();
            foreach (PackageInfo package in m_Packages.Values)
            {
                if (package.Ready && !package.Destroying)
                {
                    UnloadUnusedAssetsOperation operation = package.Package.UnloadUnusedAssetsAsync();
                    package.Operations.Add(operation);
                    operations.Add(operation);
                }
            }
            foreach (UnloadUnusedAssetsOperation operation in operations)
            {
                await ResourceOperation.WaitAsync(operation, cancellationToken);
            }
        }

        /// <summary>
        /// 清理已归还句柄的登记。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间。</param>
        /// <param name="realElapseSeconds">真实流逝时间。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (PackageInfo package in m_Packages.Values)
            {
                RemoveReleasedHandles(package);
            }
        }

        /// <summary>
        /// 关闭资源后端。
        /// </summary>
        public void Shutdown()
        {
            if (m_Shutdown)
            {
                return;
            }
            m_Shutdown = true;
            var packageNames = new List<string>(m_Packages.Keys);
            foreach (string packageName in packageNames)
            {
                s_ShuttingDownPackages.Add(packageName, this);
            }
            m_ShutdownCancellation.Cancel();
            CancelPendingDownloaders();
            ShutdownAsync(packageNames).Forget();
        }

        private async UniTask InitializeCallbackAsync(InitResourcesCompleteCallback callback)
        {
            await InitializePackageAsync(null, null, null, m_ShutdownToken);
            callback();
        }

        private async UniTask InitializePackageAsync(PackageInfo package, UniTaskCompletionSource completion)
        {
            Exception error = null;
            try
            {
                if (package.Package.InitializeStatus != EOperationStatus.Succeeded)
                {
                    InitializePackageOptions options = package.Options ?? CreateOptions(package.Settings);
                    await ResourceOperation.WaitAsync(package.Package.InitializePackageAsync(options), m_ShutdownToken);
                }
                string version = package.Version ?? package.Settings.PackageVersion;
                if (string.IsNullOrEmpty(version))
                {
                    RequestPackageVersionOperation request = package.Package.RequestPackageVersionAsync();
                    await ResourceOperation.WaitAsync(request, m_ShutdownToken);
                    version = request.PackageVersion;
                }
                await ResourceOperation.WaitAsync(package.Package.LoadPackageManifestAsync(new LoadPackageManifestOptions(version, 60)), m_ShutdownToken);
                package.Ready = true;
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finally
            {
                package.Initializing = false;
            }

            if (error is OperationCanceledException)
            {
                completion.TrySetCanceled(m_ShutdownToken);
            }
            else if (error != null)
            {
                completion.TrySetException(error);
            }
            else
            {
                completion.TrySetResult();
            }
        }

        private PackageInfo CreatePackage(string packageName)
        {
            string name = GetPackageName(packageName);
            if (m_Packages.TryGetValue(name, out PackageInfo package))
            {
                return package;
            }
            ResourcePackageSettings settings = m_Settings.GetPackage(name);
            if (!YooAssets.IsInitialized)
            {
                YooAssets.Initialize(new ResourceLogger());
            }
            if (YooAssets.TryGetPackage(name, out _))
            {
                throw new FrameworkException("Resource package is owned by another resource service: " + name);
            }
            package = new PackageInfo(settings, YooAssets.CreatePackage(name));
            m_Packages.Add(name, package);
            return package;
        }

        private InitializePackageOptions CreateOptions(ResourcePackageSettings settings)
        {
            string builtinRoot = ReadOnlyPath.TrimEnd('/', '\\') + "/yoo/" + settings.Name;
            string cacheRoot = Path.Combine(ReadWritePath, "QGF", "Resources", settings.Name);
            switch (GetPlayMode(settings))
            {
                case ResourcePlayMode.EditorSimulate:
                    if (!Application.isEditor || ResourceComponent.EditorSimulationBuilder == null)
                    {
                        throw new FrameworkException("Editor simulation is only available in the Unity Editor.");
                    }
                    string root = ResourceComponent.EditorSimulationBuilder(m_Settings, settings.Name);
                    return new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(root)
                    };

                case ResourcePlayMode.Offline:
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(builtinRoot)
                    };

                case ResourcePlayMode.Host:
                    return new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters = settings.UseBuiltinFiles
                            ? FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(builtinRoot)
                            : null,
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(new RemoteService(settings), cacheRoot)
                    };

                case ResourcePlayMode.Web:
                    return new WebPlayModeOptions
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                        WebNetworkFileSystemParameters = FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(new RemoteService(settings))
                    };

                default:
                    throw new FrameworkException("Resource play mode is invalid.");
            }
        }

        private ResourcePlayMode GetPlayMode(ResourcePackageSettings settings)
        {
            return Application.isEditor && m_Settings.UseEditorSimulation ? ResourcePlayMode.EditorSimulate : settings.PlayMode;
        }

        private string GetPackageName(string packageName)
        {
            return string.IsNullOrEmpty(packageName) ? m_Settings.DefaultPackageName : packageName;
        }

        private void ThrowIfShutdown()
        {
            if (m_Shutdown)
            {
                throw new OperationCanceledException("The resource service has shut down.", m_ShutdownToken);
            }
        }

        private static void RemoveReleasedHandles(PackageInfo package)
        {
            for (int i = package.Assets.Count - 1; i >= 0; i--)
            {
                if (!package.Assets[i].Handle.IsValid)
                {
                    package.Assets.RemoveAt(i);
                }
            }
            for (int i = package.Operations.Count - 1; i >= 0; i--)
            {
                if (package.Operations[i].IsDone)
                {
                    package.Operations.RemoveAt(i);
                }
            }
        }

        private async UniTask ShutdownAsync(List<string> packageNames)
        {
            Exception error = null;
            try
            {
                // 先等待在途请求交回句柄，再销毁本后端创建的包。
                while (m_LoadingCount > 0 || HasInitializingPackage() || HasPendingOperations())
                {
                    await UniTask.NextFrame();
                }
                foreach (PackageInfo package in m_Packages.Values)
                {
                    if (YooAssets.IsInitialized)
                    {
                        await ResourceOperation.WaitAsync(package.Package.DestroyPackageAsync(), CancellationToken.None);
                        YooAssets.RemovePackage(package.Package.PackageName);
                    }
                }
            }
            catch (Exception exception)
            {
                error = exception;
                throw;
            }
            finally
            {
                foreach (string packageName in packageNames)
                {
                    if (s_ShuttingDownPackages.TryGetValue(packageName, out YooAssetResourceBackend owner) && ReferenceEquals(owner, this))
                    {
                        s_ShuttingDownPackages.Remove(packageName);
                    }
                }
                m_Packages.Clear();
                m_ShutdownCancellation.Dispose();
                if (error == null)
                {
                    m_ShutdownCompletion.TrySetResult();
                }
                else
                {
                    m_ShutdownCompletion.TrySetException(error);
                }
            }
        }

        private bool HasInitializingPackage()
        {
            foreach (PackageInfo package in m_Packages.Values)
            {
                if (package.Initializing || package.Destroying || package.Package.InitializeStatus == EOperationStatus.Processing)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasPendingOperations()
        {
            foreach (PackageInfo package in m_Packages.Values)
            {
                foreach (AsyncOperationBase operation in package.Operations)
                {
                    if (!operation.IsDone)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CancelPendingDownloaders()
        {
            foreach (PackageInfo package in m_Packages.Values)
            {
                foreach (AsyncOperationBase operation in package.Operations)
                {
                    ResourceDownloaderOperation downloader = operation as ResourceDownloaderOperation;
                    if (downloader != null && !downloader.IsDone)
                    {
                        downloader.CancelDownload();
                    }
                }
            }
        }

        /// <summary>
        /// 资源包及其在途请求和句柄。
        /// </summary>
        private sealed class PackageInfo
        {
            internal readonly ResourcePackageSettings Settings;
            internal readonly ResourcePackage Package;
            internal readonly List<AssetBorrow> Assets = new List<AssetBorrow>();
            internal readonly List<AsyncOperationBase> Operations = new List<AsyncOperationBase>();
            internal readonly Dictionary<string, SceneInfo> Scenes = new Dictionary<string, SceneInfo>(StringComparer.Ordinal);
            internal UniTaskCompletionSource Initialization;
            internal InitializePackageOptions Options;
            internal string Version;
            internal bool Initializing;
            internal bool Ready;
            internal bool Destroying;
            internal int Requests;

            internal PackageInfo(ResourcePackageSettings settings, ResourcePackage package)
            {
                Settings = settings;
                Package = package;
            }
        }

        /// <summary>
        /// 远端资源地址查询服务。
        /// </summary>
        private sealed class RemoteService : IRemoteService
        {
            private readonly ResourcePackageSettings m_Settings;

            internal RemoteService(ResourcePackageSettings settings)
            {
                if (string.IsNullOrWhiteSpace(settings.HostServer))
                {
                    throw new FrameworkException("Resource host server is not configured: " + settings.Name);
                }
                m_Settings = settings;
            }

            /// <summary>
            /// 获取远端文件地址。
            /// </summary>
            /// <param name="fileName">文件名称。</param>
            /// <returns>主服务器与备用服务器的文件地址。</returns>
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                string host = m_Settings.HostServer.TrimEnd('/') + "/" + fileName;
                string fallback = m_Settings.FallbackServer.TrimEnd('/') + "/" + fileName;
                return host == fallback ? new[] { host } : new[] { host, fallback };
            }
        }

        /// <summary>
        /// 将 YooAsset 日志转交给 QGF。
        /// </summary>
        private sealed class ResourceLogger : YooAsset.ILogger
        {
            /// <summary>
            /// 输出普通日志。
            /// </summary>
            /// <param name="message">日志内容。</param>
            public void Log(string message)
            {
                Runtime.Log.Info("{0}", message);
            }

            /// <summary>
            /// 输出警告日志。
            /// </summary>
            /// <param name="message">日志内容。</param>
            public void LogWarning(string message)
            {
                Runtime.Log.Warning("{0}", message);
            }

            /// <summary>
            /// 输出错误日志。
            /// </summary>
            /// <param name="message">日志内容。</param>
            public void LogError(string message)
            {
                Runtime.Log.Error("{0}", message);
            }

            /// <summary>
            /// 输出异常日志。
            /// </summary>
            /// <param name="exception">异常。</param>
            public void LogException(Exception exception)
            {
                Runtime.Log.Error("{0}", exception);
            }
        }
    }
}
