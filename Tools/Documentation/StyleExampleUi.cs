using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 只制作仓库里的保存资产，不给示例增加皮肤管理器或运行时代码。
public static class StyleExampleUi
{
    private static readonly Color Ink = Hex("E9F4FC"), Muted = Hex("B8CBD5");
    private static readonly Color Accent = Hex("A6E7F5"), Soft = new Color(.025f, .055f, .08f, .42f), Track = new Color(.12f, .24f, .32f, .4f);
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static object Run() => RunModules(null);

    public static object RunModules(string[] folders) => RunSelected(folders, null);

    public static object RunExamples(string[] names) => RunSelected(null, names);

    private static object RunSelected(string[] folders, string[] names)
    {
        if (!Path.GetFullPath(Application.dataPath).Contains(Path.DirectorySeparatorChar + "Artifacts~" + Path.DirectorySeparatorChar))
            throw new Exception("Use the isolated authoring project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene.");
        sprites.Clear();
        var roots = folders == null ? new[] { "Assets/Examples" } : folders.Select(folder => "Assets/Examples/" + folder).ToArray();
        var layouts = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = names?.Select(name => Path.GetDirectoryName((string)layouts[name]["script"]).Replace('\\', '/')).ToArray();
        int scenes = 0, prefabs = 0;
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", roots).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                if (cases != null && !cases.Any(folder => path.StartsWith(folder + "/", StringComparison.Ordinal))) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponentInChildren<RectTransform>(true) == null) continue;
                    Apply(root, path);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in AssetDatabase.FindAssets("t:Scene", roots).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                if (cases != null && !cases.Any(folder => path.StartsWith(folder + "/", StringComparison.Ordinal))) continue;
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var root in scene.GetRootGameObjects()) Apply(root, path);
                if (!path.Contains("02_StarForce"))
                {
                    RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
                    foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.clearFlags = CameraClearFlags.Skybox;
                }
                EditorSceneManager.SaveScene(scene);
                scenes++;
            }
        }
        finally
        {
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        var result = new { passed = true, scenes, prefabs, palette = "StarForce / transparent controls / white outlines", background = "Unity default skybox; StarForce keeps its own scene", runtimeThemeDependency = false };
        Directory.CreateDirectory("Artifacts~/SciFiStyle");
        File.WriteAllText("Artifacts~/SciFiStyle/style-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static void Apply(GameObject root, string path)
    {
        bool starForce = path.Contains("02_StarForce");
        if (starForce)
            foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true)) scaler.matchWidthOrHeight = 1;
        var outline = SpriteFor(path, "button-outline.png", "ButtonOutline.png");
        var checkmark = SpriteFor(path, "checked.png", "Checkmark.png");
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            text.color = Ink;
            text.lineSpacing = 1.05f;
            foreach (var shadow in text.GetComponents<Shadow>()) UnityEngine.Object.DestroyImmediate(shadow);
            if (text.name.Contains("Title")) text.fontStyle = FontStyle.Bold;
            if (!starForce && text.fontSize >= 26) text.fontSize = 26;
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<Mask>() != null) { image.color = Color.white; continue; }
            if (image.name.Contains("Marker")) image.color = image.transform.childCount == 0 ? Accent : Track;
            else if (image.name.Contains("Checkmark") || image.name.Contains("Fill")) image.color = Accent;
            else if (image.name.Contains("Handle")) image.color = Color.white;
            else if (image.name.Contains("Arrow")) image.color = Muted;
            else if (image.name.Contains("Mask")) image.color = new Color(.015f, .035f, .06f, .45f);
            else if (image.name.Contains("Page") || image.name.Contains("Board")) image.color = Soft;
            else if (image.GetComponent<ScrollRect>() != null || image.name.Contains("Background") || image.name.Contains("Window")) image.color = Soft;
            else image.color = Track;
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            bool board = button.name.Contains("Board");
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                if (board) image.color = new Color(.02f, .07f, .12f, .22f);
                else Border(image, outline);
            }
            foreach (var text in button.GetComponentsInChildren<Text>(true))
            { text.color = Ink; text.fontStyle = FontStyle.Bold; }
            Colors(button);
        }
        foreach (var input in root.GetComponentsInChildren<InputField>(true))
        {
            input.caretColor = Accent;
            input.selectionColor = new Color(.15f, .6f, .8f, .3f);
            input.textComponent.color = Ink;
            if (input.placeholder != null) input.placeholder.color = Muted;
            if (input.targetGraphic is Image image) Border(image, outline);
            Colors(input);
        }
        foreach (var dropdown in root.GetComponentsInChildren<Dropdown>(true))
        {
            StyleDropdown(dropdown, outline, checkmark);
        }
        foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.GetComponentInParent<Dropdown>(true) != null) continue;
            if (toggle.graphic != null) toggle.graphic.color = Accent;
            if (toggle.graphic is Image tick) tick.sprite = checkmark;
            if (toggle.targetGraphic != null) toggle.targetGraphic.color = Track;
            Colors(toggle);
        }
        foreach (var slider in root.GetComponentsInChildren<Slider>(true))
        {
            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Accent;
            if (slider.handleRect != null) slider.handleRect.GetComponent<Image>().color = Color.white;
            Colors(slider);
        }
        foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.GetComponent<Image>() is Image image && scroll.name == "ScrollView_Example")
            {
                image.color = Soft;
                image.sprite = null;
                var rect = (RectTransform)scroll.transform;
                rect.offsetMin = new Vector2(28, 28);
                rect.offsetMax = new Vector2(-28, -28);
                var column = scroll.content.GetComponent<VerticalLayoutGroup>();
                if (column != null)
                {
                    column.padding = new RectOffset(24, 24, 24, 24);
                    column.spacing = path.Contains("/15_Sound/05_Spatial/") ? 8 : 12;
                }
            }
        }
        if (path.Contains("/14_UI/") && path.EndsWith("/Art/Window.prefab", StringComparison.Ordinal))
        {
            var label = root.GetComponentInChildren<Text>(true);
            label.fontSize = 20;
            label.fontStyle = FontStyle.Normal;
            var panel = (RectTransform)label.transform.parent;
            var border = panel.Find("Image_Border") as RectTransform;
            if (border == null)
            {
                border = new GameObject("Image_Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                border.SetParent(panel, false);
            }
            border.anchorMin = Vector2.zero;
            border.anchorMax = Vector2.one;
            border.offsetMin = border.offsetMax = Vector2.zero;
            Border(border.GetComponent<Image>(), outline);
            border.GetComponent<Image>().raycastTarget = false;
        }
        if (starForce)
        {
            StyleCommonButtons(root);
            if (root.name == "MenuForm") StyleStarForceMenu(root);
        }
    }

    private static void StyleDropdown(Dropdown dropdown, Sprite outline, Sprite checkmark)
    {
        if (dropdown.targetGraphic is Image image) Border(image, outline);
        Colors(dropdown);
        dropdown.captionText.color = Ink;
        dropdown.captionText.lineSpacing = 1;
        dropdown.captionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        // 长选项适配标题区域，避免换行后溢出到下面的按钮。
        dropdown.captionText.verticalOverflow = VerticalWrapMode.Truncate;
        dropdown.captionText.resizeTextForBestFit = true;
        dropdown.captionText.resizeTextMinSize = 14;
        dropdown.captionText.resizeTextMaxSize = 18;
        dropdown.captionText.rectTransform.offsetMin = new Vector2(14, 4);
        dropdown.captionText.rectTransform.offsetMax = new Vector2(-34, -4);
        foreach (var arrow in dropdown.GetComponentsInChildren<Image>(true).Where(i => i.transform.parent == dropdown.transform && i.name.Contains("Arrow")))
        {
            arrow.sprite = null;
            arrow.color = Color.clear;
            Stroke(arrow.transform, "Image_ArrowLeft", new Vector2(-2.5f, 0), -45);
            Stroke(arrow.transform, "Image_ArrowRight", new Vector2(2.5f, 0), 45);
        }
        var template = dropdown.template;
        template.anchorMin = new Vector2(0, 0);
        template.anchorMax = new Vector2(1, 0);
        template.pivot = new Vector2(.5f, 1);
        template.anchoredPosition = new Vector2(0, -4);
        template.sizeDelta = new Vector2(0, 226);
        var surface = template.GetComponent<Image>();
        // 展开列表需要独立的底色，避免下层按钮透过来干扰选项文字。
        surface.sprite = null;
        surface.type = Image.Type.Simple;
        surface.color = new Color(.025f, .055f, .075f, 1);
        var border = template.Find("Image_Border") as RectTransform;
        if (border == null)
        {
            border = new GameObject("Image_Border", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            border.SetParent(template, false);
        }
        border.anchorMin = Vector2.zero;
        border.anchorMax = Vector2.one;
        border.offsetMin = border.offsetMax = Vector2.zero;
        Border(border.GetComponent<Image>(), outline);
        border.GetComponent<Image>().raycastTarget = false;
        var scroll = template.GetComponent<ScrollRect>();
        scroll.viewport.anchorMin = Vector2.zero;
        scroll.viewport.anchorMax = Vector2.one;
        scroll.viewport.offsetMin = new Vector2(8, 8);
        scroll.viewport.offsetMax = new Vector2(-22, -8);
        scroll.content.anchorMin = new Vector2(0, 1);
        scroll.content.anchorMax = Vector2.one;
        scroll.content.pivot = new Vector2(.5f, 1);
        scroll.content.anchoredPosition = Vector2.zero;
        scroll.content.sizeDelta = new Vector2(0, 40);
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        if (scroll.verticalScrollbar != null)
        {
            var bar = scroll.verticalScrollbar;
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(1, 0);
            barRect.anchorMax = Vector2.one;
            barRect.offsetMin = new Vector2(-14, 8);
            barRect.offsetMax = new Vector2(-6, -8);
            if (bar.GetComponent<Image>() is Image track) track.color = new Color(1, 1, 1, .06f);
            bar.targetGraphic.color = new Color(.7f, .85f, .9f, .6f);
        }
        var item = dropdown.itemText.GetComponentInParent<Toggle>(true);
        var itemRect = (RectTransform)item.transform;
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = Vector2.one;
        itemRect.pivot = new Vector2(.5f, .5f);
        itemRect.sizeDelta = new Vector2(0, 40);
        itemRect.anchoredPosition = new Vector2(0, -20);
        // 默认模板只有 20 高，18 号字体加行距后会被 Text 自身裁空。
        var label = dropdown.itemText;
        label.fontSize = 18;
        label.lineSpacing = 1;
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 18;
        label.color = Ink;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(30, 3);
        label.rectTransform.offsetMax = new Vector2(-8, -3);
        label.raycastTarget = false;
        if (item.targetGraphic is Image selection)
        {
            selection.sprite = null;
            selection.type = Image.Type.Simple;
            selection.color = Color.white;
        }
        if (item.graphic != null) item.graphic.color = Ink;
        if (item.graphic is Image tick) tick.sprite = checkmark;
        var colors = item.colors;
        colors.normalColor = new Color(.75f, .85f, .9f, .025f);
        colors.highlightedColor = new Color(.2f, .6f, .75f, .32f);
        colors.selectedColor = new Color(.2f, .6f, .75f, .23f);
        colors.pressedColor = new Color(.2f, .6f, .75f, .42f);
        colors.fadeDuration = .1f;
        item.colors = colors;
    }

    private static void Stroke(Transform parent, string name, Vector2 position, float angle)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(7, 1.5f);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
        rect.GetComponent<Image>().color = Ink;
        rect.GetComponent<Image>().raycastTarget = false;
    }

    private static Sprite SpriteFor(string path, string sourceName, string targetName)
    {
        string module = string.Join("/", path.Split('/').Take(3));
        string source = "Assets/Examples/02_StarForce/GameMain/UI/UISprites/Common/" + sourceName;
        string target = module.Contains("02_StarForce") ? source
            : module.Contains("00_PointPointPoint") ? module + "/GameMain/UI/UISprites/" + targetName
            : "Assets/Examples/01_Modules/Common/UI/UISprites/" + targetName;
        if (sprites.TryGetValue(target, out var sprite)) return sprite;
        if (!File.Exists(target))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!AssetDatabase.CopyAsset(source, target)) throw new Exception("Could not copy UI sprite: " + target);
        }
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(target);
        if (sprite == null) throw new Exception("Missing UI sprite: " + target);
        sprites[target] = sprite;
        return sprite;
    }

    private static void Border(Image image, Sprite outline)
    {
        image.sprite = outline;
        image.type = Image.Type.Sliced;
        image.color = new Color(.86f, .94f, .98f, .85f);
        image.pixelsPerUnitMultiplier = 1.5f;
        image.fillCenter = true;
    }

    private static void StyleCommonButtons(GameObject root)
    {
        foreach (var button in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && c.GetType().FullName == "StarForce.CommonButton"))
        {
            bool primary = button.name == "Button_Start" || button.name == "Button_Confirm";
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            { image.color = primary ? Hex("C1F0FF") : Hex("B7D6E8"); }
            foreach (var text in button.GetComponentsInChildren<Text>(true)) text.color = Ink;
        }
    }

    private static void StyleStarForceMenu(GameObject root)
    {
        // 旧菜单按竖屏宽度缩放，横屏下标题和第四个按钮会超出画面。
        var background = root.transform.Find("Panel_MenuSurface") as RectTransform;
        if (background == null)
        {
            background = new GameObject("Panel_MenuSurface", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            background.SetParent(root.transform, false);
            background.SetAsFirstSibling();
        }
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = background.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(.015f, .04f, .07f, .2f);
        background.GetComponent<Image>().raycastTarget = false;
        var center = root.GetComponentsInChildren<RectTransform>(true).Single(rect => rect.name == "Panel_CenterScreen");
        center.anchorMin = new Vector2(.5f, 0);
        center.anchorMax = new Vector2(.5f, 1);
        center.anchoredPosition = Vector2.zero;
        center.sizeDelta = new Vector2(768, 0);
        var headline = center.Cast<Transform>().Single(item => item.name == "Panel_Headline") as RectTransform;
        headline.anchorMin = new Vector2(.08f, .58f);
        headline.anchorMax = new Vector2(.92f, .76f);
        headline.offsetMin = headline.offsetMax = Vector2.zero;
        var labels = headline.GetComponentsInChildren<Text>(true);
        for (int index = 0; index < labels.Length; index++)
        {
            var rect = (RectTransform)labels[index].transform;
            rect.anchorMin = new Vector2(0, index == 0 ? .42f : 0);
            rect.anchorMax = new Vector2(1, index == 0 ? 1 : .42f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            labels[index].fontSize = index == 0 ? 64 : 30;
            labels[index].fontStyle = index == 0 ? FontStyle.Bold : FontStyle.Normal;
            labels[index].resizeTextForBestFit = true;
            labels[index].resizeTextMinSize = index == 0 ? 36 : 18;
            labels[index].resizeTextMaxSize = labels[index].fontSize;
            labels[index].alignment = TextAnchor.MiddleCenter;
            labels[index].color = index == 0 ? Ink : Muted;
        }
        var buttons = center.Cast<Transform>().Single(item => item.name == "Panel_Buttons") as RectTransform;
        buttons.anchorMin = new Vector2(.2f, .2f);
        buttons.anchorMax = new Vector2(.8f, .54f);
        buttons.offsetMin = buttons.offsetMax = Vector2.zero;
        string[] names = { "Button_Start", "Button_Setting", "Button_About", "Button_Quit" };
        for (int index = 0; index < names.Length; index++)
        {
            var button = buttons.Cast<Transform>().Single(item => item.name == names[index]) as RectTransform;
            button.anchorMin = new Vector2(0, .8f - index * .25f);
            button.anchorMax = new Vector2(1, 1f - index * .25f);
            button.offsetMin = button.offsetMax = Vector2.zero;
            foreach (var image in button.GetComponentsInChildren<Image>(true))
            {
                var rect = (RectTransform)image.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            foreach (var label in button.GetComponentsInChildren<Text>(true))
            {
                var rect = (RectTransform)label.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                label.fontSize = 30;
                label.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private static void Colors(Selectable control)
    {
        var colors = control.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Hex("A8DEF4");
        colors.pressedColor = Hex("76ACC6");
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.45f, .58f, .66f, .45f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = .12f;
        control.colors = colors;
    }

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
}
