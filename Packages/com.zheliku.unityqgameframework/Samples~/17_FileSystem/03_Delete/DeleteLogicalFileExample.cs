using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Runtime;
using FileInfo = UnityQGameFramework.Core.FileSystem.FileInfo;

namespace UnityQGameFramework.Examples
{
    public sealed class DeleteLogicalFileExample : MonoBehaviour
    {
        [SerializeField] private InputField nameInput, newNameInput;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Button inspectButton, renameButton, deleteButton, addButton, resetButton;
        [SerializeField] private Text entriesText, infoText, resultText;
        private IFileSystem container;
        private string path;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                path = Path.Combine(Application.persistentDataPath, "QgfExamples", "FileSystem03", "example.dat");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // 故意只允许两个逻辑文件，观察写第三份时返回 false，以及删除后释放的名额。
                container = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 2, 16);
                nameInput.characterLimit = newNameInput.characterLimit = 64;
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "GetAllFileInfos()", "GetAllFileInfos(List)" });
                inspectButton.onClick.AddListener(Inspect);
                renameButton.onClick.AddListener(() =>
                {
                    string name = nameInput.text.Trim(), renamed = newNameInput.text.Trim();
                    bool changed = container.RenameFile(name, renamed);
                    resultText.text = changed ? "重命名成功，内容和字节位置保持不变。" : "重命名失败：旧键不存在或新键已被占用。";
                    if (changed) nameInput.SetTextWithoutNotify(renamed);
                    Inspect();
                });
                deleteButton.onClick.AddListener(() =>
                {
                    // 只移除选中的逻辑文件，其他条目与物理容器不受影响。
                    resultText.text = container.DeleteFile(nameInput.text.Trim()) ? "已删除所选条目" : "键不存在，未删除任何条目";
                    Inspect();
                });
                addButton.onClick.AddListener(() =>
                {
                    resultText.text = container.WriteFile("third.txt", Encoding.UTF8.GetBytes("第三份记录")) ? "已写入 third.txt" : "写入被拒绝：逻辑文件数量达到上限。";
                    Inspect();
                });
                resetButton.onClick.AddListener(ResetEntries);
                ResetEntries();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void ResetEntries()
        {
            foreach (var info in container.GetAllFileInfos()) container.DeleteFile(info.Name);
            bool first = container.WriteFile("saved.txt", Encoding.UTF8.GetBytes("保留记录"));
            bool second = container.WriteFile("temp.txt", Encoding.UTF8.GetBytes("临时记录"));
            resultText.text = first && second ? "已恢复两份初始记录" : "恢复失败";
            nameInput.SetTextWithoutNotify("temp.txt");
            Inspect();
        }

        private void Inspect()
        {
            var entries = new List<FileInfo>();
            if (queryMode.value == 0) entries.AddRange(container.GetAllFileInfos());
            else container.GetAllFileInfos(entries);
            entriesText.text = $"逻辑文件 {container.FileCount} / {container.MaxFileCount}";
            foreach (var entry in entries) entriesText.text += $"\n{entry.Name}：{entry.Length} 字节，容器偏移 {entry.Offset}";
            string name = nameInput.text.Trim();
            // 不存在的键返回无效 FileInfo；Offset 是容器中的字节位置，不是逻辑文件内偏移。
            var info = container.GetFileInfo(name);
            infoText.text = $"逻辑文件：{(container.HasFile(name) ? "存在" : "不存在")}    "
                + $"文件信息：{(info.IsValid ? "有效" : "无效")}\n"
                + (info.IsValid ? $"{info.Name} / {info.Length} 字节 / 偏移 {info.Offset}" : "找不到所选条目");
        }

        private void Update()
        {
            bool ready = container != null;
            inspectButton.interactable = deleteButton.interactable = ready && !string.IsNullOrWhiteSpace(nameInput.text);
            renameButton.interactable = inspectButton.interactable && !string.IsNullOrWhiteSpace(newNameInput.text);
            addButton.interactable = resetButton.interactable = ready;
        }

        private void OnDestroy()
        {
            var files = GameEntry.GetComponent<FileSystemComponent>();
            if (files != null && container != null) files.DestroyFileSystem(container, true);
            if (path != null && File.Exists(path)) File.Delete(path);
        }
    }
}
