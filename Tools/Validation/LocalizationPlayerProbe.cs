using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 在独立 Player 验证当前语言接口、离线资源及 uGUI / TMP 自动绑定。
/// </summary>
public sealed class LocalizationPlayerProbe : MonoBehaviour
{
    /// <summary>
    /// 等待示例启动，检查语言切换并写入验收结果后退出。
    /// </summary>
    private async void Start()
    {
        if (Application.isEditor)
        {
            return;
        }
        string report = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localization-player.json");
        var result = new Result { unity = Application.unityVersion };
        try
        {
            result.core = LocalizationPlayerChecks.Run();
            result.setting = SettingPersistenceValidation.Run();
            await UniTask.WaitUntil(() => GameEntry.GetComponent<LocalizationComponent>() != null
                && GameEntry.GetComponent<LocalizationComponent>().Language == Language.EnglishUnitedStates
                && !GameEntry.GetComponent<LocalizationComponent>().IsChangingLanguage).Timeout(TimeSpan.FromSeconds(40));
            LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
            LocalizeText title = FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(binding => binding.Key == "Game.Name");
            LocalizeTmpText tmp = FindFirstObjectByType<LocalizeTmpText>();
            LocalizeImage image = FindFirstObjectByType<LocalizeImage>();
            Require(title.GetComponent<Text>().text == "My Flying Game (US)", "English uGUI text");
            Require(tmp.GetComponent<TMP_Text>().text == "My Flying Game (US)", "English TMP text");
            TMP_FontAsset original = tmp.GetComponent<TMP_Text>().font;
            await localization.SetLanguageAsync(Language.ChineseSimplified, destroyCancellationToken);
            Require(title.GetComponent<Text>().text == "我的飞行游戏" && tmp.GetComponent<TMP_Text>().text == "我的飞行游戏", "Atomic uGUI and TMP refresh");
            Require(tmp.GetComponent<TMP_Text>().font == localization.GetAsset<TMP_FontAsset>("UI.TMPFont")
                && tmp.GetComponent<TMP_Text>().font != original, "Localized TMP font loaded through Resource");
            Require(image.GetComponent<Image>().sprite == localization.GetAsset<Sprite>("UI.Badge"), "Localized image loaded through Resource");
            Require(localization.GetString("Fallback.Hint") == "This line falls back to English.", "Configured fallback text");
            GameObject preview = title.transform.parent.gameObject;
            preview.SetActive(false);
            await localization.SetLanguageAsync(Language.EnglishUnitedStates, destroyCancellationToken);
            preview.SetActive(true);
            Require(tmp.GetComponent<TMP_Text>().text == "My Flying Game (US)" && tmp.GetComponent<TMP_Text>().font == original,
                "Pooled TMP binding reads the current language");
            result.bindingChecks = 7;
            result.passed = true;
        }
        catch (Exception exception)
        {
            result.error = exception.ToString();
        }
        File.WriteAllText(report, JsonUtility.ToJson(result, true));
        Application.Quit(result.passed ? 0 : 1);
    }

    /// <summary>
    /// 检查 Player 的实际运行结果。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="message">失败时的操作名称。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 保存 Player 验收结果。
    /// </summary>
    [Serializable]
    private sealed class Result
    {
        /// <summary>
        /// 是否通过所有检查。
        /// </summary>
        public bool passed;

        /// <summary>
        /// 实际 Unity 版本。
        /// </summary>
        public string unity;

        /// <summary>
        /// 已通过的 Core 检查。
        /// </summary>
        public string[] core;

        /// <summary>
        /// 已通过的真实文件持久化检查。
        /// </summary>
        public string[] setting;

        /// <summary>
        /// 已通过的界面与资源检查数量。
        /// </summary>
        public int bindingChecks;

        /// <summary>
        /// 验证失败的异常。
        /// </summary>
        public string error;
    }
}
