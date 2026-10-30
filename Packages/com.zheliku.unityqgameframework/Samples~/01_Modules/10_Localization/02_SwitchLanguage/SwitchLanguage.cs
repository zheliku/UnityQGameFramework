using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 切换语言；文本、字体和图片由各自的本地化组件自动刷新。
    /// </summary>
    public sealed class SwitchLanguage : MonoBehaviour
    {
        [SerializeField] private Button action1Button, action2Button, rewardButton;
        [SerializeField] private Toggle previewToggle;
        [SerializeField] private GameObject preview;
        [SerializeField] private LocalizeText rewardText;
        private int coins;

        private void Start()
        {
            // 语言和资源地址已保存在场景的 LocalizationComponent Inspector 中。
            action1Button.onClick.AddListener(() => ChangeLanguage(Language.EnglishUnitedStates));
            action2Button.onClick.AddListener(() => ChangeLanguage(Language.ChineseSimplified));
            previewToggle.onValueChanged.AddListener(preview.SetActive);
            rewardButton.onClick.AddListener(() =>
            {
                // 业务只更新动态参数；文字顺序与复数规则由翻译决定。
                coins++;
                rewardText.SetFormatValue("Count", coins);
            });
            rewardText.SetFormatValues(new Dictionary<string, object> { { "Player", "Pilot" }, { "Count", coins } });
            ChangeLanguage(Language.EnglishUnitedStates);
        }

        private async void ChangeLanguage(Language language)
        {
            try
            {
                // Resource 加载字典、字体和图片；成功后发出事件，绑定组件统一刷新。
                await QGF.Localization.SetLanguageAsync(language, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 后一次切换或退出场景取消了本次请求，旧语言继续可用。
            }
            catch (Exception exception)
            {
                Log.Warning("Language change failed: {0}", exception.Message);
            }
        }
    }
}
