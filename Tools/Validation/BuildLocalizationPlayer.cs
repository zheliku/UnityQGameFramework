using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 在隔离工程构建包含当前框架、离线资源与 TMP 的验收 Player。
/// </summary>
public static class BuildLocalizationPlayer
{
    private const string Root = "Assets/LocalizationPlayerValidation";

    /// <summary>
    /// 构建 IL2CPP 验收 Player，记录结果后退出隔离编辑器。
    /// </summary>
    public static void RunBatch()
    {
        try
        {
            string player = Run();
            File.WriteAllText("Artifacts~/localization-player-build.txt", player);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText("Artifacts~/localization-player-build.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 准备专用资产和场景，构建 Windows 高裁剪 Player。
    /// </summary>
    /// <param name="il2cpp">是否使用 IL2CPP；否则使用 Mono。</param>
    /// <returns>Player 可执行文件路径。</returns>
    public static string Run(bool il2cpp = true)
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an idle isolated validation project.");
        }
        Directory.CreateDirectory(Root + "/Content");
        if (Shader.Find("TextMeshPro/Distance Field") == null)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
        }
        AssetDatabase.Refresh();
        if (TMP_Settings.LoadDefaultSettings() == null)
        {
            throw new InvalidOperationException("Import TMP Essential Resources before creating validation fonts.");
        }
        var scene = EditorSceneManager.OpenScene("Assets/Examples/01_Modules/10_Localization/02_SwitchLanguage/SwitchLanguage.unity");
        const string source = "Assets/Examples/01_Modules/10_Localization/Common/ChineseFont.otf";
        Font font = AssetDatabase.LoadAssetAtPath<Font>(source);
        TMP_FontAsset english = CreateFont(font, "EnglishTMP");
        TMP_FontAsset chinese = CreateFont(font, "ChineseTMP");
        TMP_FontAsset original = CreateFont(font, "OriginalTMP");
        ResourceComponent resources = UnityEngine.Object.FindFirstObjectByType<ResourceComponent>();
        ResourceSettings settings = UnityEngine.Object.Instantiate(resources.Settings);
        AssetDatabase.CreateAsset(settings, Root + "/ResourceSettings.asset");
        BundleCollectorSetting collector = UnityEngine.Object.Instantiate(ResourceBuildService.GetCollector(resources.Settings));
        collector.Packages = collector.Packages.Where(package => package.PackageName == settings.DefaultPackageName).ToList();
        var group = new BundleCollectorGroup { GroupName = "LocalizationValidation" };
        group.Collectors.Add(new BundleCollector
        {
            CollectPath = Root + "/Content",
            CollectorGUID = AssetDatabase.AssetPathToGUID(Root + "/Content"),
            AddressRuleName = nameof(AddressDisable),
            PackRuleName = nameof(PackSeparately)
        });
        collector.Packages[0].Groups.Add(group);
        AssetDatabase.CreateAsset(collector, Root + "/Collector.asset");
        ResourceSettingsEditor.SetCollector(settings, collector);
        var settingsData = new SerializedObject(settings);
        SerializedProperty packages = settingsData.FindProperty("m_Packages");
        for (int index = 0; index < packages.arraySize; index++)
        {
            packages.GetArrayElementAtIndex(index).FindPropertyRelative("m_Mode").intValue = (int)EPlayMode.OfflinePlayMode;
        }
        settingsData.ApplyModifiedPropertiesWithoutUndo();
        var resourceData = new SerializedObject(resources);
        resourceData.FindProperty("m_Settings").objectReferenceValue = settings;
        resourceData.ApplyModifiedPropertiesWithoutUndo();
        LocalizationComponent localization = UnityEngine.Object.FindFirstObjectByType<LocalizationComponent>();
        LocalizationLanguage[] languages = localization.Languages.Select(configuration => new LocalizationLanguage(
            configuration.Language, configuration.DictionaryAssetName, configuration.FallbackLanguage,
            configuration.Assets.Where(asset => asset.Key != "UI.TMPFont").Concat(configuration.Language == Language.English || configuration.Language == Language.ChineseSimplified
                ? new[] { new LocalizationAssetMapping("UI.TMPFont", AssetDatabase.GetAssetPath(configuration.Language == Language.English ? english : chinese)) }
                : Array.Empty<LocalizationAssetMapping>()).ToArray())).ToArray();
        localization.SetLanguages(languages);
        PrefabUtility.RecordPrefabInstancePropertyModifications(localization);
        Transform preview = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(transform => transform.name == "Panel_LocalizedPreview");
        if (preview.Find("Text_TMPValidation") != null)
        {
            UnityEngine.Object.DestroyImmediate(preview.Find("Text_TMPValidation").gameObject);
        }
        var textObject = new GameObject("Text_TMPValidation", typeof(RectTransform));
        textObject.SetActive(false);
        textObject.transform.SetParent(preview, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = original;
        LocalizeTmpText binding = textObject.AddComponent<LocalizeTmpText>();
        var bindingData = new SerializedObject(binding);
        bindingData.FindProperty("m_Key").stringValue = "Game.Name";
        bindingData.FindProperty("m_FontKey").stringValue = "UI.TMPFont";
        bindingData.ApplyModifiedPropertiesWithoutUndo();
        textObject.SetActive(true);
        new GameObject("Localization Player Probe").AddComponent<LocalizationPlayerProbe>();
        EditorSceneManager.SaveScene(scene, Root + "/Launcher.unity");
        AssetDatabase.SaveAssets();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, il2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
        return ResourceBuildService.BuildPlayer(collector, settings.DefaultPackageName, Root + "/Launcher.unity",
            "Artifacts~/LocalizationPlayer/Localization.exe");
    }

    /// <summary>
    /// 创建独立字体资源并保存材质和图集，覆盖 Player 中的真实资源依赖。
    /// </summary>
    /// <param name="font">包含示例字符的字体。</param>
    /// <param name="name">字体资产名。</param>
    /// <returns>保存后的 TMP 字体。</returns>
    private static TMP_FontAsset CreateFont(Font font, string name)
    {
        string path = Root + "/Content/" + name + ".asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null)
        {
            return existing;
        }
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
        asset.name = name;
        string characters = new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray()) + "我的飞行游戏";
        if (!asset.TryAddCharacters(characters))
        {
            throw new InvalidOperationException("The validation font is missing required characters.");
        }
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D texture in asset.atlasTextures)
        {
            AssetDatabase.AddObjectToAsset(texture, asset);
        }
        EditorUtility.SetDirty(asset);
        return asset;
    }
}
