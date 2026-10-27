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
            directoryText.text = "点击检查目录，观察空目录与含文件目录的区别。";
            Format();
        }

        private void Format()
        {
            if (!int.TryParse(countInput.text, out int count))
            {
                resultText.text = "数量请输入整数。";
                return;
            }

            // Text.Format 使用 Base 安装的 Helper；这里对照一、二、三个参数的重载。
            resultText.text = Utility.Text.Format("玩家：{0}", nameInput.text) + "\n" + Utility.Text.Format("{0} 获得 {1} 个能量", nameInput.text, count) + "\n" + Utility.Text.Format("{0}：{1} × {2} = ", nameInput.text, count, 10) + (long)count * 10 + "\n" // Regular 只统一斜杠；Remote 为不含协议的路径添加 file:///。
            + "统一分隔符：" + Utility.Path.GetRegularPath(pathInput.text) + "\n" + "远程格式：" + Utility.Path.GetRemotePath(pathInput.text);
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
                directoryText.text = $"{directory}\n根目录：{(removed ? "已移除" : "含文件，保留")}\n" + $"空目录：{(Directory.Exists(IoPath.Combine(directory, "Empty")) ? "仍存在" : "已移除")}\n" + $"含文件目录：{(File.Exists(ownedFile) ? "已保留" : "已删除")}";
                cleanupButton.interactable = true;
            }
            catch (IOException)
            {
                directoryText.text = "临时目录读写失败，请检查目录权限。";
            }
            catch (UnauthorizedAccessException)
            {
                directoryText.text = "没有临时目录的读写权限。";
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
                    directoryText.text = $"删除 keep.txt 后，根目录：{(removed ? "已移除" : "仍有内容")}\n{directory}";
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
                    directoryText.text = "临时文件暂时无法清理。";
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (directoryText != null)
                {
                    directoryText.text = "没有临时文件的清理权限。";
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
