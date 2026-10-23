#if UNITY_EDITOR
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;
using ResourceApplySuccessEventArgs = UnityQGameFramework.Runtime.ResourceApplySuccessEventArgs;

namespace UnityQGameFramework.Examples
{
    public sealed class ApplyResourcePack : MonoBehaviour
    {
        [SerializeField] private Button prepareButton, verifyButton, applyButton, loadButton;
        [SerializeField] private Toggle corruptToggle;
        [SerializeField] private Text stateText, packText, progressText;
        private ResourceFixtureManifest fixture;
        private ResourceHttpServer server;
        private string pack, damagedPack;
        private GameObject asset, instance;
        private bool ready, busy, prepared, verified, applied;
        private int appliedCount, packLength;

        private string SelectedPack => corruptToggle.isOn ? damagedPack : pack;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "先读取版本清单，确定当前缺少哪些资源。";
                if (fixture == null || string.IsNullOrEmpty(fixture.ResourcePackFile)) return;
                pack = Path.Combine(ResourceExamplePaths.BuildRoot, fixture.ResourcePackFile);
                // 只修改本次运行的副本，原包保持完整。长度不变，末尾一字节改变会使 CRC 校验失败。
                damagedPack = Path.Combine(QGF.Resource.ReadWritePath, "DamagedPack.dat");
                byte[] bytes = File.ReadAllBytes(pack);
                packLength = bytes.Length;
                bytes[bytes.Length - 1] ^= 1;
                File.WriteAllBytes(damagedPack, bytes);
                server = new ResourceHttpServer(Path.Combine(ResourceExamplePaths.BuildRoot, fixture.RemoteFolder));
                QGF.Resource.UpdatePrefixUri = server.Url;
                QGF.Event.Subscribe(ResourceApplySuccessEventArgs.EventId, OnApplied);
                prepareButton.onClick.AddListener(Prepare);
                verifyButton.onClick.AddListener(() =>
                {
                    // 校验整个包的长度和 CRC；成功不表示资源已应用到可写区。
                    verified = QGF.Resource.VerifyResourcePack(SelectedPack);
                    stateText.text = verified ? "资源包校验通过，可以应用。" : "资源包校验失败，请切回完整包。";
                });
                corruptToggle.onValueChanged.AddListener(_ => verified = false);
                applyButton.onClick.AddListener(() =>
                {
                    busy = true;
                    appliedCount = 0;
                    // 资源包可以随安装程序或独立下载提供；应用只补齐检查后缺失的资源。
                    QGF.Resource.ApplyResources(SelectedPack, (path, result) =>
                    {
                        if (this == null) return;
                        busy = false;
                        applied = result;
                        stateText.text = result ? "资源包已应用；这一步没有逐个下载资源。" : "资源包应用失败。";
                    });
                });
                loadButton.onClick.AddListener(LoadCube);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Prepare()
        {
            busy = true;
            // 与在线更新使用同一份版本清单，资源文件稍后从本地资源包取得。
            QGF.Resource.UpdateVersionList(fixture.VersionLength, fixture.VersionHash,
                fixture.VersionCompressedLength, fixture.VersionCompressedHash, new UpdateVersionListCallbacks(
                    (path, uri) => QGF.Resource.CheckResources((moved, removed, count, length, compressed) =>
                    {
                        if (this == null) return;
                        busy = false;
                        prepared = true;
                        stateText.text = $"缺少 {count} 个资源。请选择完整包或损坏副本进行校验。";
                    }),
                    (uri, message) =>
                    {
                        if (this == null) return;
                        busy = false;
                        stateText.text = "清单下载失败：" + message;
                    }));
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
                    instance = Instantiate(asset, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                    busy = false;
                    stateText.text = "已加载资源包应用后的 Cube。";
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    busy = false;
                    stateText.text = "加载失败：" + message;
                }));
        }

        private void OnApplied(object sender, GameEventArgs args)
        {
            var success = (ResourceApplySuccessEventArgs)args;
            appliedCount++;
            progressText.text = $"已应用 {appliedCount}/{QGF.Resource.ResourceCount}\n最近完成：{success.Name}";
        }

        private void Update()
        {
            prepareButton.interactable = ready && !busy && !prepared;
            verifyButton.interactable = prepared && !busy && !applied;
            corruptToggle.interactable = prepared && !busy && !applied;
            applyButton.interactable = prepared && verified && !busy && !applied;
            loadButton.interactable = applied && !busy && asset == null;
            if (!ready) return;
            packText.text = $"{Path.GetFileName(SelectedPack)}\n{packLength} 字节\n"
                + $"校验通过：{verified}    已应用：{applied}\nHTTP 请求 {server.Requests} 次（版本清单）";
            if (busy && prepared && !applied)
                progressText.text = $"正在应用：{Path.GetFileName(QGF.Resource.ApplyingResourcePackPath)}\n剩余 {QGF.Resource.ApplyWaitingCount} 个资源";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ResourceApplySuccessEventArgs.EventId, OnApplied))
                events.Unsubscribe(ResourceApplySuccessEventArgs.EventId, OnApplied);
            server?.Dispose();
            if (instance != null) Destroy(instance);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
