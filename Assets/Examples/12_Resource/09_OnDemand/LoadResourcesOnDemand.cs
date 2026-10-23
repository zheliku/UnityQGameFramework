#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;
using ResourceUpdateFailureEventArgs = UnityQGameFramework.Runtime.ResourceUpdateFailureEventArgs;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadResourcesOnDemand : MonoBehaviour
    {
        [SerializeField] private Button prepareButton, loadButton, recoverButton, updateDataButton, readButton;
        [SerializeField] private Toggle failureToggle;
        [SerializeField] private Text stateText, groupsText, tasksText, binaryText;
        private ResourceFixtureManifest fixture;
        private ResourceHttpServer server;
        private GameObject asset, instance;
        private bool ready, preparing, checkedResources, loading, updating, downloadFailed;
        private int loadCount, loadFailureCount;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "先准备清单。资源文件留到使用时下载。";
                if (fixture == null) return;
                server = new ResourceHttpServer(Path.Combine(ResourceExamplePaths.BuildRoot, fixture.RemoteFolder));
                QGF.Resource.UpdatePrefixUri = server.Url;
                QGF.Resource.UpdateRetryCount = 1;
                // 累计下载 16 KB 后保存一次本地清单；一批下载结束时也会保存。
                QGF.Resource.GenerateReadWriteVersionListLength = 16 * 1024;
                QGF.Event.Subscribe(ResourceUpdateFailureEventArgs.EventId, OnDownloadFailure);
                prepareButton.onClick.AddListener(Prepare);
                loadButton.onClick.AddListener(LoadCube);
                recoverButton.onClick.AddListener(() => UpdateGroup("Base"));
                updateDataButton.onClick.AddListener(() => UpdateGroup("Data"));
                readButton.onClick.AddListener(ReadBinary);
                failureToggle.onValueChanged.AddListener(value => server.RejectResources = value);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Prepare()
        {
            preparing = true;
            // 参数来自本例的构建产物；正式项目通常从版本服务取得。
            QGF.Resource.UpdateVersionList(fixture.VersionLength, fixture.VersionHash,
                fixture.VersionCompressedLength, fixture.VersionCompressedHash, new UpdateVersionListCallbacks(
                    (path, uri) =>
                    {
                        if (this == null) return;
                        // 检查只登记资源的位置和待更新项，不在这里下载资源文件。
                        QGF.Resource.CheckResources((moved, removed, count, length, compressed) =>
                        {
                            if (this == null) return;
                            preparing = false;
                            checkedResources = true;
                            stateText.text = $"已登记 {count} 个待下载资源。直接加载 Cube，或先尝试读取二进制。";
                        });
                    },
                    (uri, message) =>
                    {
                        if (this == null) return;
                        preparing = false;
                        stateText.text = "清单下载失败：" + message;
                    }));
        }

        private void LoadCube()
        {
            loading = true;
            stateText.text = "LoadAsset 已提交，等待下载和加载完成。";
            var resources = QGF.Resource;
            // UpdatableWhilePlaying 中，LoadAsset 会下载尚未就绪的 Bundle 及依赖。
            // 不必先 UpdateResources("Base")，成功回调在资源准备并加载完后执行。
            resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), new LoadAssetCallbacks(
                (name, value, duration, context) =>
                {
                    if (this == null) { if (resources != null) resources.UnloadAsset(value); return; }
                    asset = (GameObject)value;
                    instance = Instantiate(asset, new Vector3(3, -3.5f, 0), Quaternion.Euler(20, 35, 0));
                    loadCount++;
                    loading = downloadFailed = false;
                    stateText.text = "Cube 已下载并加载。Data 组仍可留到稍后更新。";
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    loadFailureCount++;
                    loading = false;
                    stateText.text = "资产加载失败：" + status + "\n" + message;
                }));
        }

        private void OnDownloadFailure(object sender, GameEventArgs args)
        {
            var failure = (ResourceUpdateFailureEventArgs)args;
            // 事件中的 Name 是带扩展名的资源包名，从组内取得后比较。
            if (!loading || failure.RetryCount < failure.TotalRetryCount
                || Array.IndexOf(QGF.Resource.GetResourceGroup("Base").GetResourceNames(), failure.Name) < 0) return;
            // 下载失败事件不等于 LoadAsset 的失败回调：原加载任务仍在等待。
            // 修复下载来源后更新 Base，让原任务继续，不再重复提交 LoadAsset。
            downloadFailed = true;
            stateText.text = "Cube 下载重试已耗尽，加载任务仍在等待。关闭模拟故障，再补下载 Base。";
        }

        private void UpdateGroup(string groupName)
        {
            updating = true;
            stateText.text = "正在更新 " + groupName + "…";
            // 二进制先更新 Data；失败的 Cube 则补下载 Base，恢复原有加载任务。
            QGF.Resource.UpdateResources(groupName, (group, result) =>
            {
                if (this == null) return;
                updating = false;
                stateText.text = result ? groupName + " 已就绪。" : groupName + " 更新失败，可以再次更新。";
            });
        }

        private void ReadBinary()
        {
            // LoadBinary 不会触发按需下载。未准备好时回调 NotReady，先更新所属组再读。
            QGF.Resource.LoadBinary(fixture.DiskAssetName, new LoadBinaryCallbacks(
                (name, bytes, duration, context) =>
                {
                    if (this != null) binaryText.text = "读取成功：\n" + Encoding.UTF8.GetString(bytes);
                },
                (name, status, message, context) =>
                {
                    if (this != null) binaryText.text = "读取结果：" + status + "\n先更新 Data 组，再读同一路径。";
                }));
        }

        private void Update()
        {
            prepareButton.interactable = ready && !preparing && !checkedResources;
            loadButton.interactable = checkedResources && !loading && !updating && asset == null;
            readButton.interactable = checkedResources;
            if (!ready) { recoverButton.interactable = updateDataButton.interactable = false; return; }
            var resources = QGF.Resource;
            bool idle = !updating && QGF.Download.WorkingAgentCount == 0 && QGF.Download.WaitingTaskCount == 0;
            recoverButton.interactable = downloadFailed && idle && !failureToggle.isOn;
            updateDataButton.interactable = checkedResources && idle && !loading && !resources.GetResourceGroup("Data").Ready;
            if (!checkedResources) return;
            var baseGroup = resources.GetResourceGroup("Base");
            var dataGroup = resources.GetResourceGroup("Data");
            // HasAsset 表示清单中的类型/位置；是否已下载要看资源组的 Ready。
            groupsText.text = $"Base  {baseGroup.ReadyCount}/{baseGroup.TotalCount}    Data  {dataGroup.ReadyCount}/{dataGroup.TotalCount}\n"
                + $"Cube：{ResourceExamplePaths.GetAssetStatusText(resources.HasAsset(fixture.PrefabAssetName))}\n"
                + $"二进制：{ResourceExamplePaths.GetAssetStatusText(resources.HasAsset(fixture.DiskAssetName))}";
            tasksText.text = $"按需下载 {resources.UpdateWaitingWhilePlayingCount}    按组更新 {resources.UpdateWaitingCount}\n"
                + $"加载中 {resources.LoadWorkingAgentCount}    等待加载 {resources.LoadWaitingTaskCount}\n"
                + $"当前更新组：{resources.UpdatingResourceGroup?.Name ?? "无"}    HTTP 请求 {server.Requests}\n"
                + $"资产回调：成功 {loadCount} / 失败 {loadFailureCount}";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ResourceUpdateFailureEventArgs.EventId, OnDownloadFailure))
                events.Unsubscribe(ResourceUpdateFailureEventArgs.EventId, OnDownloadFailure);
            server?.Dispose();
            if (instance != null) Destroy(instance);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
