using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// YooAsset 资源后端。
    /// </summary>
    internal sealed partial class YooAssetResourceBackend : IResourceBackend
    {
        private static readonly Dictionary<string, YooAssetResourceBackend> s_ShuttingDownPackages = new Dictionary<string, YooAssetResourceBackend>(StringComparer.Ordinal);
        private static int s_DestroyingPackageCount;
        private readonly ResourceSettings m_Settings;
        private readonly Dictionary<string, PackageInfo> m_Packages = new Dictionary<string, PackageInfo>(StringComparer.Ordinal);
        private readonly CancellationTokenSource m_ShutdownCancellation = new CancellationTokenSource();
        private readonly CancellationToken m_ShutdownToken;
        private readonly UniTaskCompletionSource m_ShutdownCompletion = new UniTaskCompletionSource();
        private bool m_Shutdown;
        private int m_LoadingCount;

        /// <summary>
        /// 创建使用指定配置和目录的资源后端。
        /// </summary>
        /// <param name="settings">共享的资源运行配置。</param>
        /// <param name="readOnlyPath">StreamingAssets 根目录。</param>
        /// <param name="readWritePath">下载缓存根目录。</param>
        internal YooAssetResourceBackend(ResourceSettings settings, string readOnlyPath, string readWritePath)
        {
            if (settings == null)
            {
                throw new GameFrameworkException("Resource settings is invalid.");
            }

            settings.GetPackage();
            m_Settings = settings;
            m_ShutdownToken = m_ShutdownCancellation.Token;
            SetPaths(readOnlyPath, readWritePath);
            Application.quitting += Shutdown;
        }

        /// <summary>
        /// 获取资源只读路径。
        /// </summary>
        public string ReadOnlyPath
        {
            get; private set;
        }

        /// <summary>
        /// 获取资源读写路径。
        /// </summary>
        public string ReadWritePath
        {
            get; private set;
        }

        /// <summary>
        /// 获取默认资源包的运行模式。
        /// </summary>
        public ResourceMode ResourceMode
        {
            get
            {
                EPlayMode mode = m_Settings.GetPackage().PlayMode;
                return mode == EPlayMode.HostPlayMode || mode == EPlayMode.WebPlayMode ? ResourceMode.Updatable : ResourceMode.Package;
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
                throw new GameFrameworkException("Set resource paths before creating packages.");
            }
            if (string.IsNullOrEmpty(readOnlyPath) || string.IsNullOrEmpty(readWritePath))
            {
                throw new GameFrameworkException("Resource paths are invalid.");
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
                throw new GameFrameworkException("Init resources complete callback is invalid.");
            }
            InitializeCallbackAsync(callback).Forget();
        }

        /// <summary>
        /// 按配置初始化资源包并加载清单。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="cancellationToken">仅取消本次等待的标记。</param>
        /// <returns>已准备好加载资产的资源包。</returns>
        internal UniTask<ResourcePackage> InitializePackageAsync(string packageName, CancellationToken cancellationToken)
        {
            return InitializePackageAsync(packageName, null, null, cancellationToken);
        }

        /// <summary>
        /// 合并同一包的初始化请求，防止重复创建文件系统。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="options">显式的官方选项，为空时按配置创建。</param>
        /// <param name="version">指定清单版本，为空时使用配置或文件系统当前版本。</param>
        /// <param name="cancellationToken">仅取消本次等待的标记。</param>
        /// <returns>已准备好加载资产的资源包。</returns>
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
                throw new GameFrameworkException("Resource package is being destroyed: " + package.Package.PackageName);
            }
            if (options != null)
            {
                if (package.Initializing || package.Ready || package.Package.InitializeStatus == EOperationStatus.Succeeded)
                {
                    if (!ReferenceEquals(package.Options, options) || package.Version != version)
                    {
                        throw new GameFrameworkException("The resource package is already initialized with other options: " + package.Package.PackageName);
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

        /// <summary>
        /// 在没有活跃借用和请求时销毁资源包。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="cancellationToken">取消等待的标记，销毁开始后仍完成注销。</param>
        /// <returns>资源包销毁任务。</returns>
        internal async UniTask DestroyPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package))
            {
                throw new GameFrameworkException("Resource package was not created: " + name);
            }
            RemoveReleasedHandles(package);
            if (package.Destroying || package.Initializing || package.Requests > 0 || package.Assets.Count > 0 || package.Scenes.Count > 0 || package.Operations.Count > 0)
            {
                throw new GameFrameworkException("Resource package still has active operations or resource handles: " + name);
            }

            package.Destroying = true;
            s_DestroyingPackageCount++;
            try
            {
                // 销毁发出后必须完成注销，不能因取消等待留下半销毁的包。
                await ResourceOperation.WaitAsync(package.Package.DestroyPackageAsync(), CancellationToken.None);
                YooAssets.RemovePackage(name);
                m_Packages.Remove(name);
            }
            finally
            {
                s_DestroyingPackageCount--;
                package.Destroying = false;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// 为已初始化的资源包创建下载器。
        /// </summary>
        /// <param name="options">YooAsset 下载器选项。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <returns>已登记的 YooAsset 下载器。</returns>
        internal ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options, string packageName)
        {
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                throw new GameFrameworkException("Initialize the resource package before creating a downloader: " + name);
            }
            ResourceDownloaderOperation operation = package.Package.CreateResourceDownloader(options);
            package.Operations.Add(operation);
            return operation;
        }

        /// <summary>
        /// 启动已登记的下载器并等待完成，取消时停止下载。
        /// </summary>
        /// <param name="downloader">由当前资源后端创建的下载器。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>等待下载完成的异步任务。</returns>
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
                throw new GameFrameworkException("The downloader does not belong to an active resource package.");
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

        /// <summary>
        /// 检查资源借用和在途操作后清理资源包缓存。
        /// </summary>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="clearMethod">YooAsset 缓存清理方式。</param>
        /// <param name="clearParameter">缓存清理方式所需的参数。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>等待缓存清理完成的异步任务。</returns>
        internal async UniTask ClearCacheAsync(string packageName, string clearMethod, object clearParameter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            string name = GetPackageName(packageName);
            if (!m_Packages.TryGetValue(name, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                throw new GameFrameworkException("Initialize the resource package before clearing its cache: " + name);
            }
            RemoveReleasedHandles(package);
            if (package.Assets.Count > 0 || package.Scenes.Count > 0 || package.Requests > 0 || package.Operations.Count > 0)
            {
                throw new GameFrameworkException("Finish resource operations and return all resource borrows before clearing the cache: " + name);
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

        /// <summary>
        /// 请求卸载所有已就绪资源包中的闲置资产。
        /// </summary>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>等待闲置资产卸载完成的异步任务。</returns>
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
            Application.quitting -= Shutdown;
            var packageNames = new List<string>(m_Packages.Keys);
            foreach (string packageName in packageNames)
            {
                s_ShuttingDownPackages.Add(packageName, this);
            }
            m_ShutdownCancellation.Cancel();
            CancelPendingDownloaders();
            ShutdownAsync(packageNames).Forget();
        }

        /// <summary>
        /// 初始化默认资源包后执行完成回调。
        /// </summary>
        /// <param name="callback">默认资源包初始化完成回调。</param>
        /// <returns>等待初始化和回调完成的异步任务。</returns>
        private async UniTask InitializeCallbackAsync(InitResourcesCompleteCallback callback)
        {
            await InitializePackageAsync(null, null, null, m_ShutdownToken);
            callback();
        }

        /// <summary>
        /// 初始化文件系统、读取版本并加载清单，再发布完成状态。
        /// </summary>
        /// <param name="package">当前包的状态与参数。</param>
        /// <param name="completion">同一包所有等待者共用的完成源。</param>
        /// <returns>初始化任务。</returns>
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

        /// <summary>
        /// 获取资源包状态，不存在时创建并登记所有权。
        /// </summary>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <returns>当前后端持有的资源包状态。</returns>
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
                throw new GameFrameworkException("Resource package is owned by another resource service: " + name);
            }
            package = new PackageInfo(settings, YooAssets.CreatePackage(name));
            m_Packages.Add(name, package);
            return package;
        }

        /// <summary>
        /// 按资源包配置的运行模式创建 YooAsset 原生初始化选项。
        /// </summary>
        /// <param name="settings">当前资源包配置。</param>
        /// <returns>默认选项或自定义初始化扩展返回的选项。</returns>
        private InitializePackageOptions CreateOptions(ResourcePackageSettings settings)
        {
            string builtinRoot = ReadOnlyPath.TrimEnd('/', '\\') + "/yoo/" + settings.Name;
            string cacheRoot = Path.Combine(ReadWritePath, "QGF", "Resources", settings.Name);
            switch (settings.PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    if (!Application.isEditor || ResourceComponent.EditorPackageBuilder == null)
                    {
                        throw new GameFrameworkException("Editor simulation is only available in the Unity Editor.");
                    }
                    string root = ResourceComponent.EditorPackageBuilder(m_Settings, settings.Name, true);
                    return new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(root)
                    };

                case EPlayMode.OfflinePlayMode:
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(builtinRoot)
                    };

                case EPlayMode.HostPlayMode:
                    return new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters = settings.UseBuiltinFiles
                            ? FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(builtinRoot)
                            : null,
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(new RemoteService(settings), cacheRoot)
                    };

                case EPlayMode.WebPlayMode:
                    return new WebPlayModeOptions
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                        WebNetworkFileSystemParameters = FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(new RemoteService(settings))
                    };

                case EPlayMode.CustomPlayMode:
                    System.Type type = Utility.Assembly.GetType(settings.InitializerTypeName);
                    if (type == null || type.IsAbstract || !typeof(IResourcePackageInitializer).IsAssignableFrom(type))
                    {
                        throw new GameFrameworkException("The custom initializer must implement IResourcePackageInitializer: " + settings.InitializerTypeName);
                    }
                    var initializer = (IResourcePackageInitializer)Activator.CreateInstance(type);
                    return initializer.CreateOptions(settings, builtinRoot, cacheRoot)
                        ?? throw new GameFrameworkException("The custom initializer returned null options: " + settings.Name);

                default:
                    throw new GameFrameworkException("Resource play mode is invalid.");
            }
        }

        /// <summary>
        /// 将空资源包名称解析为默认包名称。
        /// </summary>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <returns>实际使用的资源包名称。</returns>
        private string GetPackageName(string packageName)
        {
            return string.IsNullOrEmpty(packageName) ? m_Settings.DefaultPackageName : packageName;
        }

        /// <summary>
        /// 资源后端已关闭时抛出取消异常。
        /// </summary>
        private void ThrowIfShutdown()
        {
            if (m_Shutdown)
            {
                throw new OperationCanceledException("The resource service has shut down.", m_ShutdownToken);
            }
        }

        /// <summary>
        /// 移除已释放的资产句柄和已完成的操作登记。
        /// </summary>
        /// <param name="package">资源包状态记录。</param>
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

        /// <summary>
        /// 等待在途操作归还句柄，再销毁当前后端创建的资源包。
        /// </summary>
        /// <param name="packageNames">本次关闭的资源包名称列表。</param>
        /// <returns>完成资源后端关闭的异步任务。</returns>
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

        /// <summary>
        /// 检查是否还有正在初始化或销毁的资源包。
        /// </summary>
        /// <returns>是否存在尚未结束的资源包初始化或销毁。</returns>
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

        /// <summary>
        /// 检查资源包是否仍有未完成的操作。
        /// </summary>
        /// <returns>是否存在未完成的资源操作。</returns>
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

        /// <summary>
        /// 取消当前后端尚未完成的下载器。
        /// </summary>
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
            /// <summary>
            /// 资源包运行设置。
            /// </summary>
            internal readonly ResourcePackageSettings Settings;

            /// <summary>
            /// 当前后端持有的 YooAsset 资源包。
            /// </summary>
            internal readonly ResourcePackage Package;

            /// <summary>
            /// 尚未移除的资产借用登记。
            /// </summary>
            internal readonly List<AssetBorrow> Assets = new List<AssetBorrow>();

            /// <summary>
            /// 需要跟踪完成状态的原生资源操作。
            /// </summary>
            internal readonly List<AsyncOperationBase> Operations = new List<AsyncOperationBase>();

            /// <summary>
            /// 按资源地址登记的场景借用。
            /// </summary>
            internal readonly Dictionary<string, SceneInfo> Scenes = new Dictionary<string, SceneInfo>(StringComparer.Ordinal);

            /// <summary>
            /// 供调用方共同等待的初始化结果。
            /// </summary>
            internal UniTaskCompletionSource Initialization;

            /// <summary>
            /// 本次资源包初始化选项。
            /// </summary>
            internal InitializePackageOptions Options;

            /// <summary>
            /// 本次加载的资源清单版本。
            /// </summary>
            internal string Version;

            /// <summary>
            /// 资源包是否正在初始化。
            /// </summary>
            internal bool Initializing;

            /// <summary>
            /// 资源包是否已初始化并加载清单。
            /// </summary>
            internal bool Ready;

            /// <summary>
            /// 资源包是否正在销毁。
            /// </summary>
            internal bool Destroying;

            /// <summary>
            /// 当前资源包的在途请求数量。
            /// </summary>
            internal int Requests;

            /// <summary>
            /// 初始化资源包状态记录。
            /// </summary>
            /// <param name="settings">资源包运行设置。</param>
            /// <param name="package">当前后端持有的 YooAsset 资源包。</param>
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

            /// <summary>
            /// 初始化资源包的远端地址服务。
            /// </summary>
            /// <param name="settings">资源包运行设置。</param>
            internal RemoteService(ResourcePackageSettings settings)
            {
                if (string.IsNullOrWhiteSpace(settings.HostServer))
                {
                    throw new GameFrameworkException("Resource host server is not configured: " + settings.Name);
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
                // YooAsset 关闭时会终止常驻下载调度器；运行中的任务中断仍按警告记录。
                if ((!YooAssets.IsInitialized || s_DestroyingPackageCount > 0 || s_ShuttingDownPackages.Count > 0)
                    && message == "Async operation 'DownloadSchedulerOperation' has been aborted.")
                {
                    Runtime.Log.Debug("{0}", message);
                    return;
                }
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
                Runtime.Log.Error(exception);
            }
        }
    }
}
