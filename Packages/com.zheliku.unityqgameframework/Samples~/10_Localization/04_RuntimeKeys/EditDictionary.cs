using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class EditDictionary : MonoBehaviour
    {
        [SerializeField] private InputField keyInput, valueInput, argumentInput;
        [SerializeField] private Button addButton, removeButton, clearButton;
        [SerializeField] private Text stateText, rawText, formattedText;
        private ILocalizationManager manager;
        private string lastAction = "添加文本后，对照原始值与格式化结果。";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // AddRawString 由 Core 接口提供；这里取得的仍是组件使用的同一个模块。
                manager = FrameworkEntry.GetModule<ILocalizationManager>();
                addButton.onClick.AddListener(() =>
                {
                    bool added = manager.AddRawString(keyInput.text, valueInput.text);
                    lastAction = added ? "已添加：" + keyInput.text : "键已存在，先移除再添加。";
                });
                removeButton.onClick.AddListener(() =>
                {
                    QGF.Localization.RemoveRawString(keyInput.text);
                    lastAction = "已移除：" + keyInput.text;
                });
                // 所有修改只影响内存；这里不会把文本写回 TOML。
                clearButton.onClick.AddListener(() =>
                {
                    QGF.Localization.RemoveAllRawStrings();
                    lastAction = "已清空运行时字典。";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (manager == null) return;
            bool valid = !string.IsNullOrEmpty(keyInput.text);
            bool exists = valid && QGF.Localization.HasRawString(keyInput.text);
            addButton.interactable = valid;
            removeButton.interactable = exists;
            clearButton.interactable = QGF.Localization.DictionaryCount > 0;
            stateText.text = $"字典 {QGF.Localization.DictionaryCount} 项\n{lastAction}";
            rawText.text = exists ? "原始文本：" + QGF.Localization.GetRawString(keyInput.text) : "键尚未添加。";
            string result = exists ? QGF.Localization.GetString(keyInput.text, argumentInput.text) : string.Empty;
            formattedText.text = result.StartsWith("<Error>", StringComparison.Ordinal)
                ? "格式不匹配，请检查 {0} 占位符。" : result;
        }
    }
}
