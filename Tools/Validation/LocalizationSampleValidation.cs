using TMPro;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core;

/// <summary>
/// 检查发布示例通过组件展示语言、资源与池化绑定。
/// </summary>
public static class LocalizationSampleValidation
{
    /// <summary>
    /// 检查 StarForce 按统一语言身份加载四种字典并进入菜单。
    /// </summary>
    /// <returns>已验证的语言数量。</returns>
    public static async Task<object> RunStarForce()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an idle isolated project.");
        }
        Type menuType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("StarForce.MenuForm")).First(type => type != null);
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
        Language[] languages = { Language.English, Language.ChineseSimplified, Language.ChineseTraditional, Language.Korean };
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            foreach (Language language in languages)
            {
                EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
                BaseComponent component = UnityEngine.Object.FindFirstObjectByType<BaseComponent>();
                component.EditorLanguage = language;
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorUtility.SetDirty(component);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorApplication.isPlaying = true;
                await Until(() => EditorApplication.isPlaying && GameEntry.GetComponent<LocalizationComponent>() != null
                    && GameEntry.GetComponent<LocalizationComponent>().DictionaryCount > 0
                    && UnityEngine.Object.FindFirstObjectByType(menuType) != null);
                LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
                Require(localization.Language == language && localization.DictionaryCount > 0,
                    "StarForce starts with dictionary: " + language.Code + ", actual=" + localization.Language.Code + ", count=" + localization.DictionaryCount);
                EditorApplication.isPlaying = false;
                await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
                await Task.Delay(100);
            }
            return new
            {
                passed = true,
                unity = Application.unityVersion,
                languages = languages.Length
            };
        }
        finally
        {
            EditorApplication.isPlaying = false;
            await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
        }
    }

    /// <summary>
    /// 在隔离工程运行实际示例并操作原有按钮。
    /// </summary>
    /// <returns>示例检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an idle isolated project.");
        }
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/01_Modules/10_Localization/02_SwitchLanguage/SwitchLanguage.unity");
            EditorApplication.isPlaying = true;
            await Until(() => EditorApplication.isPlaying && GameEntry.GetComponent<LocalizationComponent>() != null
                && GameEntry.GetComponent<LocalizationComponent>().Language == Language.EnglishUnitedStates && !GameEntry.GetComponent<LocalizationComponent>().IsChangingLanguage);
            LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
            LocalizeText title = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(text => text.Key == "Game.Name");
            LocalizeText hint = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(text => text.Key == "Fallback.Hint");
            LocalizeText reward = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(text => text.Key == "Reward.Coins");
            LocalizeImage badge = UnityEngine.Object.FindFirstObjectByType<LocalizeImage>();
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            Button english = buttons.Single(button => button.name == "Button_Action1");
            Button chinese = buttons.Single(button => button.name == "Button_Action2");
            Button addCoin = buttons.Single(button => button.name == "Button_AddCoin");
            Toggle preview = UnityEngine.Object.FindFirstObjectByType<Toggle>();
            TMP_FontAsset original = title.GetComponent<TMP_Text>().font;
            Sprite originalBadge = badge.GetComponent<Image>().sprite;
            Require(title.GetComponent<TMP_Text>().text == "My Flying Game (US)", "Declarative text binding displays the selected region");
            addCoin.onClick.Invoke();
            Require(reward.GetComponent<TMP_Text>().text == "Pilot received 1 coin", "Dynamic arguments use the English singular form");
            addCoin.onClick.Invoke();
            Require(reward.GetComponent<TMP_Text>().text == "Pilot received 2 coins", "Dynamic arguments use the English plural form");
            chinese.onClick.Invoke();
            await Until(() => localization.Language == Language.ChineseSimplified && !localization.IsChangingLanguage);
            Require(title.GetComponent<TMP_Text>().text == "我的飞行游戏" && reward.GetComponent<TMP_Text>().text == "Pilot 获得 2 金币", "Language event updates every text binding without business refresh code");
            Require(title.GetComponent<TMP_Text>().font == localization.GetAsset<TMP_FontAsset>("UI.Font") && badge.GetComponent<Image>().sprite != originalBadge,
                "The default Resource package provides the Chinese font and badge");
            Require(hint.GetComponent<TMP_Text>().text == "This line falls back to English.", "Missing Chinese key uses the configured English fallback");
            preview.isOn = false;
            Require(!title.isActiveAndEnabled && title.GetComponent<TMP_Text>().font == original && badge.GetComponent<Image>().sprite == null,
                "Disabling the preview restores its original resource references");
            english.onClick.Invoke();
            await Until(() => localization.Language == Language.EnglishUnitedStates && !localization.IsChangingLanguage);
            preview.isOn = true;
            Require(title.GetComponent<TMP_Text>().text == "My Flying Game (US)" && reward.GetComponent<TMP_Text>().text == "Pilot received 2 coins"
                && badge.GetComponent<Image>().sprite == originalBadge, "Re-enabled bindings immediately read the current language and retain business arguments");
            return new
            {
                passed = true,
                unity = Application.unityVersion,
                checks = 8,
                automaticText = true,
                localizedFontAndSprite = true,
                pooledBindings = true
            };
        }
        finally
        {
            EditorApplication.isPlaying = false;
            await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
        }
    }

    /// <summary>
    /// 等待当前场景完成语言准备。
    /// </summary>
    /// <param name="condition">完成条件。</param>
    /// <returns>等待任务。</returns>
    private static async Task Until(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(25);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Localization example timed out.");
            }
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// 检查示例的可见结果。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="message">检查名称。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
