#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ReadBinaryResource : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, readButton;
        [SerializeField] private Text stateText, pathText, bytesText;

        private ResourceFixtureManifest fixture;
        private byte[] result;
        private bool initialized, loading;

        private void Start()
        {
            fixture = ResourceExamplePaths.Fixture;
            stateText.text = fixture == null ? ResourceExamplePaths.Notice : "初始化资源包，再读取 Archive 中的一段字节。";
            initializeButton.onClick.AddListener(Initialize);
            readButton.onClick.AddListener(ReadFileSegment);
        }

        private void Initialize()
        {
            loading = true;
            // Package 初始化读取资源清单，之后才能按资源名查询二进制文件。
            QGF.Resource.InitResources(() =>
            {
                if (this == null) return;
                initialized = true;
                loading = false;
                stateText.text = "资源包已初始化。";
            });
        }

        private void ReadFileSegment()
        {
            const int offset = 8;
            const int length = 16;
            string file = fixture.FileSystemAssetName;
            // offset 是源文件位置；该重载只返回指定区间的新 byte[]。
            result = QGF.Resource.LoadBinarySegmentFromFileSystem(file, offset, length);
            stateText.text = $"从文件偏移 {offset} 读取 {result.Length} 字节。";
        }

        private void Update()
        {
            initializeButton.interactable = fixture != null && !initialized && !loading;
            readButton.interactable = initialized && !loading;
            if (!initialized) return;

            string file = fixture.FileSystemAssetName;
            bool found = QGF.Resource.GetBinaryPath(file, out bool readOnly, out bool inFileSystem,
                out string relative, out string fileName);
            pathText.text = $"资源长度：{QGF.Resource.GetBinaryLength(file)} 字节\n"
                + $"位置：{(readOnly ? "只读" : "读写")} · {(inFileSystem ? "文件系统" : "散文件")}\n"
                + $"路径：{(found ? relative + "/" + fileName : "未找到")}";
            bytesText.text = result == null
                ? "尚未读取。"
                : $"已读取 {result.Length} 字节\n前 16 字节：{BitConverter.ToString(result, 0, Math.Min(16, result.Length))}";
        }
    }
}
#endif
