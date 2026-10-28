using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示读取文件片段。
    /// </summary>
    public sealed class ReadFileSegments : MonoBehaviour
    {
        [SerializeField]
        private Slider offsetSlider, lengthSlider;
        [SerializeField]
        private Button readButton;
        [SerializeField]
        private Text rangeText, resultText;
        private IFileSystem container;
        private string path;
        private void Start()
        {
            path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem04", "example.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
            container.WriteFile("bytes.txt", Encoding.ASCII.GetBytes("0123456789ABCDEF"));
            offsetSlider.wholeNumbers = lengthSlider.wholeNumbers = true;
            readButton.onClick.AddListener(Read);
            Read();
        }

        private void Read()
        {
            int offset = (int)offsetSlider.value;
            int length = (int)lengthSlider.value;
            // 一次调用同时展示源文件 offset 与读取 length；返回数组只包含实际读到的字节。
            byte[] bytes = container.ReadFileSegment("bytes.txt", offset, length) ?? Array.Empty<byte>();
            resultText.text = string.Join(System.Environment.NewLine, $"Read {bytes.Length} bytes, source offset {offset}", Encoding.ASCII.GetString(bytes));
        }

        private void Update()
        {
            readButton.interactable = container != null;
            rangeText.text = "Source: 0123456789ABCDEF (16 bytes). Adjust offset and length; reads beyond the end return only the remaining bytes.";
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null)
            {
                files.DestroyFileSystem(container, true);
            }

            if (path != null && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
