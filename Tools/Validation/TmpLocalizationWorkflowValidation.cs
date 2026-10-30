using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 验证 TMP 迁移后的资源引用、StarForce 热切换和命名参数。
/// </summary>
public static class TmpLocalizationWorkflowValidation
{
    private static readonly List<string> s_Checks = new List<string>();

    /// <summary>
    /// 在隔离工程执行实际示例操作，保存结果后退出编辑器。
    /// </summary>
    public static async void RunBatch()
    {
        var report = new Report { unity = Application.unityVersion };
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
        try
        {
            if (!Application.dataPath.Contains("Artifacts~"))
            {
                throw new InvalidOperationException("Use an isolated validation project.");
            }
            s_Checks.Clear();
            File.WriteAllText("Artifacts~/tmp-localization-stage.txt", "Prerequisites");
            if (TMP_Settings.LoadDefaultSettings() == null)
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                TMP_Settings.LoadDefaultSettings();
            }
            File.WriteAllText("Artifacts~/tmp-localization-stage.txt", "Saved UI");
            ValidateSavedUi();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            File.WriteAllText("Artifacts~/tmp-localization-stage.txt", "Module example");
            await CheckModuleExample();
            await CheckInteractiveControls();
            File.WriteAllText("Artifacts~/tmp-localization-stage.txt", "StarForce");
            await CheckStarForce();
            report.passed = true;
        }
        catch (Exception exception)
        {
            report.error = exception.ToString();
        }
        finally
        {
            EditorApplication.isPlaying = false;
            await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
        }
        report.checks = s_Checks.ToArray();
        Directory.CreateDirectory("Artifacts~");
        File.WriteAllText("Artifacts~/tmp-localization-workflow.json", JsonUtility.ToJson(report, true));
        EditorApplication.Exit(report.passed ? 0 : 1);
    }

    /// <summary>
    /// 检查每份场景和预制体的 TMP、输入框、下拉框与资源引用。
    /// </summary>
    private static void ValidateSavedUi()
    {
        foreach (string id in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Examples" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            ValidateRoot(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        }
        foreach (string id in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Examples" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            var scene = EditorSceneManager.OpenScene(path);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ValidateRoot(root, path);
            }
        }
        Require(!Directory.GetDirectories("Assets/Examples/02_StarForce/GameMain/Localization").Any(), "StarForce dictionaries use one flat directory");
    }

    /// <summary>
    /// 检查单个资产中的序列化控件，允许非 UI 的空对象引用。
    /// </summary>
    /// <param name="root">资产或场景根节点。</param>
    /// <param name="path">用于诊断的资产路径。</param>
    private static void ValidateRoot(GameObject root, string path)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "No missing scripts: " + path + " / " + child.name);
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Require(text.font != null && text.font.material != null && text.font.material.shader != null,
                "TMP font and shader: " + path + " / " + text.name);
        }
        foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            Require(input.textComponent != null && input.textViewport != null && input.textComponent != input.placeholder,
                "Input text and placeholder are distinct: " + path);
        }
        foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            Require(dropdown.captionText != null && dropdown.itemText != null && dropdown.template != null,
                "Dropdown caption and template: " + path);
        }
        Require(!root.GetComponentsInChildren<MonoBehaviour>(true).Any(component => component != null && component.GetType().Name == "ExampleSceneLayout"),
            "Native scene layout: " + path);
    }

    /// <summary>
    /// 操作本地化示例，检查命名参数、复数、关闭订阅和 TMP 字形。
    /// </summary>
    /// <returns>示例检查任务。</returns>
    private static async Task CheckModuleExample()
    {
        EditorSceneManager.OpenScene("Assets/Examples/01_Modules/10_Localization/02_SwitchLanguage/SwitchLanguage.unity");
        EditorApplication.isPlaying = true;
        await Until(() => EditorApplication.isPlaying && GameEntry.GetComponent<LocalizationComponent>() != null
            && GameEntry.GetComponent<LocalizationComponent>().Language == Language.EnglishUnitedStates
            && !GameEntry.GetComponent<LocalizationComponent>().IsChangingLanguage);
        LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
        LocalizeText reward = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(text => text.Key == "Reward.Coins");
        var values = new Dictionary<string, object> { { "Count", 1 }, { "Player", "Ada" } };
        reward.SetFormatValues(values);
        values["Player"] = "Changed";
        Require(reward.GetComponent<TMP_Text>().text == "Ada received 1 coin", "Named values are independent of dictionary order and ownership");
        reward.SetFormatValue("Count", 3);
        Require(reward.GetComponent<TMP_Text>().text == "Ada received 3 coins", "One named value updates plural formatting");
        await localization.SetLanguageAsync(Language.ChineseSimplified);
        Require(reward.GetComponent<TMP_Text>().text == "Ada 获得 3 金币", "Named values survive automatic language changes");
        TMP_Text title = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).Single(text => text.Key == "Game.Name").GetComponent<TMP_Text>();
        title.ForceMeshUpdate();
        Require(title.textInfo.characterCount > 0 && title.font.HasCharacters(title.text), "TMP generates Chinese glyph geometry");
        GameObject preview = title.transform.parent.gameObject;
        preview.SetActive(false);
        await localization.SetLanguageAsync(Language.EnglishUnitedStates);
        preview.SetActive(true);
        Require(title.text == "My Flying Game (US)", "Re-enabled text binds to the current language");
        await CheckInspectorValues(title.font);
        EditorApplication.isPlaying = false;
        await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
        await Task.Delay(100);
    }

    /// <summary>
    /// 检查 Inspector 初值、缺键策略和图片尺寸的启用生命周期。
    /// </summary>
    /// <param name="font">测试文本使用的 TMP 字体。</param>
    /// <returns>绑定配置检查任务。</returns>
    private static async Task CheckInspectorValues(TMP_FontAsset font)
    {
        var owner = new GameObject("Configured Text", typeof(RectTransform));
        owner.SetActive(false);
        TMP_Text text = owner.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = "Original";
        LocalizeText binding = owner.AddComponent<LocalizeText>();
        var data = new SerializedObject(binding);
        data.FindProperty("m_Key").stringValue = "Reward.Coins";
        SerializedProperty values = data.FindProperty("m_InitialFormatValues");
        values.arraySize = 2;
        SerializedProperty player = values.GetArrayElementAtIndex(0);
        player.FindPropertyRelative("m_Name").stringValue = "Player";
        player.FindPropertyRelative("m_Text").stringValue = "Inspector";
        SerializedProperty count = values.GetArrayElementAtIndex(1);
        count.FindPropertyRelative("m_Name").stringValue = "Count";
        count.FindPropertyRelative("m_Type").enumValueIndex = (int)LocalizationFormatValueType.Integer;
        count.FindPropertyRelative("m_Integer").intValue = 2;
        data.ApplyModifiedPropertiesWithoutUndo();
        owner.SetActive(true);
        await Task.Delay(100);
        Require(text.text == "Inspector received 2 coins", "Inspector values retain their numeric type");
        data.Update();
        data.FindProperty("m_MissingTranslation").enumValueIndex = (int)MissingTranslationBehavior.KeepOriginal;
        data.ApplyModifiedPropertiesWithoutUndo();
        binding.Key = "Missing.Key";
        Require(text.text == "Original", "Missing-key policy can keep the original text");
        data.Update();
        data.FindProperty("m_MissingTranslation").enumValueIndex = (int)MissingTranslationBehavior.ClearText;
        data.ApplyModifiedPropertiesWithoutUndo();
        binding.Refresh();
        Require(text.text == string.Empty, "Missing-key policy can clear the text");
        var imageOwner = new GameObject("Native Size Image", typeof(RectTransform), typeof(Image));
        imageOwner.SetActive(false);
        RectTransform rectangle = imageOwner.GetComponent<RectTransform>();
        rectangle.sizeDelta = new Vector2(120, 40);
        LocalizeImage imageBinding = imageOwner.AddComponent<LocalizeImage>();
        var imageData = new SerializedObject(imageBinding);
        imageData.FindProperty("m_Key").stringValue = "UI.Badge";
        imageData.FindProperty("m_SetNativeSize").boolValue = true;
        imageData.ApplyModifiedPropertiesWithoutUndo();
        imageOwner.SetActive(true);
        await Task.Delay(100);
        Require(imageOwner.GetComponent<Image>().sprite != null, "Configured image uses the current localized sprite");
        imageOwner.SetActive(false);
        Require(rectangle.sizeDelta == new Vector2(120, 40), "Disabled image restores its original dimensions");
        UnityEngine.Object.Destroy(owner);
        UnityEngine.Object.Destroy(imageOwner);
    }

    /// <summary>
    /// 用设置窗口的实际提交入口切换语言，确保框架和主菜单没有重建。
    /// </summary>
    /// <returns>StarForce 检查任务。</returns>
    private static async Task CheckStarForce()
    {
        EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
        BaseComponent initial = UnityEngine.Object.FindFirstObjectByType<BaseComponent>();
        initial.EditorLanguage = Language.English;
        PrefabUtility.RecordPrefabInstancePropertyModifications(initial);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorApplication.isPlaying = true;
        await Until(() => EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() != null);
        var menu = UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>();
        BaseComponent framework = GameEntry.GetComponent<BaseComponent>();
        LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
        foreach (Language language in new[] { Language.ChineseSimplified, Language.Korean, Language.ChineseTraditional, Language.English })
        {
            menu.OnSettingButtonClick();
            await Until(() => UnityEngine.Object.FindFirstObjectByType<StarForce.SettingForm>() != null);
            var settings = UnityEngine.Object.FindFirstObjectByType<StarForce.SettingForm>();
            if (language == Language.ChineseSimplified)
            {
                settings.OnChineseSimplifiedSelected(true);
            }
            else if (language == Language.ChineseTraditional)
            {
                settings.OnChineseTraditionalSelected(true);
            }
            else if (language == Language.Korean)
            {
                settings.OnKoreanSelected(true);
            }
            else
            {
                settings.OnEnglishSelected(true);
            }
            settings.OnSubmitButtonClick();
            await Until(() => localization.Language == language && !localization.IsChangingLanguage && !settings.Available);
            Require(GameEntry.GetComponent<BaseComponent>() == framework && UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() == menu,
                "Language switch retains the framework and menu: " + language.Code);
            foreach (LocalizeText binding in menu.GetComponentsInChildren<LocalizeText>(true))
            {
                TMP_Text text = binding.GetComponent<TMP_Text>();
                Require(text.text == localization.GetString(binding.Key), "Menu binding refreshes: " + language.Code + " / " + binding.Key);
                Require(text.font.HasCharacters(text.text), "Font covers translated glyphs: " + language.Code);
            }
            Require(StarForce.GameEntry.Setting.GetString(StarForce.Constant.Setting.Language) == language.Code, "Saved language matches the committed language");
            Directory.CreateDirectory("Artifacts~/TmpUiScreens");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/TmpUiScreens/StarForce-" + language.Code + ".png"));
            await Task.Delay(200);
        }
        EditorApplication.isPlaying = false;
        await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
    }

    /// <summary>
    /// 验证迁移后的下拉框模板和可编辑文本控件仍能交互。
    /// </summary>
    /// <returns>控件检查任务。</returns>
    private static async Task CheckInteractiveControls()
    {
        EditorSceneManager.OpenScene("Assets/Examples/01_Modules/06_Procedure/03_Data/ProcedureDataExample.unity");
        EditorApplication.isPlaying = true;
        await Until(() => EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<TMP_Dropdown>() != null);
        await Task.Delay(150);
        TMP_Dropdown dropdown = UnityEngine.Object.FindFirstObjectByType<TMP_Dropdown>();
        Require(dropdown.options.Count > 1, "Migrated dropdown retains its options");
        dropdown.Show();
        await Task.Delay(150);
        Require(dropdown.IsExpanded && dropdown.itemText.font != null, "TMP dropdown opens a visible text template");
        dropdown.value = 1;
        Require(dropdown.captionText.text == dropdown.options[1].text, "Dropdown selection updates its caption");
        dropdown.Hide();
        EditorApplication.isPlaying = false;
        await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
        await Task.Delay(100);
        EditorSceneManager.OpenScene("Assets/Examples/01_Modules/01_Base/06_Random/DrawRandom.unity");
        EditorApplication.isPlaying = true;
        await Until(() => EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<TMP_InputField>() != null);
        TMP_InputField input = UnityEngine.Object.FindFirstObjectByType<TMP_InputField>();
        input.text = "123";
        Require(input.textComponent.text.Contains("123") && input.textComponent != input.placeholder, "TMP input edits the value rather than its placeholder");
        EditorApplication.isPlaying = false;
        await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
        await Task.Delay(100);
    }

    /// <summary>
    /// 等待编辑器或异步语言请求达到预期状态。
    /// </summary>
    /// <param name="condition">完成条件。</param>
    /// <returns>等待任务。</returns>
    private static async Task Until(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(60);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("TMP localization workflow did not reach the expected state.");
            }
            await Task.Delay(30);
        }
    }

    /// <summary>
    /// 保存通过的实际检查。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="message">检查名称。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        s_Checks.Add(message);
    }

    /// <summary>
    /// 保存跨版本复核结果。
    /// </summary>
    [Serializable]
    private sealed class Report
    {
        /// <summary>
        /// 是否通过全部检查。
        /// </summary>
        public bool passed;

        /// <summary>
        /// 实际 Unity 版本。
        /// </summary>
        public string unity;

        /// <summary>
        /// 通过的检查名称。
        /// </summary>
        public string[] checks;

        /// <summary>
        /// 失败诊断。
        /// </summary>
        public string error;
    }
}
