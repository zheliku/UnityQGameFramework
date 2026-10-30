using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 在隔离工程检查语言切换、界面池化和场景销毁后的补偿卸载。
/// </summary>
public static class LocalizationIntegrationValidation
{
    private static string Root;
    private static readonly List<string> s_Checks = new List<string>();

    /// <summary>
    /// 创建专用资源并执行运行时检查，不写入主工程。
    /// </summary>
    /// <returns>已通过的检查及 Unity 版本。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an idle isolated validation project.");
        }
        s_Checks.Clear();
        Root = "Assets/LocalizationValidation" + DateTime.UtcNow.ToString("HHmmssfff");
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions options = EditorSettings.enterPlayModeOptions;
        try
        {
            Prepare();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.isPlaying = true;
            await Until(() => EditorApplication.isPlaying && GameEntry.GetComponent<LocalizationComponent>() != null);
            await Task.Delay(500);
            await CheckLanguageSwitch();
            await CheckDestroyedSceneOwner();
        }
        finally
        {
            EditorApplication.isPlaying = false;
            await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying);
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
        }
        var result = new Result
        {
            passed = true,
            unity = Application.unityVersion,
            checks = s_Checks.ToArray()
        };
        Directory.CreateDirectory("Artifacts~");
        File.WriteAllText("Artifacts~/localization-integration.json", JsonUtility.ToJson(result, true));
        return result;
    }

    /// <summary>
    /// 检查完整提交、取消、失败恢复以及界面绑定的订阅寿命。
    /// </summary>
    /// <returns>检查任务。</returns>
    private static async Task CheckLanguageSwitch()
    {
        LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
        EventComponent events = GameEntry.GetComponent<EventComponent>();
        var english = new LocalizationLanguage(Language.English, Root + "/Content/English.txt", Language.Unspecified,
            new LocalizationAssetMapping("Logo", Root + "/Content/English.png", LocalizationAssetType.Sprite),
            new LocalizationAssetMapping("Font", Root + "/Content/Font.ttf", LocalizationAssetType.Font));
        var chinese = new LocalizationLanguage(Language.ChineseSimplified, Root + "/Content/Chinese.txt", Language.English,
            new LocalizationAssetMapping("Logo", Root + "/Content/Chinese.png", LocalizationAssetType.Sprite));
        localization.SetLanguages(english, chinese,
            new LocalizationLanguage(Language.EnglishUnitedStates, Root + "/Content/English.txt"),
            new LocalizationLanguage(Language.French, Root + "/Content/Broken.txt", Language.English),
            new LocalizationLanguage(Language.German, Root + "/Content/InvalidFormat.txt", Language.English),
            new LocalizationLanguage(Language.Spanish, Root + "/Content/Spanish.txt", Language.English),
            new LocalizationLanguage(Language.Italian, Root + "/Content/Italian.txt", Language.English),
            new LocalizationLanguage(Language.FromCode("pt"), Root + "/Content/Portuguese.txt", Language.English));
        int notifications = 0;
        EventHandler<GameEventArgs> changed = (sender, args) => notifications++;
        events.Subscribe(UnityQGameFramework.Runtime.LanguageChangedEventArgs.EventId, changed);
        await localization.SetLanguageAsync(Language.EnglishUnitedStates);
        Require(localization.GetString("Game.Name") == "Regional" && localization.Language == Language.EnglishUnitedStates, "Regional dictionary is selected");
        Require(localization.GetString("Fallback.Hint") == "Fallback" && notifications == 1, "Parent language fills missing keys before notification");

        var textObject = new GameObject("Localized Text", typeof(RectTransform), typeof(Text));
        Text text = textObject.GetComponent<Text>();
        Font originalFont = text.font;
        LocalizeText binding = textObject.AddComponent<LocalizeText>();
        typeof(LocalizeText).GetField("m_FontKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(binding, "Font");
        binding.Key = "Game.Name";
        var imageObject = new GameObject("Localized Image", typeof(RectTransform), typeof(Image));
        Image image = imageObject.GetComponent<Image>();
        LocalizeImage imageBinding = imageObject.AddComponent<LocalizeImage>();
        imageBinding.Key = "Logo";
        await Task.Delay(50);
        Require(text.text == "Regional" && text.font == localization.GetAsset<Font>("Font"), "LocalizeText uses the prepared language font");
        Require(image.sprite == localization.GetAsset<Sprite>("Logo"), "PNG is loaded as Sprite for LocalizeImage");
        AssetHandle[] previousHandles = GetLanguageHandles(localization);
        await localization.SetLanguageAsync(Language.ChineseSimplified);
        Require(text.text == "Chinese" && image.sprite == localization.GetAsset<Sprite>("Logo"), "Text and image refresh on language notification");
        Require(previousHandles.All(handle => !handle.IsValid), "Old language handles are released after interface refresh");
        binding.Key = "Coins";
        binding.SetArguments(2);
        Require(text.text == localization.GetString("Coins", 2), "Text binding expands positional arguments using the selected language culture");
        Require(localization.GetAsset<Font>("Font") != null, "Missing target assets fall back to the configured language");
        Language committedCode = localization.Language;
        AssetHandle[] committedHandles = GetLanguageHandles(localization);
        int committedNotifications = notifications;
        using (var cancellation = new CancellationTokenSource())
        {
            UniTask pending = localization.SetLanguageAsync(Language.Spanish, cancellation.Token);
            cancellation.Cancel();
            Require(await pending.SuppressCancellationThrow(), "In-flight language request observes cancellation");
        }
        Require(localization.Language == committedCode && notifications == committedNotifications && committedHandles.All(handle => handle.IsValid),
            "Cancellation keeps the old language, handles and notification count");
        foreach (Language code in new[] { Language.French, Language.German })
        {
            bool rejected = false;
            try
            {
                await localization.SetLanguageAsync(code);
            }
            catch (Exception)
            {
                rejected = true;
            }
            Require(rejected && localization.Language == committedCode && notifications == committedNotifications
                && committedHandles.All(handle => handle.IsValid), "Invalid dictionary preserves the committed snapshot: " + code);
        }
        UniTask superseded = localization.SetLanguageAsync(Language.FromCode("pt"));
        await localization.SetLanguageAsync(Language.ChineseSimplified);
        Require(await superseded.SuppressCancellationThrow() && localization.Language == Language.ChineseSimplified, "A newer request cancels the uncommitted previous language");
        int listeners = events.Count(UnityQGameFramework.Runtime.LanguageChangedEventArgs.EventId);
        textObject.SetActive(false);
        imageObject.SetActive(false);
        Require(events.Count(UnityQGameFramework.Runtime.LanguageChangedEventArgs.EventId) == listeners - 2 && text.font == originalFont && image.sprite == null,
            "Pooled bindings unsubscribe and restore original resource references");
        await localization.SetLanguageAsync(Language.EnglishUnitedStates);
        textObject.SetActive(true);
        imageObject.SetActive(true);
        await Task.Delay(50);
        Require(events.Count(UnityQGameFramework.Runtime.LanguageChangedEventArgs.EventId) == listeners && text.text == "2 coins",
            "Re-enabled bindings read the current language without duplicate subscriptions");
        bool cycleRejected = false;
        try
        {
            localization.SetLanguages(new LocalizationLanguage(Language.English, english.DictionaryAssetName, Language.ChineseSimplified), chinese);
        }
        catch (GameFrameworkException)
        {
            cycleRejected = true;
        }
        Require(cycleRejected && localization.Languages.Count == 8, "Fallback cycles are rejected without replacing valid configuration");
        UnityEngine.Object.DestroyImmediate(textObject);
        UnityEngine.Object.DestroyImmediate(imageObject);
        events.Unsubscribe(UnityQGameFramework.Runtime.LanguageChangedEventArgs.EventId, changed);
        AssetHandle[] lastHandles = GetLanguageHandles(localization);
        UniTask destruction = localization.SetLanguageAsync(Language.Italian);
        UnityEngine.Object.DestroyImmediate(localization);
        Require(await destruction.SuppressCancellationThrow(), "Destroying Localization cancels its pending switch");
        Require(lastHandles.All(handle => !handle.IsValid), "Destroying Localization releases its committed handles");
    }

    /// <summary>
    /// 销毁等待中的场景组件，确认晚到的场景完成补偿卸载。
    /// </summary>
    /// <returns>检查任务。</returns>
    private static async Task CheckDestroyedSceneOwner()
    {
        SceneComponent scenes = GameEntry.GetComponent<SceneComponent>();
        ISceneManager manager = GameFrameworkEntry.GetModule<ISceneManager>();
        string path = Root + "/Content/Target.unity";
        UniTask pending = scenes.LoadSceneAsync(path);
        UnityEngine.Object.DestroyImmediate(scenes);
        Require(await pending.SuppressCancellationThrow(), "Destroyed SceneComponent cancels the pending caller");
        await Until(() => !manager.SceneIsLoading(path) && !manager.SceneIsLoaded(path) && !manager.SceneIsUnloading(path));
        Require(UnityEngine.SceneManagement.SceneManager.sceneCount == 1, "Late scene success is compensated after component destruction");
    }

    /// <summary>
    /// 保存当前快照的句柄，用于验证新旧快照的所有权。
    /// </summary>
    /// <param name="localization">待检查的组件。</param>
    /// <returns>本次快照的句柄副本。</returns>
    private static AssetHandle[] GetLanguageHandles(LocalizationComponent localization)
    {
        var assets = (Dictionary<string, AssetHandle>)typeof(LocalizationComponent).GetField("m_LanguageAssets", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(localization);
        return assets.Values.ToArray();
    }

    /// <summary>
    /// 创建仅供验证的字典、图片、字体和资源收集配置。
    /// </summary>
    private static void Prepare()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (AssetDatabase.IsValidFolder(Root))
        {
            AssetDatabase.DeleteAsset(Root);
        }
        Directory.CreateDirectory(Root + "/Content");
        File.WriteAllText(Root + "/Content/English.txt", "[en.Game]\nName = \"English\"\n[en-US.Game]\nName = \"Regional\"\n[en.Fallback]\nHint = \"Fallback\"\n[en]\nCoins = \"{0} {0:plural:coin|coins}\"\n");
        File.WriteAllText(Root + "/Content/Chinese.txt", "[zh-Hans.Game]\nName = \"Chinese\"\n");
        File.WriteAllText(Root + "/Content/Broken.txt", "[fr\n");
        File.WriteAllText(Root + "/Content/InvalidFormat.txt", "[de.Game]\nName = \"{0\"\n");
        File.WriteAllText(Root + "/Content/Spanish.txt", "[es.Game]\nName = \"Spanish\"\n");
        File.WriteAllText(Root + "/Content/Italian.txt", "[it.Game]\nName = \"Italian\"\n");
        File.WriteAllText(Root + "/Content/Portuguese.txt", "[pt.Game]\nName = \"Portuguese\"\n");
        File.Copy("Assets/Examples/02_StarForce/GameMain/Fonts/MainFont.ttf", Root + "/Content/Font.ttf");
        foreach (string name in new[] { "English", "Chinese" })
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(Enumerable.Repeat(name == "English" ? Color.red : Color.blue, 4).ToArray());
            texture.Apply();
            File.WriteAllBytes(Root + "/Content/" + name + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.Refresh();
        foreach (string name in new[] { "English", "Chinese" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Content/" + name + ".png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, Root + "/Content/Target.unity");
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var collector = ScriptableObject.CreateInstance<BundleCollectorSetting>();
        AssetDatabase.CreateAsset(collector, Root + "/Collector.asset");
        var package = new BundleCollectorPackage { PackageName = "LocalizationValidation", AutoCollectShaders = false };
        var group = new BundleCollectorGroup { GroupName = "Content" };
        group.Collectors.Add(new BundleCollector
        {
            CollectPath = Root + "/Content",
            CollectorGUID = AssetDatabase.AssetPathToGUID(Root + "/Content"),
            PackRuleName = nameof(PackSeparately),
            AddressRuleName = nameof(AddressDisable)
        });
        package.Groups.Add(group);
        collector.Packages.Add(package);
        EditorUtility.SetDirty(collector);
        var settings = ScriptableObject.CreateInstance<ResourceSettings>();
        JsonUtility.FromJsonOverwrite("{\"m_DefaultPackageName\":\"LocalizationValidation\",\"m_SerializationVersion\":3,\"m_Packages\":[{\"m_Name\":\"LocalizationValidation\",\"m_Mode\":1,\"m_LegacyPlayMode\":-1}]}", settings);
        AssetDatabase.CreateAsset(settings, Root + "/ResourceSettings.asset");
        var settingsBinding = new SerializedObject(settings);
        settingsBinding.FindProperty("m_EditorCollector").objectReferenceValue = collector;
        settingsBinding.ApplyModifiedPropertiesWithoutUndo();
        var root = new GameObject("QGF");
        root.AddComponent<BaseComponent>();
        root.AddComponent<EventComponent>();
        ResourceComponent resources = root.AddComponent<ResourceComponent>();
        var resourceBinding = new SerializedObject(resources);
        resourceBinding.FindProperty("m_Settings").objectReferenceValue = settings;
        resourceBinding.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<LocalizationComponent>();
        root.AddComponent<SceneComponent>();
        EditorSceneManager.SaveScene(scene, Root + "/Bootstrap.unity");
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 等待编辑器或场景达到预期状态。
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
                throw new TimeoutException("Localization validation timed out.");
            }
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// 登记通过的检查，失败时保留具体操作名称。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="description">检查名称。</param>
    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
        s_Checks.Add(description);
    }

    /// <summary>
    /// 保存集成检查结果。
    /// </summary>
    [Serializable]
    private sealed class Result
    {
        /// <summary>
        /// 是否通过所有检查。
        /// </summary>
        public bool passed;

        /// <summary>
        /// 实际运行的 Unity 版本。
        /// </summary>
        public string unity;

        /// <summary>
        /// 已通过的行为检查。
        /// </summary>
        public string[] checks;
    }
}
