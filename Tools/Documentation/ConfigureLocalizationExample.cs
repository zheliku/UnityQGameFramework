using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core;

/// <summary>
/// 保存本地化示例的组件绑定，不给消费项目添加制作脚本。
/// </summary>
public static class ConfigureLocalizationExample
{
    private const string ScenePath = "Assets/Examples/01_Modules/10_Localization/02_SwitchLanguage/SwitchLanguage.unity";

    /// <summary>
    /// 在编辑源场景配置自动文本、字体、图片与隐藏预览操作。
    /// </summary>
    /// <returns>保存的场景路径。</returns>
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
        {
            throw new InvalidOperationException("Stop Play Mode and save current scenes first.");
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            const string art = "Assets/Examples/01_Modules/10_Localization/Common/";
            foreach (string badge in new[] { "EnglishBadge", "ChineseBadge" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(art + badge + ".png");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            SwitchLanguage example = UnityEngine.Object.FindFirstObjectByType<SwitchLanguage>();
            var exampleData = new SerializedObject(example);
            Button first = (Button)exampleData.FindProperty("action1Button").objectReferenceValue;
            Transform content = first.transform.parent;
            Canvas canvas = first.GetComponentInParent<Canvas>();
            Text template = canvas.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "Text_Output" || text.gameObject.name == "Text_GameName");
            GameObject oldPreview = canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == "Panel_LocalizedPreview")?.gameObject;
            if (oldPreview != null)
            {
                template.transform.SetParent(content, false);
                UnityEngine.Object.DestroyImmediate(oldPreview);
            }
            foreach (string name in new[] { "Toggle_Preview", "Button_AddCoin", "Text_BindingHint" })
            {
                if (content.Find(name) != null)
                {
                    UnityEngine.Object.DestroyImmediate(content.Find(name).gameObject);
                }
            }

            var panel = new GameObject("Panel_LocalizedPreview", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvas.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.75f, .5f);
            panelRect.sizeDelta = new Vector2(380, 340);
            var surface = panel.AddComponent<Image>();
            surface.color = new Color(.025f, .055f, .08f, .3f);
            surface.raycastTarget = false;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text fallback = CopyText(template, panel.transform, "Text_FallbackHint", 72);
            Text reward = CopyText(template, panel.transform, "Text_Reward", 42);
            template.name = "Text_GameName";
            template.transform.SetParent(panel.transform, false);
            template.transform.SetAsFirstSibling();
            template.GetComponent<LayoutElement>().preferredHeight = 48;
            BindText(template, "Game.Name");
            BindText(fallback, "Fallback.Hint");
            LocalizeText rewardBinding = BindText(reward, "Reward.Coins");
            var logo = new GameObject("Image_LanguageBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(LocalizeImage));
            logo.transform.SetParent(panel.transform, false);
            logo.GetComponent<LayoutElement>().preferredHeight = 84;
            logo.GetComponent<Image>().preserveAspect = true;
            logo.GetComponent<Image>().raycastTarget = false;
            logo.GetComponent<LocalizeImage>().Key = "UI.Badge";
            Text hint = CopyText(template, content, "Text_BindingHint", 84);
            UnityEngine.Object.DestroyImmediate(hint.GetComponent<LocalizeText>());
            hint.text = "Text, font and badge update through localization components. Hide the preview, switch language, then show it again.";
            hint.fontSize = 22;
            hint.transform.SetSiblingIndex(3);
            Button add = UnityEngine.Object.Instantiate(first, content);
            add.name = "Button_AddCoin";
            add.GetComponentInChildren<Text>().text = "Add Coin (+1)";
            add.transform.SetSiblingIndex(3);
            GameObject toggle = DefaultControls.CreateToggle(default);
            toggle.name = "Toggle_Preview";
            toggle.transform.SetParent(content, false);
            toggle.transform.SetSiblingIndex(4);
            var toggleElement = toggle.AddComponent<LayoutElement>();
            toggleElement.preferredHeight = 48;
            Text toggleLabel = toggle.GetComponentInChildren<Text>();
            EditorUtility.CopySerialized(first.GetComponentInChildren<Text>(), toggleLabel);
            toggleLabel.text = "Show localized preview";
            toggleLabel.alignment = TextAnchor.MiddleLeft;
            var labelRect = (RectTransform)toggleLabel.transform;
            labelRect.offsetMin = new Vector2(44, 0);
            labelRect.offsetMax = Vector2.zero;
            toggle.GetComponent<Toggle>().isOn = true;
            // 复用模块示例已有的 StarForce 风格勾选图片。
            var sprites = "Assets/Examples/01_Modules/Common/UI/UISprites/";
            toggle.transform.Find("Background").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprites + "ButtonOutline.png");
            toggle.transform.Find("Background/Checkmark").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprites + "Checkmark.png");
            var backgroundRect = (RectTransform)toggle.transform.Find("Background");
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0, .5f);
            backgroundRect.anchoredPosition = new Vector2(14, 0);
            backgroundRect.sizeDelta = new Vector2(24, 24);
            var responsive = canvas.GetComponent<UnityQGameFramework.Examples.Modules.Presentation.ExampleSceneLayout>()
                ?? canvas.gameObject.AddComponent<UnityQGameFramework.Examples.Modules.Presentation.ExampleSceneLayout>();
            var responsiveData = new SerializedObject(responsive);
            responsiveData.FindProperty("controls").objectReferenceValue = first.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>();
            responsiveData.FindProperty("window").objectReferenceValue = panelRect;
            responsiveData.ApplyModifiedPropertiesWithoutUndo();
            exampleData.FindProperty("rewardButton").objectReferenceValue = add;
            exampleData.FindProperty("previewToggle").objectReferenceValue = toggle.GetComponent<Toggle>();
            exampleData.FindProperty("preview").objectReferenceValue = panel;
            exampleData.FindProperty("rewardText").objectReferenceValue = rewardBinding;
            exampleData.ApplyModifiedPropertiesWithoutUndo();
            LocalizationComponent localization = UnityEngine.Object.FindFirstObjectByType<LocalizationComponent>();
            var data = new SerializedObject(localization);
            SerializedProperty languages = data.FindProperty("m_Languages");
            languages.arraySize = 3;
            ConfigureLanguage(languages.GetArrayElementAtIndex(0), Language.English, Language.Unspecified, "Localization_EnglishBadge", null);
            ConfigureLanguage(languages.GetArrayElementAtIndex(1), Language.EnglishUnitedStates, Language.Unspecified, null, null);
            ConfigureLanguage(languages.GetArrayElementAtIndex(2), Language.ChineseSimplified, Language.English, "Localization_ChineseBadge", "Localization_ChineseFont");
            data.FindProperty("m_FallbackLanguageCode").stringValue = Language.English.Code;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(localization);
            localization.ValidateLanguages();
            EditorSceneManager.SaveScene(scene);
            return ScenePath;
        }
        finally
        {
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }

    /// <summary>
    /// 复制现有示例文本的样式并指定布局高度。
    /// </summary>
    /// <param name="template">样式来源。</param>
    /// <param name="parent">父节点。</param>
    /// <param name="name">物体名。</param>
    /// <param name="height">首选高度。</param>
    /// <returns>新文本。</returns>
    private static Text CopyText(Text template, Transform parent, string name, float height)
    {
        Text text = UnityEngine.Object.Instantiate(template, parent);
        text.name = name;
        text.text = "Loading language...";
        text.GetComponent<LayoutElement>().preferredHeight = height;
        return text;
    }

    /// <summary>
    /// 保存文本与可选语言字体的 Inspector 绑定。
    /// </summary>
    /// <param name="text">目标文本。</param>
    /// <param name="key">字典键。</param>
    /// <returns>本地化文本组件。</returns>
    private static LocalizeText BindText(Text text, string key)
    {
        LocalizeText binding = text.GetComponent<LocalizeText>() ?? text.gameObject.AddComponent<LocalizeText>();
        var data = new SerializedObject(binding);
        data.FindProperty("m_Key").stringValue = key;
        data.FindProperty("m_FontKey").stringValue = "UI.Font";
        data.ApplyModifiedPropertiesWithoutUndo();
        return binding;
    }

    /// <summary>
    /// 保存一个语言配置，运行时不重复写入这些值。
    /// </summary>
    /// <param name="language">语言配置属性。</param>
    /// <param name="code">标准语言代码。</param>
    /// <param name="fallback">显式回退代码。</param>
    /// <param name="badge">图片地址。</param>
    /// <param name="font">字体地址。</param>
    private static void ConfigureLanguage(SerializedProperty language, Language code, Language fallback, string badge, string font)
    {
        language.FindPropertyRelative("m_Code").stringValue = code.Code;
        language.FindPropertyRelative("m_DictionaryAssetName").stringValue = "Localization_Dictionary";
        language.FindPropertyRelative("m_FallbackCode").stringValue = fallback.Code;
        SerializedProperty assets = language.FindPropertyRelative("m_Assets");
        assets.arraySize = (badge == null ? 0 : 1) + (font == null ? 0 : 1);
        if (badge != null)
        {
            SetAsset(assets.GetArrayElementAtIndex(0), "UI.Badge", badge, LocalizationAssetType.Sprite);
        }
        if (font != null)
        {
            SetAsset(assets.GetArrayElementAtIndex(assets.arraySize - 1), "UI.Font", font, LocalizationAssetType.Font);
        }
    }

    /// <summary>
    /// 保存语言资源键与加载类型。
    /// </summary>
    /// <param name="asset">映射属性。</param>
    /// <param name="key">业务键。</param>
    /// <param name="address">YooAsset 地址。</param>
    /// <param name="type">加载类型。</param>
    private static void SetAsset(SerializedProperty asset, string key, string address, LocalizationAssetType type)
    {
        asset.FindPropertyRelative("m_Key").stringValue = key;
        asset.FindPropertyRelative("m_AssetName").stringValue = address;
        asset.FindPropertyRelative("m_AssetType").enumValueIndex = (int)type;
    }
}
