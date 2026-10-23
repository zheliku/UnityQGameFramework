using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Config;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class RuntimeConfigValues : MonoBehaviour
    {
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Text resultText;
        [SerializeField] private Text valuesText;
        [SerializeField] private Text countText;
        [SerializeField] private Dropdown addMode;
        [SerializeField] private InputField valueInput;
        private bool viewReady;
        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;
                addMode.ClearOptions();
                addMode.AddOptions(new List<string> { "显式提供四种值", "Core：字符串转换" });
                valueInput.interactable = false;
                addMode.onValueChanged.AddListener(mode => valueInput.interactable = mode == 1);

                addButton.onClick.AddListener(() =>
                {
                    // AddConfig 直接接收四种表示，供自定义配置适配器使用；日常配置用 TOML。
                    // Core 字符串入口分别尝试转换；转换不出的类型得到 false 或 0。
                    bool added = addMode.value == 0
                        ? QGF.Config.AddConfig("Runtime.Mode", true, 7, 0.75f, "训练")
                        : FrameworkEntry.GetModule<IConfigManager>().AddConfig("Runtime.Mode", valueInput.text);
                    resultText.text = added ? "已添加 Runtime.Mode" : "同名项已存在，旧值保留";
                });
                removeButton.onClick.AddListener(() =>
                {
                    // 只移除本次要重读的键，避免重复加载同名项。
                    bool removed = QGF.Config.RemoveConfig("Runtime.Mode");
                    resultText.text = removed ? "已移除 Runtime.Mode" : "没有这个配置项";
                });
                clearButton.onClick.AddListener(() =>
                {
                    QGF.Config.RemoveAllConfigs();
                    resultText.text = "已清空";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!viewReady) return;
            // 必需键先检查是否存在，再读取对应类型。
            valuesText.text = QGF.Config.HasConfig("Runtime.Mode")
                ? $"Runtime.Mode\nbool = {QGF.Config.GetBool("Runtime.Mode")}\nint = {QGF.Config.GetInt("Runtime.Mode")}\n"
                    + $"float = {QGF.Config.GetFloat("Runtime.Mode")}\nstring = {QGF.Config.GetString("Runtime.Mode")}"
                : "Runtime.Mode 尚未添加";
            // Count 返回当前配置数量。
            countText.text = $"当前配置：{QGF.Config.Count} 项";
        }
    }
}
