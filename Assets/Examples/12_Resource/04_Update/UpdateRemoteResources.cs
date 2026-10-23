#if UNITY_EDITOR
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;
using ResourceUpdateChangedEventArgs = UnityQGameFramework.Runtime.ResourceUpdateChangedEventArgs;
using ResourceUpdateFailureEventArgs = UnityQGameFramework.Runtime.ResourceUpdateFailureEventArgs;

namespace UnityQGameFramework.Examples
{
    public sealed class UpdateRemoteResources : MonoBehaviour
    {
        [SerializeField] private Button versionButton, checkButton, updateButton, stopButton, loadButton;
        [SerializeField] private Dropdown verifyMode, groupMode;
        [SerializeField] private Toggle ignoreVariantToggle, failureToggle;
        [SerializeField] private Text stateText, groupsText, transferText, serverText;
        private ResourceFixtureManifest fixture;
        private ResourceHttpServer server;
        private GameObject asset, instance;
        private IResourceGroupCollection allGroups;
        private bool ready, busy, versionReady, checkedResources, updating, verificationPassed;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "先下载版本清单，再检查本地资源。";
                if (fixture == null) return;
                server = new ResourceHttpServer(Path.Combine(ResourceExamplePaths.BuildRoot, fixture.RemoteFolder));
                QGF.Resource.UpdatePrefixUri = server.Url;
                QGF.Resource.UpdateRetryCount = 1;
                // 更新通过 Download 模块下载文件；进度和失败由 Event 模块通知界面。
                QGF.Event.Subscribe(ResourceUpdateChangedEventArgs.EventId, OnProgress);
                QGF.Event.Subscribe(ResourceUpdateFailureEventArgs.EventId, OnFailure);
                versionButton.onClick.AddListener(DownloadVersion);
                checkButton.onClick.AddListener(Check);
                updateButton.onClick.AddListener(DownloadGroup);
                stopButton.onClick.AddListener(() =>
                {
                    // 清空待安排的资源，不会取消当前文件，也不会触发本次完成回调。
                    QGF.Resource.StopUpdateResources();
                    updating = false;
                    stateText.text = "已停止安排后续资源；当前文件继续下载。";
                });
                loadButton.onClick.AddListener(LoadCube);
                failureToggle.onValueChanged.AddListener(value => server.RejectResources = value);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void DownloadVersion()
        {
            busy = true;
            // 实际项目从版本服务取得这四个校验参数；本例使用构建菜单写出的 fixture.json。
            var result = QGF.Resource.CheckVersionList(fixture.ResourceVersion);
            if (result == CheckVersionListResult.Updated)
            {
                versionReady = true;
                busy = false;
                stateText.text = "版本清单已是最新。";
                return;
            }
            QGF.Resource.UpdateVersionList(fixture.VersionLength, fixture.VersionHash,
                fixture.VersionCompressedLength, fixture.VersionCompressedHash, new UpdateVersionListCallbacks(
                    (path, uri) =>
                    {
                        if (this == null) return;
                        versionReady = true;
                        busy = false;
                        stateText.text = $"版本清单已下载：{QGF.Resource.CheckVersionList(fixture.ResourceVersion)}。";
                    },
                    (uri, message) =>
                    {
                        if (this == null) return;
                        busy = false;
                        stateText.text = "清单下载失败：" + message;
                    }));
        }

        private void Check()
        {
            busy = true;
            if (verifyMode.value == 0) CheckLocalResources();
            else
            {
                // VerifyResources 检查已有缓存的 CRC，必须放在 CheckResources 前面。
                // 本例每次运行使用空缓存，因此首次校验没有文件；下载工作由后面的检查决定。
                VerifyResourcesCompleteCallback completed = result =>
                {
                    if (this == null) return;
                    verificationPassed = result;
                    CheckLocalResources();
                };
                if (verifyMode.value == 1) QGF.Resource.VerifyResources(completed);
                else QGF.Resource.VerifyResources(16 * 1024, completed);
            }
        }

        private void CheckLocalResources()
        {
            CheckResourcesCompleteCallback completed = (moved, removed, count, length, compressed) =>
            {
                if (this == null) return;
                // 集合去重合并 Base 与 Data，后续属性会随资源就绪状态变化。
                allGroups = QGF.Resource.GetResourceGroupCollection(new[] { "Base", "Data" });
                checkedResources = true;
                busy = false;
                stateText.text = $"待更新 {count} 个资源，下载 {compressed} 字节。\n移动 {moved} 个，移除 {removed} 个。";
            };
            // ignoreOtherVariant 决定是否保留其他变体；本例构建未设置变体。
            if (ignoreVariantToggle.isOn) QGF.Resource.CheckResources(true, completed);
            else QGF.Resource.CheckResources(completed);
        }

