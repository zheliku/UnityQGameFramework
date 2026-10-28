using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using IoPath = System.IO.Path;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示文本与路径。
    /// </summary>
    public sealed class TextAndPaths : MonoBehaviour
    {
        [SerializeField]
        private InputField nameInput;
        [SerializeField]
        private InputField countInput;
        [SerializeField]
        private InputField pathInput;
        [SerializeField]
        private Button formatButton;
        [SerializeField]
        private Button directoryButton;
        [SerializeField]
        private Button cleanupButton;
        [SerializeField]
        private Text resultText;
        [SerializeField]
        private Text directoryText;
        private string directory;
        private string ownedFile;
        private void Start()
        {
            formatButton.onClick.AddListener(Format);
            directoryButton.onClick.AddListener(CheckDirectories);
            cleanupButton.onClick.AddListener(Cleanup);
            cleanupButton.interactable = false;
            directoryText.text = "Inspect the folder to compare empty folders with folders containing files.";
            Format();
        }

        private void Format()
        {
            if (!int.TryParse(countInput.text, out int count))
            {
                resultText.text = "Enter an integer quantity.";
                return;
            }

            // Text.Format 使用 Base 安装的 Helper；这里对照一、二、三个参数的重载。
            resultText.text = Utility.Text.Format("Player: {0}", nameInput.text) + "\n" + Utility.Text.Format("{0} gained {1} energy", nameInput.text, count) + "\n" + Utility.Text.Format("{0}：{1} × {2} = ", nameInput.text, count, 10) + (long)count * 10 + "\n" // Regular 只统一斜杠；Remote 为不含协议的路径添加 file:///。
            + "Normalized separators: " + Utility.Path.GetRegularPath(pathInput.text) + "\n" + "Server format: " + Utility.Path.GetRemotePath(pathInput.text);
        }

        private void CheckDirectories()
        {
            try
            {
                // 路径输入仅用于字符串演示。文件操作始终使用本示例独占的临时目录。
                if (directory == null)
                {
                    directory = IoPath.Combine(Application.temporaryCachePath, "QgfPath-" + Guid.NewGuid().ToString("N"));
                    ownedFile = IoPath.Combine(directory, "WithFile", "keep.txt");
                }

                Directory.CreateDirectory(IoPath.Combine(directory, "Empty"));
                Directory.CreateDirectory(IoPath.GetDirectoryName(ownedFile));
                File.WriteAllText(ownedFile, "QGF path example");
                // 递归清理空目录。含文件的目录会保留，因此根目录返回 false。
                bool removed = Utility.Path.RemoveEmptyDirectory(directory);
                directoryText.text = $"{directory}\nRoot folder: {(removed ? "Removed" : "Contains files; retained")}\n" + $"Empty folder: {(Directory.Exists(IoPath.Combine(directory, "Empty")) ? "Still exists" : "Removed")}\n" + $"Nonempty folder: {(File.Exists(ownedFile) ? "Retained" : "Deleted")}";
                cleanupButton.interactable = true;
            }
            catch (IOException)
            {
                directoryText.text = "Cannot read or write the temporary folder. Check folder permissions.";
            }
            catch (UnauthorizedAccessException)
            {
                directoryText.text = "Permission denied when reading or writing the temporary folder.";
            }
        }

        private void Cleanup()
        {
            try
            {
                if (directory == null)
                {
                    return;
                }

                // 只删除本示例写入的文件，再让 QGF 清理剩余空目录。
                if (File.Exists(ownedFile))
                {
                    File.Delete(ownedFile);
                }

                bool removed = Utility.Path.RemoveEmptyDirectory(directory);
                if (directoryText != null)
                {
                    directoryText.text = $"Root folder after deleting keep.txt: {(removed ? "Removed" : "Contains files")}\n{directory}";
                }

                if (cleanupButton != null)
                {
                    cleanupButton.interactable = Directory.Exists(directory);
                }

                if (removed)
                {
                    directory = null;
                }
            }
            catch (IOException)
            {
                if (directoryText != null)
                {
                    directoryText.text = "The temporary file could not be deleted.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (directoryText != null)
                {
                    directoryText.text = "Permission denied when deleting the temporary file.";
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
