using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class ResourceComponent
    {
        [SerializeField]
        private ResourceSettings m_Settings;

        private YooAssetResourceBackend m_YooAssetBackend;

        /// <summary>
        /// 获取资源配置。
        /// </summary>
        public ResourceSettings Settings
        {
            get
            {
                return m_Settings;
            }
        }

        /// <summary>
        /// 获取默认资源包配置的 YooAsset 运行模式。
        /// </summary>
        public EPlayMode PlayMode
        {
            get
            {
                return m_Settings.GetPackage().PlayMode;
            }
        }

        /// <summary>
        /// 获取正在执行的资源请求数量。
        /// </summary>
        public int LoadingCount
        {
            get
            {
                return m_ResourceManager.LoadingCount;
            }
        }

        /// <summary>
        /// 获取或设置编辑器资源包的构建方法。
        /// </summary>
        public static Func<ResourceSettings, string, bool, string> EditorPackageBuilder
        {
            get; set;
        }

        /// <summary>
        /// 获取资源管理器。
        /// </summary>
        internal IResourceManager ResourceManager
        {
            get
            {
                return m_ResourceManager;
            }
        }

        /// <summary>
        /// 初始化资源包并加载清单。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="cancellationToken">取消标记，仅取消本次等待。</param>
        /// <returns>已初始化的资源包。</returns>
        public UniTask<ResourcePackage> InitializePackageAsync(string packageName = null, CancellationToken cancellationToken = default)
        {
            return GetYooAssetBackend().InitializePackageAsync(packageName, cancellationToken);
        }

        /// <summary>
        /// 使用指定文件系统初始化资源包。
        /// </summary>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="options">本次初始化使用的文件系统选项。</param>
        /// <param name="version">清单版本，为空时请求当前版本。</param>
        /// <param name="cancellationToken">取消标记，仅取消本次等待。</param>
        /// <returns>已初始化的资源包。</returns>
        public UniTask<ResourcePackage> InitializePackageAsync(string packageName, InitializePackageOptions options, string version = null, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            return GetYooAssetBackend().InitializePackageAsync(packageName, options, version, cancellationToken);
        }

        /// <summary>
        /// 销毁没有活跃借用或请求的资源包。
        /// </summary>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="cancellationToken">取消标记，销毁发出后仍会完成包注销。</param>
        /// <returns>销毁任务。</returns>
        public UniTask DestroyPackageAsync(string packageName = null, CancellationToken cancellationToken = default)
        {
            return GetYooAssetBackend().DestroyPackageAsync(packageName, cancellationToken);
        }

        /// <summary>
        /// 创建资源包下载器。
        /// </summary>
        /// <param name="options">下载标签、并发数和重试选项。</param>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <returns>未开始的资源下载操作；使用时交给 DownloadResourcesAsync，放弃时调用 CancelDownload。</returns>
        public ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options, string packageName = null)
        {
            return GetYooAssetBackend().CreateResourceDownloader(options, packageName);
        }

        /// <summary>
        /// 开始下载资源并等待完成。
        /// </summary>
        /// <param name="downloader">由当前资源组件创建的下载器。</param>
        /// <param name="cancellationToken">取消标记，取消时终止本次下载。</param>
        /// <returns>下载任务，下载失败时抛出异常。</returns>
        public UniTask DownloadResourcesAsync(ResourceDownloaderOperation downloader, CancellationToken cancellationToken = default)
        {
            return GetYooAssetBackend().DownloadResourcesAsync(downloader, cancellationToken);
        }

        /// <summary>
        /// 清理资源包磁盘缓存。
        /// </summary>
        /// <param name="clearMethod">YooAsset.ClearCacheMethods 中的清理方式。</param>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="clearParameter">清理方式所需的附加参数。</param>
        /// <param name="cancellationToken">取消标记，仅取消本次等待。</param>
        /// <returns>缓存清理任务。</returns>
        public UniTask ClearCacheAsync(string clearMethod, string packageName = null, object clearParameter = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(clearMethod))
            {
                throw new ArgumentException("Clear method is invalid.", nameof(clearMethod));
            }
            return GetYooAssetBackend().ClearCacheAsync(packageName, clearMethod, clearParameter, cancellationToken);
        }

        /// <summary>
        /// 异步加载资产，使用结束后通过 UnloadAsset 归还。
        /// </summary>
        /// <typeparam name="T">资产类型。</typeparam>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>借用的资产。</returns>
        public async UniTask<T> LoadAssetAsync<T>(string assetName, string packageName = null, int priority = DefaultPriority, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            AssetHandle handle = await GetYooAssetBackend().LoadAssetAsync(assetName, typeof(T), packageName, priority, true, cancellationToken);
            return (T)handle.AssetObject;
        }

        /// <summary>
        /// 异步加载资产句柄，由调用者释放本次句柄。
        /// </summary>
        /// <typeparam name="T">资产类型。</typeparam>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>本次借用的独立资产句柄。</returns>
        public UniTask<AssetHandle> LoadAssetHandleAsync<T>(string assetName, string packageName = null, int priority = DefaultPriority, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            return GetYooAssetBackend().LoadAssetAsync(assetName, typeof(T), packageName, priority, false, cancellationToken);
        }

        /// <summary>
        /// 归还指定资源包中借用的资产。
        /// </summary>
        /// <param name="asset">要归还的资产。</param>
        /// <param name="packageName">借用时指定的资源包名称。</param>
        public void UnloadAsset(object asset, string packageName)
        {
            GetYooAssetBackend().UnloadAsset(asset, packageName);
        }

        /// <summary>
        /// 异步读取二进制内容，完成后自动归还资源句柄。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="packageName">资源包名称，为空时使用默认包。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>拥有独立生命周期的文件内容。</returns>
        public UniTask<byte[]> LoadBinaryAsync(string assetName, string packageName = null, CancellationToken cancellationToken = default)
        {
            return GetYooAssetBackend().LoadBinaryAsync(assetName, packageName, cancellationToken);
        }

        /// <summary>
        /// 异步清理资源包中未使用的资产。
        /// </summary>
        /// <param name="cancellationToken">取消标记，仅取消本次等待。</param>
        /// <returns>内存清理任务。</returns>
        public UniTask UnloadUnusedAssetsAsync(CancellationToken cancellationToken = default)
        {
            return GetYooAssetBackend().UnloadUnusedAssetsAsync(cancellationToken);
        }

        /// <summary>
        /// 获取已加载场景的实际名称。
        /// </summary>
        /// <param name="sceneAssetName">场景资源地址。</param>
        /// <returns>实际场景名称；未加载时返回空。</returns>
        internal string GetLoadedSceneName(string sceneAssetName)
        {
            return GetYooAssetBackend().GetLoadedSceneName(sceneAssetName, null);
        }

        /// <summary>
        /// 获取当前资源组件使用的 YooAsset 后端。
        /// </summary>
        /// <returns>YooAsset 资源后端；尚未配置时抛出异常。</returns>
        private YooAssetResourceBackend GetYooAssetBackend()
        {
            if (m_YooAssetBackend == null)
            {
                throw new GameFrameworkException("Assign ResourceSettings to the Resource component before using YooAsset APIs.");
            }
            return m_YooAssetBackend;
        }
    }
}
