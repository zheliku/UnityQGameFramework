#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ReadBinaryResource : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, readButton;
        [SerializeField] private Dropdown readMode;
        [SerializeField] private Text stateText, pathText, bytesText;
        private ResourceFixtureManifest fixture;
        private byte[] result;
        private bool initialized, loading, ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "先初始化 Package，再选择二进制读取入口。";
                initializeButton.onClick.AddListener(() =>
                {
                    loading = true;
                    // 初始化资源清单后才能查询磁盘和文件系统中的二进制。
                    QGF.Resource.InitResources(() =>
                    {
                        if (this == null) return;
                        initialized = true;
                        loading = false;
                        stateText.text = "已初始化，Archive 在文件系统中，Message 是磁盘文件。";
                    });
                });
                readButton.onClick.AddListener(Read);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Read()
        {
            var resources = QGF.Resource;
            string file = fixture.FileSystemAssetName;
            result = null;
            if (readMode.value < 2)
            {
                loading = true;
                // 磁盘二进制通过 LoadBinary 异步读取；返回 byte[] 不需要 UnloadAsset。
                var callbacks = new LoadBinaryCallbacks((name, bytes, duration, context) =>
                {
                    if (this == null) return;
                    result = bytes;
                    loading = false;
                    stateText.text = $"异步读取 {bytes.Length} 字节；上下文匹配 {ReferenceEquals(context, this)}。";
                }, (name, status, message, context) =>
                {
                    if (this == null) return;
                    loading = false;
                    stateText.text = "读取失败：" + message;
                });
                if (readMode.value == 0) resources.LoadBinary(fixture.DiskAssetName, callbacks);
                else resources.LoadBinary(fixture.DiskAssetName, callbacks, this);
                return;
            }
            // 文件系统入口同步读取。startIndex 是目标缓冲位置，offset 是源文件位置。
            var buffer = new byte[readMode.value < 6 ? resources.GetBinaryLength(file) + 4 : 16];
            int count = 0, destination = 0;
            switch (readMode.value)
            {
                case 2: result = resources.LoadBinaryFromFileSystem(file); break;
                case 3: count = resources.LoadBinaryFromFileSystem(file, buffer); break;
                case 4: destination = 2; count = resources.LoadBinaryFromFileSystem(file, buffer, 2); break;
                case 5: destination = 2; count = resources.LoadBinaryFromFileSystem(file, buffer, 2, 12); break;
                case 6: result = resources.LoadBinarySegmentFromFileSystem(file, 8); break;
                case 7: result = resources.LoadBinarySegmentFromFileSystem(file, 17, 8); break;
                case 8: count = resources.LoadBinarySegmentFromFileSystem(file, buffer); break;
                case 9: count = resources.LoadBinarySegmentFromFileSystem(file, buffer, 8); break;
                case 10: destination = 2; count = resources.LoadBinarySegmentFromFileSystem(file, buffer, 2, 8); break;
                case 11: count = resources.LoadBinarySegmentFromFileSystem(file, 17, buffer); break;
                case 12: count = resources.LoadBinarySegmentFromFileSystem(file, 17, buffer, 8); break;
                default: destination = 2; count = resources.LoadBinarySegmentFromFileSystem(file, 17, buffer, 2, 8); break;
            }
            if (result == null)
            {
                result = new byte[count];
                Array.Copy(buffer, destination, result, 0, count);
            }
            stateText.text = $"同步读取 {result.Length} 字节。";
        }

        private void Update()
        {
            if (!ready) return;
            initializeButton.interactable = fixture != null && !initialized && !loading;
            readButton.interactable = initialized && !loading;
            if (!initialized) return;
            string assetName = readMode.value < 2 ? fixture.DiskAssetName : fixture.FileSystemAssetName;
            // GetBinaryPath 提供完整路径；重载同时返回存储区域和相对路径信息。
            string path = QGF.Resource.GetBinaryPath(assetName);
            bool found = QGF.Resource.GetBinaryPath(assetName, out bool readOnly, out bool inFileSystem, out string relative, out string file);
            pathText.text = $"资源清单：{ResourceExamplePaths.GetAssetStatusText(QGF.Resource.HasAsset(assetName))}\n"
                + $"长度：{QGF.Resource.GetBinaryLength(assetName)} 字节\n"
                + $"路径：{(found ? "可用" : "未找到")}    区域：{(readOnly ? "只读" : "读写")}\n"
                + $"存储：{(inFileSystem ? "文件系统" : "散文件")}\n{relative}/{file}\n{path}";
            bytesText.text = result == null ? "尚未读取。" : $"已读取 {result.Length} 字节\n前 16 字节：{BitConverter.ToString(result, 0, Math.Min(16, result.Length))}";
        }
    }
}
#endif
