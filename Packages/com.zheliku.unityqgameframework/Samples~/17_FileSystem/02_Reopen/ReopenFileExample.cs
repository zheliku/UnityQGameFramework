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
    public sealed class ReopenFileExample : MonoBehaviour
    {
        [SerializeField] private Dropdown accessMode, queryMode;
        [SerializeField] private Button closeButton, reopenButton, writeButton, readButton, queryButton;
        [SerializeField] private Text stateText, resultText, managerText;
        private IFileSystem container;
        private string path;
        private bool ready;
        private int revision;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem02", "example.dat");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
                if (!container.WriteFile("level.txt", Encoding.UTF8.GetBytes("训练场"))) throw new IOException("初始写入失败。");
                accessMode.ClearOptions();
                accessMode.AddOptions(new List<string> { "Read：只读", "Write：只写", "ReadWrite：读写" });
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "GetAllFileSystems()", "GetAllFileSystems(List)" });
                closeButton.onClick.AddListener(() =>
                {
                    // false 只关闭句柄并移出管理器，磁盘容器和其中的数据仍然保留。
                    QGF.FileSystem.DestroyFileSystem(container, false);
                    container = null;
                    resultText.text = "已关闭。选择访问方式后重新打开，或查询管理器。";
                });
                reopenButton.onClick.AddListener(() =>
                {
                    // 同一路径只能打开一次；关闭后才能换权限重新加载。
                    var access = accessMode.value == 0 ? FileSystemAccess.Read : accessMode.value == 1 ? FileSystemAccess.Write : FileSystemAccess.ReadWrite;
                    container = QGF.FileSystem.LoadFileSystem(path, access);
                    resultText.text = $"已按 {AccessName(container.Access)} 重开；文件数 {container.FileCount}。";
                });
                writeButton.onClick.AddListener(() =>
                    resultText.text = container.WriteFile("level.txt", Encoding.UTF8.GetBytes("关卡 " + ++revision)) ? "写入成功" : "写入失败");
                readButton.onClick.AddListener(() => resultText.text = "读回：" + Encoding.UTF8.GetString(container.ReadFile("level.txt")));
                queryButton.onClick.AddListener(() =>
                {
                    var results = new List<IFileSystem>();
                    if (queryMode.value == 0) results.AddRange(QGF.FileSystem.GetAllFileSystems());
                    else QGF.FileSystem.GetAllFileSystems(results);
                    // Has/Get 查询管理器中已打开的句柄。
                    managerText.text = $"上次查询：管理器数量 {QGF.FileSystem.Count}，查询得到 {results.Count} 个\n"
                        + $"文件系统：{(QGF.FileSystem.HasFileSystem(path) ? "已登记" : "未登记")}\n"
                        + $"当前句柄：{(container != null && ReferenceEquals(container, QGF.FileSystem.GetFileSystem(path)) ? "匹配" : "不匹配")}";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            closeButton.interactable = container != null;
            reopenButton.interactable = accessMode.interactable = ready && container == null;
            writeButton.interactable = container != null && (container.Access & FileSystemAccess.Write) != 0;
            readButton.interactable = container != null && (container.Access & FileSystemAccess.Read) != 0;
            queryButton.interactable = ready;
            if (ready) stateText.text = (container == null ? "句柄已关闭" : $"已打开：{AccessName(container.Access)}")
                + $"\n当前打开容器：{QGF.FileSystem.Count}    磁盘文件：{(File.Exists(path) ? "仍存在" : "不存在")}\n{path}";
        }

        private static string AccessName(FileSystemAccess access)
        {
            if (access == FileSystemAccess.Read) return "只读";
            if (access == FileSystemAccess.Write) return "只写";
            return "读写";
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null) files.DestroyFileSystem(container, true);
            // 若在关闭状态退出，管理器里已没有句柄，直接删除本示例的磁盘文件。
            if (path != null && File.Exists(path)) File.Delete(path);
        }
    }
}
