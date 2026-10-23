#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;
using ResourceVerifyStartEventArgs = UnityQGameFramework.Runtime.ResourceVerifyStartEventArgs;
using ResourceVerifySuccessEventArgs = UnityQGameFramework.Runtime.ResourceVerifySuccessEventArgs;
using ResourceVerifyFailureEventArgs = UnityQGameFramework.Runtime.ResourceVerifyFailureEventArgs;

namespace UnityQGameFramework.Examples
{
    public sealed class VerifyCachedResources : MonoBehaviour
    {
        [SerializeField] private Button versionButton, verifyButton, checkButton, repairButton, readButton;
        [SerializeField] private Dropdown damageMode;
        [SerializeField] private Toggle budgetToggle;
        [SerializeField] private Text stateText, verifyText, resultText;
        private ResourceFixtureManifest fixture;
        private ResourceHttpServer server;
        private bool ready, busy, versionReady, verified, checkedResources, verificationPassed;
        private int total, valid, invalid;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "已复制三个资源作为本地缓存。先下载版本清单。";
                if (fixture == null) return;
                server = new ResourceHttpServer(Path.Combine(ResourceExamplePaths.BuildRoot, fixture.RemoteFolder));
                QGF.Resource.UpdatePrefixUri = server.Url;
                QGF.Event.Subscribe(ResourceVerifyStartEventArgs.EventId, OnVerify);
                QGF.Event.Subscribe(ResourceVerifySuccessEventArgs.EventId, OnVerify);
                QGF.Event.Subscribe(ResourceVerifyFailureEventArgs.EventId, OnVerify);
                versionButton.onClick.AddListener(() =>
                {
                    busy = true;
                    QGF.Resource.UpdateVersionList(fixture.VersionLength, fixture.VersionHash,
                        fixture.VersionCompressedLength, fixture.VersionCompressedHash, new UpdateVersionListCallbacks(
                            (path, uri) =>
                            {
                                if (this == null) return;
                                busy = false;
                                versionReady = true;
                                stateText.text = "选择缓存状态，再执行校验。每次运行只改动自己的副本。";
                            },
                            (uri, message) =>
                            {
                                if (this == null) return;
                                busy = false;
                                stateText.text = "清单下载失败：" + message;
                            }));
                });
                verifyButton.onClick.AddListener(Verify);
                checkButton.onClick.AddListener(() =>
                {
                    busy = true;
                    // 校验会移除损坏的缓存条目；检查随后确定哪些资源需要重新下载。
                    QGF.Resource.CheckResources((moved, removed, count, length, compressed) =>
                    {
                        if (this == null) return;
                        busy = false;
                        checkedResources = true;
                        stateText.text = $"检查完成，需要补回 {count} 个资源，下载 {compressed} 字节。";
                    });
                });
                repairButton.onClick.AddListener(() =>
                {
                    busy = true;
                    QGF.Resource.UpdateResources((group, result) =>
                    {
                        if (this == null) return;
                        busy = false;
                        stateText.text = result ? "缺失资源已补回，可以读取。" : "下载失败，可以再次补回。";
                    });
                });
                readButton.onClick.AddListener(Read);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Verify()
        {
            busy = true;
            if (damageMode.value == 3)
            {
                // 取得逻辑文件的物理偏移，关闭容器后只改动数据字节，不改索引和长度。
                string path = Path.Combine(QGF.Resource.ReadWritePath, "example-files.dat");
                var fileSystem = QGF.FileSystem.LoadFileSystem(path, FileSystemAccess.Read);
                long offset = fileSystem.GetFileInfo("example/archive.bytes").Offset;
                QGF.FileSystem.DestroyFileSystem(fileSystem, false);
                using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Position = offset;
                    int value = stream.ReadByte();
                    stream.Position = offset;
                    stream.WriteByte((byte)(value ^ 1));
                }
            }
            else if (damageMode.value != 0)
            {
                string path = Path.Combine(QGF.Resource.ReadWritePath, "example/message.bytes");
                byte[] bytes = File.ReadAllBytes(path);
                if (damageMode.value == 1) bytes[0] ^= 1;
                else Array.Resize(ref bytes, bytes.Length - 1);
                File.WriteAllBytes(path, bytes);
            }
            // 要在 CheckResources 之前校验。返回 false 表示发现损坏，并已从本地清单剔除。
            VerifyResourcesCompleteCallback completed = result =>
            {
                if (this == null) return;
                verificationPassed = result;
                verified = true;
                busy = false;
                stateText.text = result ? "所有缓存校验通过，继续检查版本差异。" : "发现损坏缓存，继续检查需要补回的资源。";
            };
            if (budgetToggle.isOn) QGF.Resource.VerifyResources(16 * 1024, completed);
            else QGF.Resource.VerifyResources(completed);
        }

        private void OnVerify(object sender, GameEventArgs args)
        {
            if (args is ResourceVerifyStartEventArgs start) total = start.Count;
            else if (args is ResourceVerifySuccessEventArgs) valid++;
            else if (args is ResourceVerifyFailureEventArgs) invalid++;
            verifyText.text = $"缓存 {total} 个    通过 {valid}    损坏 {invalid}\n已完成 {valid + invalid}/{total}";
        }

        private void Read()
        {
            busy = true;
            byte[] bytes = QGF.Resource.LoadBinaryFromFileSystem(fixture.FileSystemAssetName);
            // 同一条资源路径，修复后仍按正常接口读取，无需让业务处理损坏文件。
            QGF.Resource.LoadBinary(fixture.DiskAssetName, new LoadBinaryCallbacks(
                (name, data, duration, context) =>
                {
                    if (this == null) return;
                    busy = false;
                    resultText.text = $"容器内文件：{bytes.Length} 字节\n前 8 字节：{BitConverter.ToString(bytes, 0, 8)}\n"
                        + Encoding.UTF8.GetString(data);
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    busy = false;
                    stateText.text = "读取失败：" + message;
                }));
        }

        private void Update()
        {
            versionButton.interactable = ready && !busy && !versionReady;
            damageMode.interactable = budgetToggle.interactable = !busy && !verified;
            verifyButton.interactable = versionReady && !busy && !verified;
            checkButton.interactable = verified && !busy && !checkedResources;
            repairButton.interactable = checkedResources && !busy && QGF.Resource.UpdateCandidateCount > 0;
            readButton.interactable = checkedResources && !busy && QGF.Resource.GetResourceGroup().Ready;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(ResourceVerifyStartEventArgs.EventId, OnVerify))
                    events.Unsubscribe(ResourceVerifyStartEventArgs.EventId, OnVerify);
                if (events.Check(ResourceVerifySuccessEventArgs.EventId, OnVerify))
                    events.Unsubscribe(ResourceVerifySuccessEventArgs.EventId, OnVerify);
                if (events.Check(ResourceVerifyFailureEventArgs.EventId, OnVerify))
                    events.Unsubscribe(ResourceVerifyFailureEventArgs.EventId, OnVerify);
            }
            server?.Dispose();
        }
    }
}
#endif
