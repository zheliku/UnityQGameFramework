using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示写入与读取容器。
    /// </summary>
    public sealed class WriteReadFileExample : MonoBehaviour
    {
        [SerializeField]
        private InputField contentInput;
        [SerializeField]
        private Button writeButton, readButton;
        [SerializeField]
        private Text resultText;
        private IFileSystem container;
        private string path;
        private const string FileName = "player/name.txt";
        private void Start()
        {
            path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem01", "example.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // path 是磁盘上的容器，FileName 是容器内的逻辑文件名。
            container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
            writeButton.onClick.AddListener(() =>
            {
                bool saved = container.WriteFile(FileName, Encoding.UTF8.GetBytes(contentInput.text));
                resultText.text = saved ? "已写入容器" : "写入失败";
            });
            readButton.onClick.AddListener(() =>
            {
                byte[] bytes = container.ReadFile(FileName);
                resultText.text = Encoding.UTF8.GetString(bytes);
            });
        }

        private void Update()
        {
            readButton.interactable = container != null && container.HasFile(FileName);
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null)
            {
                files.DestroyFileSystem(container, true);
            }
        }
    }
}
