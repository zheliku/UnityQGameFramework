using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ExportLogicalFile : MonoBehaviour
    {
        [SerializeField] private InputField contentInput;
        [SerializeField] private Button exportButton, deleteButton, importButton;
        [SerializeField] private Text pathsText, stateText, resultText;
        private IFileSystem container;
        private string path, exported;
        private const string Name = "player.txt";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                string directory = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem05");
                Directory.CreateDirectory(directory);
                path = Path.Combine(directory, "example.dat");
                exported = Path.Combine(directory, "exported.txt");
                // 容器文件和导出文件均写入示例专用目录。
                container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
                pathsText.text = $"容器：{container.FullPath}\n导出文件：{exported}";
                exportButton.onClick.AddListener(() =>
                {
                    if (!container.WriteFile(Name, Encoding.UTF8.GetBytes(contentInput.text))) { resultText.text = "写入失败"; return; }
                    // SaveAsFile 复制到普通磁盘文件；不会移除容器里的原条目，会覆盖同路径旧文件。
                    resultText.text = container.SaveAsFile(Name, exported) ? "已导出：" + File.ReadAllText(exported) : "导出失败";
                });
                deleteButton.onClick.AddListener(() =>
                    resultText.text = container.DeleteFile(Name) ? "已移除逻辑文件；导出文件仍在磁盘上。" : "逻辑文件不存在");
                importButton.onClick.AddListener(() =>
                {
                    // filePath 重载读取磁盘文件内容，写入容器。
                    resultText.text = container.WriteFile(Name, exported)
                        ? "已导入：" + Encoding.UTF8.GetString(container.ReadFile(Name)) : "导入失败";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            exportButton.interactable = container != null;
            deleteButton.interactable = container != null && container.HasFile(Name);
            importButton.interactable = container != null && File.Exists(exported);
            if (container != null) stateText.text = $"逻辑文件：{(container.HasFile(Name) ? "存在" : "不存在")}\n"
                + $"导出文件：{(File.Exists(exported) ? "存在" : "不存在")}";
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null) files.DestroyFileSystem(container, true);
            if (path != null && File.Exists(path)) File.Delete(path);
            if (exported != null && File.Exists(exported)) File.Delete(exported);
        }
    }
}