        private void DownloadGroup()
        {
            updating = true;
            stateText.text = "正在更新所选资源组…";
            UpdateResourcesCompleteCallback completed = (group, result) =>
            {
                if (this == null) return;
                updating = false;
                stateText.text = result ? "所选资源组更新完成。" : "更新失败；关闭模拟故障后可再次更新。";
            };
            // 默认组包含全部资源。单独更新 Base 后即可加载 Cube，不必等待 Data 组。
            if (groupMode.value == 0) QGF.Resource.UpdateResources(completed);
            else QGF.Resource.UpdateResources(groupMode.value == 1 ? "Base" : "Data", completed);
        }

        private void LoadCube()
        {
            busy = true;
            var resources = QGF.Resource;
            resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), new LoadAssetCallbacks(
                (name, value, duration, context) =>
                {
                    if (this == null) { if (resources != null) resources.UnloadAsset(value); return; }
                    asset = (GameObject)value;
                    instance = Instantiate(asset, new Vector3(3, -4, 0), Quaternion.Euler(20, 35, 0));
                    busy = false;
                    stateText.text = "已从下载后的 AssetBundle 创建 Cube。";
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    busy = false;
                    stateText.text = "加载失败：" + message;
                }));
        }

        private void OnProgress(object sender, GameEventArgs args)
        {
            var progress = (ResourceUpdateChangedEventArgs)args;
            // 事件参数会回到引用池，只取出本次显示的数据。
            transferText.text = $"{progress.Name}\n{progress.CurrentLength}/{progress.CompressedLength} 字节";
        }

        private void OnFailure(object sender, GameEventArgs args)
        {
            var failure = (ResourceUpdateFailureEventArgs)args;
            transferText.text = $"{failure.Name} 下载失败\n已重试 {failure.RetryCount}/{failure.TotalRetryCount}\n{failure.ErrorMessage}";
        }

        private void Update()
        {
            versionButton.interactable = ready && !busy && !versionReady;
            checkButton.interactable = versionReady && !busy && !checkedResources;
            verifyMode.interactable = ignoreVariantToggle.interactable = !busy && !checkedResources;
            groupMode.interactable = ready && !updating;
            stopButton.interactable = updating;
            loadButton.interactable = checkedResources && !busy && asset == null && QGF.Resource.GetResourceGroup("Base").Ready;
            if (!ready) { updateButton.interactable = false; return; }
            serverText.text = $"本地更新地址：{server.Url}\nHTTP 请求 {server.Requests} 次";
            if (!checkedResources) { updateButton.interactable = false; return; }
            var resources = QGF.Resource;
            var selected = resources.GetResourceGroup(groupMode.value == 0 ? "" : groupMode.value == 1 ? "Base" : "Data");
            // 停止后先等当前下载结束，再重新安排剩余资源，避免重复加入同一文件。
            updateButton.interactable = !busy && !updating && !selected.Ready && QGF.Download.WorkingAgentCount == 0 && QGF.Download.WaitingTaskCount == 0;
            var baseGroup = resources.GetResourceGroup("Base");
            var dataGroup = resources.GetResourceGroup("Data");
            // ReadyLength 只统计已经就绪的整个资源；文件传输进度看 OnProgress 的 CurrentLength。
            groupsText.text = $"Base  {baseGroup.ReadyCount}/{baseGroup.TotalCount}    Data  {dataGroup.ReadyCount}/{dataGroup.TotalCount}\n"
                + $"所选组：{ResourceExamplePaths.GetReadinessText(selected.Ready)}；进度 {selected.Progress:P0}\n"
                + $"资源大小 {selected.ReadyLength}/{selected.TotalLength} 字节\n"
                + $"压缩大小 {selected.ReadyCompressedLength}/{selected.TotalCompressedLength} 字节\n"
                + $"合并 Base + Data：{ResourceExamplePaths.GetReadinessText(allGroups.Ready)}，进度 {allGroups.Progress:P0}\n"
                + $"待更新 {resources.UpdateCandidateCount}    队列中 {resources.UpdateWaitingCount}";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ResourceUpdateChangedEventArgs.EventId, OnProgress))
                    events.Unsubscribe(ResourceUpdateChangedEventArgs.EventId, OnProgress);
                if (events.Check(ResourceUpdateFailureEventArgs.EventId, OnFailure))
                    events.Unsubscribe(ResourceUpdateFailureEventArgs.EventId, OnFailure);
            }
            server?.Dispose();
            if (instance != null) Destroy(instance);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
