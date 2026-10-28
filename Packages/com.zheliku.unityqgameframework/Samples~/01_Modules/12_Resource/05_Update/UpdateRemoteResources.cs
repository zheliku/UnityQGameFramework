using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using YooAsset;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 从本地 HTTP 服务器按标签下载资源，演示失败、取消和重试。
    /// </summary>
    public sealed class UpdateRemoteResources : MonoBehaviour
    {
        [SerializeField] private Button versionButton, checkButton, updateButton, stopButton, loadButton;
        [SerializeField] private Dropdown concurrencyMode, groupMode;
        [SerializeField] private Toggle autoLoadToggle, failureToggle;
        [SerializeField] private Text stateText, groupsText, transferText, serverText;
        private ResourceHttpServer server;
        private HostPlayModeOptions options;
        private ResourcePackage package;
        private ResourceDownloaderOperation downloader;
        private GameObject asset, instance;
        private bool busy, cancelled;

        private void Start()
        {
            ResourceFixtureManifest fixture = ResourceExamplePaths.Fixture;
            if (fixture == null || !Directory.Exists(fixture.RemoteFolder))
            {
                stateText.text = ResourceExamplePaths.Notice;
                return;
            }
            server = new ResourceHttpServer(fixture.RemoteFolder);
            string session = Path.Combine(ResourceExamplePaths.BuildRoot, "Sessions", Guid.NewGuid().ToString("N"));
            // 仅覆盖这个包的本次初始化，不修改 ResourceSettings 资产或项目的全局配置。
            options = new HostPlayModeOptions
            {
                // 本示例的全部资源来自 HTTP，不创建需要 BuiltinCatalog 的内置文件系统。
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(server, Path.Combine(session, "Cache"))
            };
            versionButton.onClick.AddListener(Initialize);
            checkButton.onClick.AddListener(Release);
            updateButton.onClick.AddListener(Download);
            stopButton.onClick.AddListener(() =>
            {
                cancelled = true;
                downloader.CancelDownload();
            });
            loadButton.onClick.AddListener(LoadCube);
            failureToggle.onValueChanged.AddListener(value => server.RejectResources = value);
            stateText.text = "Initialize the Host package, then download by tag. Enable the 404 fault to test retries.";
        }

        private async void Initialize()
        {
            busy = true;
            try
            {
                package = await QGF.Resource.InitializePackageAsync(ResourceExamplePaths.RemotePackageName, options, cancellationToken: destroyCancellationToken);
                stateText.text = "Version and manifest loaded. Resource files have not been downloaded yet.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = "Initialization failed. Retry: " + exception.Message;
            }
            finally
            {
                busy = false;
            }
        }

        private async void Download()
        {
            busy = true;
            cancelled = false;
            try
            {
                int concurrency = concurrencyMode.value == 0 ? 1 : concurrencyMode.value == 1 ? 2 : 4;
                // null 表示全部资源；标签来自 YooAsset Collector 中的 Base 和 Data 组。
                string[] tags = groupMode.value == 0 ? null : new[] { groupMode.value == 1 ? "Base" : "Data" };
                downloader = QGF.Resource.CreateResourceDownloader(new ResourceDownloaderOptions(tags, concurrency, 1), ResourceExamplePaths.RemotePackageName);
                // 下载失败转为异常，物体销毁时会取消等待并终止本次下载。
                await QGF.Resource.DownloadResourcesAsync(downloader, destroyCancellationToken);
                if (cancelled)
                {
                    stateText.text = "Download canceled. Retry downloads only the remaining files.";
                    return;
                }
                stateText.text = "Selected tags downloaded. Download again to observe cache hits.";
                if (autoLoadToggle.isOn)
                {
                    LoadCube();
                }
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = cancelled
                    ? "Download canceled. Retry downloads only the remaining files."
                    : "Download failed. Disable the 404 fault to retry: " + exception.Message;
            }
            finally
            {
                busy = false;
            }
        }

        private async void LoadCube()
        {
            if (asset != null)
            {
                return;
            }
            busy = true;
            try
            {
                // 地址 Base_Cube 来自 YooAsset Collector 的 AddressByGroupAndFileName 规则，不是资产路径或 GUID。
                // 指定所属包借用资产，归还时也指定同一包。
                asset = await QGF.Resource.LoadAssetAsync<GameObject>(ResourceExamplePaths.RemoteCubeAddress, ResourceExamplePaths.RemotePackageName, cancellationToken: destroyCancellationToken);
                instance = Instantiate(asset, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "Cube loaded from the Host package.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = exception.Message;
            }
            finally
            {
                busy = false;
            }
        }

        private async void Release()
        {
            busy = true;
            try
            {
                Destroy(instance);
                await UniTask.NextFrame(destroyCancellationToken);
                if (asset != null)
                {
                    QGF.Resource.UnloadAsset(asset, ResourceExamplePaths.RemotePackageName);
                    asset = null;
                }
                // 清理内存不删除磁盘缓存，下次下载仍可命中已经下载的文件。
                await QGF.Resource.UnloadUnusedAssetsAsync(destroyCancellationToken);
                stateText.text = "Instances and assets released; unused resources cleared. Disk cache retained.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                busy = false;
            }
        }

        private void Update()
        {
            bool ready = package != null && package.PackageValid;
            versionButton.interactable = server != null && !ready && !busy;
            checkButton.interactable = ready && !busy;
            updateButton.interactable = ready && !busy;
            stopButton.interactable = downloader != null && !downloader.IsDone;
            loadButton.interactable = ready && !busy && asset == null;
            concurrencyMode.interactable = groupMode.interactable = !busy;
            if (server == null)
            {
                return;
            }
            serverText.text = $"Local address: {server.Url}\nHTTP requests: {server.Requests} times";
            if (ready)
            {
                groupsText.text = $"Package: {package.PackageName}\nManifest version: {package.GetPackageVersion()}\nBase asset: {package.GetAssetInfos("Base").Length}; Data asset: {package.GetAssetInfos("Data").Length}";
            }
            if (downloader != null)
            {
                transferText.text = $"File: {downloader.CurrentDownloadCount}/{downloader.TotalDownloadCount}\nBytes: {downloader.CurrentDownloadBytes}/{downloader.TotalDownloadBytes}";
            }
        }

        private void OnDestroy()
        {
            if (downloader != null && !downloader.IsDone)
            {
                downloader.CancelDownload();
            }
            if (instance != null)
            {
                Destroy(instance);
            }
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null)
            {
                resources.UnloadAsset(asset, ResourceExamplePaths.RemotePackageName);
            }
            server?.Dispose();
        }

    }
}
