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
    public sealed class ReadFileSegments : MonoBehaviour
    {
        [SerializeField] private Dropdown readMode;
        [SerializeField] private Slider offsetSlider, lengthSlider;
        [SerializeField] private Toggle missingToggle;
        [SerializeField] private Button readButton;
        [SerializeField] private Text rangeText, resultText;
        private IFileSystem container;
        private string path;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem04", "example.dat");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // 每次运行使用独立容器文件，不接触游戏正式存档。
                container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
                // 使用 ASCII 让字符与字节一一对应；实际 UTF-8 文本不能任意按字节切开字符。
                if (!container.WriteFile("bytes.txt", Encoding.ASCII.GetBytes("0123456789ABCDEF"))) throw new IOException("初始写入失败。");
                readMode.ClearOptions();
                readMode.AddOptions(new List<string>
                {
                    "length → bytes", "offset, length → bytes", "buffer", "buffer, length",
                    "buffer, start, length", "offset, buffer", "offset, buffer, length",
                    "offset, buffer, start, length", "stream, length", "offset, stream, length"
                });
                offsetSlider.wholeNumbers = lengthSlider.wholeNumbers = true;
                readButton.onClick.AddListener(Read);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Read()
        {
            string name = missingToggle.isOn ? "missing.txt" : "bytes.txt";
            int offset = (int)offsetSlider.value, length = (int)lengthSlider.value;
            byte[] buffer = Encoding.ASCII.GetBytes("............");
            int count, start = 0;
            // offset 是源文件位置；start 是目标缓冲区位置。没有 offset 的重载从源头读取。
            switch (readMode.value)
            {
                case 0: buffer = container.ReadFileSegment(name, length) ?? Array.Empty<byte>(); count = buffer.Length; offset = 0; break;
                case 1: buffer = container.ReadFileSegment(name, offset, length) ?? Array.Empty<byte>(); count = buffer.Length; break;
                case 2: count = container.ReadFileSegment(name, buffer); offset = 0; break;
                case 3: count = container.ReadFileSegment(name, buffer, Math.Min(length, buffer.Length)); offset = 0; break;
                case 4: start = 2; count = container.ReadFileSegment(name, buffer, start, Math.Min(length, buffer.Length - start)); offset = 0; break;
                case 5: count = container.ReadFileSegment(name, offset, buffer); break;
                case 6: count = container.ReadFileSegment(name, offset, buffer, Math.Min(length, buffer.Length)); break;
                case 7: start = 2; count = container.ReadFileSegment(name, offset, buffer, start, Math.Min(length, buffer.Length - start)); break;
                default:
                    using (var stream = new MemoryStream())
                    {
                        if (readMode.value == 8) { offset = 0; count = container.ReadFileSegment(name, stream, length); }
                        else count = container.ReadFileSegment(name, offset, stream, length);
                        buffer = stream.ToArray();
                    }
                    break;
            }
            resultText.text = $"实际读取 {count} 字节；源位置 {offset}，目标位置 {start}\n"
                + $"内容：{Encoding.ASCII.GetString(buffer, start, count)}\n目标缓冲区：{Encoding.ASCII.GetString(buffer)}";
        }

        private void Update()
        {
            readButton.interactable = container != null;
            offsetSlider.interactable = readMode.value == 1 || (readMode.value >= 5 && readMode.value <= 7) || readMode.value == 9;
            lengthSlider.interactable = readMode.value != 2 && readMode.value != 5;
            rangeText.text = $"源：0123456789ABCDEF（16 字节）\n源偏移 {offsetSlider.value:F0}，请求长度 {lengthSlider.value:F0}\n"
                + "目标 buffer 长 12；带 start 的重载从位置 2 写入。超出文件尾部只返回剩余字节。";
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null) files.DestroyFileSystem(container, true);
            if (path != null && File.Exists(path)) File.Delete(path);
        }
    }
}
