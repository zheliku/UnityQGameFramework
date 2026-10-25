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
            stateText.text = "先初始化联机包，再选择标签下载。可开启 404 故障后重试。";
        }

        private async void Initialize()
        {
            busy = true;
            try
            {
                package = await QGF.Resource.InitializePackageAsync(ResourceExamplePaths.RemotePackageName, options, cancellationToken: destroyCancellationToken);
                stateText.text = "版本和清单已加载，资源文件尚未下载。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = "初始化失败，可重试：" + exception.Message;
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
                // null 表示全部资源；标签来自 Profile 中的 Base 和 Data 组。
                string[] tags = groupMode.value == 0 ? null : new[] { groupMode.value == 1 ? "Base" : "Data" };
                downloader = QGF.Resource.CreateResourceDownloader(new ResourceDownloaderOptions(tags, concurrency, 1), ResourceExamplePaths.RemotePackageName);
                // 下载失败转为异常，物体销毁时会取消等待并终止本次下载。
                await QGF.Resource.DownloadResourcesAsync(downloader, destroyCancellationToken);
                if (cancelled)
                {
                    stateText.text = "下载已取消。再次下载时只请求尚未完成的文件。";
                    return;
                }
                stateText.text = "所选标签下载完成。再次点击可观察缓存命中。";
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
                    ? "下载已取消。再次下载时只请求尚未完成的文件。"
                    : "下载失败。关闭 404 开关后可重试：" + exception.Message;
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
                // 指定所属包借用资产，归还时也指定同一包；不会与默认 Modules 包混淆。
                asset = await QGF.Resource.LoadAssetAsync<GameObject>(ResourceExamplePaths.CubeGuid, ResourceExamplePaths.RemotePackageName, cancellationToken: destroyCancellationToken);
                instance = Instantiate(asset, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "已从联机包加载 Cube。";
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
                stateText.text = "实例与资产已归还，未使用资源已清理；磁盘缓存保留。";
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
            serverText.text = $"本地地址：{server.Url}\nHTTP 请求：{server.Requests} 次";
            if (ready)
            {
                groupsText.text = $"包：{package.PackageName}\n清单版本：{package.GetPackageVersion()}\nBase 资产：{package.GetAssetInfos("Base").Length}；Data 资产：{package.GetAssetInfos("Data").Length}";
            }
            if (downloader != null)
            {
                transferText.text = $"文件：{downloader.CurrentDownloadCount}/{downloader.TotalDownloadCount}\n字节：{downloader.CurrentDownloadBytes}/{downloader.TotalDownloadBytes}";
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
