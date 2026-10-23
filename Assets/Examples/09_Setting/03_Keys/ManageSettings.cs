using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ManageSettings : MonoBehaviour
    {
        [SerializeField] private InputField keyInput, valueInput;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Button writeButton, removeButton, clearButton, saveButton;
        [SerializeField] private Text stateText, keysText;
        private readonly List<string> names = new List<string>();
        private string lastAction = "写入后点击保存。";
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // Set 可以覆盖已有值；这一点与 Config.AddConfig 的重复键规则不同。
                writeButton.onClick.AddListener(() =>
                {
                    QGF.Setting.SetString(keyInput.text, valueInput.text);
                    lastAction = "已写入：" + keyInput.text;
                });
                removeButton.onClick.AddListener(() =>
                {
                    QGF.Setting.RemoveSetting(keyInput.text);
                    lastAction = "已移除：" + keyInput.text;
                });
                // 本场景使用独立文件，RemoveAllSettings 清空这份设置的全部键。
                clearButton.onClick.AddListener(() =>
                {
                    QGF.Setting.RemoveAllSettings();
                    lastAction = "已清空内存中的设置。";
                });
                // Save 立即写盘。框架正常关闭也会保存，异常退出则未必执行关闭流程。
                saveButton.onClick.AddListener(() =>
                {
                    QGF.Setting.Save();
                    lastAction = "已保存当前设置。";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            bool valid = !string.IsNullOrEmpty(keyInput.text);
            bool exists = valid && QGF.Setting.HasSetting(keyInput.text);
            writeButton.interactable = valid;
            removeButton.interactable = exists;
            clearButton.interactable = QGF.Setting.Count > 0;
            // List 重载先清空容器；数组重载返回新的名称数组。
            if (queryMode.value == 0) QGF.Setting.GetAllSettingNames(names);
            else
            {
                names.Clear();
                names.AddRange(QGF.Setting.GetAllSettingNames());
            }
            stateText.text = $"设置 {QGF.Setting.Count} 项    所选键 {(exists ? "存在" : "不存在")}\n{lastAction}";
            keysText.text = names.Count == 0 ? "当前没有设置。" : string.Empty;
            foreach (string name in names)
                keysText.text += $"{name} = {QGF.Setting.GetString(name)}\n";
        }
    }
}
