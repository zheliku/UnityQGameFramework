using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SwitchLanguage : MonoBehaviour
    {
        [SerializeField] private Text outputText;
        [SerializeField] private Button action1Button;
        [SerializeField] private Button action2Button;
        [SerializeField] private Toggle reloadToggle;
        private bool initialized;

        [SerializeField] private TextAsset dictionary;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等框架组件完成 Start，再使用模块。
                await UniTask.NextFrame(cancellation);
                reloadToggle.SetIsOnWithoutNotify(true);
                action1Button.onClick.AddListener(() => Apply(Language.English));
                action2Button.onClick.AddListener(() => Apply(Language.ChineseSimplified));
                Apply(Language.ChineseSimplified);
                initialized = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }
        private void Apply(Language language)
        {
            // Language 只记录当前语言，不会自动重载字典或刷新界面。
            QGF.Localization.Language = language;
            if (reloadToggle.isOn)
            {
                // 清空旧键后按当前语言解析 TOML，避免上一种语言的内容残留。
                QGF.Localization.RemoveAllRawStrings();
                QGF.Localization.ParseData(dictionary.text);
            }
        }
        private void LateUpdate()
        {
            if (!initialized) return;
            // 界面主动读取字典；取消重载后可以对比语言设置与实际文本。
            outputText.text = $"当前语言：{QGF.Localization.Language}\n文本：{QGF.Localization.GetString("Game.Name")}\n字典键数：{QGF.Localization.DictionaryCount}";
            action1Button.interactable = initialized;
            action2Button.interactable = initialized;
        }
    }
}
