using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class WriteReadFileExample : MonoBehaviour
    {
        [SerializeField] private InputField contentInput;
        [SerializeField] private Dropdown writeMode, readMode;
        [SerializeField] private Button writeButton, readButton;
        [SerializeField] private Text stateText, resultText;
        private IFileSystem container;
        private string path, sourcePath;
        private const string Name = "player/name.txt";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem01", "example.dat");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                sourcePath = Path.Combine(Path.GetDirectoryName(path), "input.txt");
                // 路径指向物理容器，Name 是容器内的逻辑键。
                container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
                contentInput.characterLimit = 64;
                writeMode.ClearOptions();
                writeMode.AddOptions(new List<string> { "WriteFile(name, bytes)", "WriteFile(name, bytes, start)",
                    "WriteFile(name, bytes, start, length)", "WriteFile(name, stream)", "WriteFile(name, filePath)" });
                readMode.ClearOptions();
                readMode.AddOptions(new List<string> { "ReadFile(name)", "ReadFile(name, buffer)",
                    "ReadFile(name, buffer, start)", "ReadFile(name, buffer, start, length)", "ReadFile(name, stream)" });
                writeButton.onClick.AddListener(Write);
                readButton.onClick.AddListener(Read);
                Write();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Write()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(contentInput.text);
            byte[] padded = Encoding.UTF8.GetBytes("_" + contentInput.text + "_");
            bool written;
            // startIndex 是输入缓冲区的位置；带 length 的重载只写选中的字节。
            switch (writeMode.value)
            {
                case 0: written = container.WriteFile(Name, bytes); break;
                case 1: written = container.WriteFile(Name, Encoding.UTF8.GetBytes("_" + contentInput.text), 1); break;
                case 2: written = container.WriteFile(Name, padded, 1, bytes.Length); break;
                case 3:
                    using (var stream = new MemoryStream(bytes)) written = container.WriteFile(Name, stream);
                    break;
                default:
                    File.WriteAllBytes(sourcePath, bytes);
                    written = container.WriteFile(Name, sourcePath);
                    break;
            }
            resultText.text = written ? $"已写入 {bytes.Length} 字节；点击读取查看内容。" : "写入失败，旧数据未更新。";
        }

        private void Read()
        {
            var buffer = new byte[512];
            int count, start = 0;
            // 只解码返回 count 范围内的有效字节。
            switch (readMode.value)
            {
                case 0: buffer = container.ReadFile(Name); count = buffer.Length; break;
                case 1: count = container.ReadFile(Name, buffer); break;
                case 2: start = 2; count = container.ReadFile(Name, buffer, start); break;
                case 3: start = 2; count = container.ReadFile(Name, buffer, start, buffer.Length - start); break;
                default:
                    using (var stream = new MemoryStream())
                    {
                        count = container.ReadFile(Name, stream);
                        buffer = stream.ToArray();
                    }
                    break;
            }
            resultText.text = $"读取 {count} 字节，写入目标位置 {start}\n内容：{Encoding.UTF8.GetString(buffer, start, count)}";
        }

        private void Update()
        {
            writeButton.interactable = container != null;
            readButton.interactable = container != null && container.HasFile(Name);
            if (container != null)
                stateText.text = $"{Name}：{container.GetFileInfo(Name).Length} 字节\n逻辑文件 {container.FileCount} / {container.MaxFileCount}";
        }

        private void OnDestroy()
        {
            // 只清理本示例文件；框架先退出时句柄已关闭，再删除留下的物理容器。
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null) files.DestroyFileSystem(container, true);
            if (path != null && File.Exists(path)) File.Delete(path);
            if (sourcePath != null && File.Exists(sourcePath)) File.Delete(sourcePath);
        }
    }
}
