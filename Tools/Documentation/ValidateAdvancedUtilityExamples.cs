using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

// 仅供隔离工程验收。按钮使用输入系统鼠标，文本与 Dropdown 是测试输入。
public static class ValidateAdvancedUtilityExamples
{
    private static readonly List<string> assertions = new List<string>();
    private static readonly string[] Names = { "DrawRandom", "FindLoadedTypes", "ConvertStructure" };

    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Use the isolated idle validation project.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)) throw new Exception("Unsaved scene.");
        assertions.Clear();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
        };
        var completed = new List<object>();
        bool enabled = EditorSettings.enterPlayModeOptionsEnabled, background = Application.runInBackground;
        var options = EditorSettings.enterPlayModeOptions;
        var inputSettings = InputSystem.settings;
        var oldBackground = inputSettings.backgroundBehavior;
        var oldEditorInput = inputSettings.editorInputBehaviorInPlayMode;
        var disabled = new HashSet<InputDevice>();
        Mouse mouse = null;
        // 保存临时选项，连接或程序集重载中断验证时可据此恢复。
        Directory.CreateDirectory("Artifacts~/AdvancedUtilityExamples");
        File.WriteAllText("Artifacts~/AdvancedUtilityExamples/active-run.json", JsonConvert.SerializeObject(new {
            active = true, startedUtc = DateTime.UtcNow, enabled, options, background,
            inputBackground = oldBackground, inputEditor = oldEditorInput, originalScene = setup
        }, Formatting.Indented));
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (string name in Names)
            {
                string anchor = AssetDatabase.GUIDToAssetPath("e638c6e0da015f4d99bdc5c5491facc0");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Base sample not imported.");
                string script = Path.GetDirectoryName(anchor).Replace('\\', '/') + ((string)layouts[name]["script"]).Substring("Assets/Examples/01_Modules".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(400);
                Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityQGameFramework.Examples.Modules." + name)).First(t => t != null);
                var owner = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(owner != null, name + " saved controller");
                var data = new SerializedObject(owner);
                foreach (var field in ((JArray)layouts[name]["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, name + "." + field + " saved reference");
                int start = assertions.Count;
                await Check(name, owner, data, mouse);
                await Play(false);
                Require(errors.Count == 0, name + " no runtime errors: " + string.Join("\n", errors));
                completed.Add(new { name, scene = script.Substring(0, script.Length - 3) + ".unity", assertions = assertions.Skip(start).ToArray() });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled)) InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = oldBackground;
            inputSettings.editorInputBehaviorInPlayMode = oldEditorInput;
            EditorSettings.enterPlayModeOptionsEnabled = enabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            Application.logMessageReceived -= capture;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Artifacts~/AdvancedUtilityExamples/active-run.json", JsonConvert.SerializeObject(new { active = false, stoppedUtc = DateTime.UtcNow }, Formatting.Indented));
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new { passed = true, unity = Application.unityVersion, assertionCount = assertions.Count, scenes = completed,
            nativeButtons = true, testInputs = "Dropdown and InputField values assigned by test; Global random seed explicitly applied; shared native cache reserve/free explicitly clicked", settingsUnchanged = true };
        Directory.CreateDirectory("Artifacts~/AdvancedUtilityExamples");
        File.WriteAllText("Artifacts~/AdvancedUtilityExamples/advanced-utility-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, result.assertionCount, scenes = completed.Count, result.settingsUnchanged };
    }

    private static async Task Check(string name, MonoBehaviour owner, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = field => (Button)data.FindProperty(field).objectReferenceValue;
        Func<string, Dropdown> dropdown = field => (Dropdown)data.FindProperty(field).objectReferenceValue;
        Func<string, InputField> input = field => (InputField)data.FindProperty(field).objectReferenceValue;
        Func<string, Text> text = field => (Text)data.FindProperty(field).objectReferenceValue;
        if (name == "DrawRandom")
        {
            Require(dropdown("mode").options.Count == 5, "Five random operations grouped");
            input("seedInput").text = "123";
            input("minInput").text = "1";
            input("maxInput").text = "7";
            for (int mode = 0; mode < 5; mode++)
            {
                dropdown("mode").value = mode;
                await Click(button("seedButton"), mouse);
                string first = text("resultText").text;
                Require(text("seedText").text.Contains("全局种子 123"), "Explicit global seed mode=" + mode);
                var expected = new System.Random(123);
                for (int draw = 0; draw < 3; draw++)
                {
                    if (draw > 0) await Click(button("drawButton"), mouse);
                    string result = text("resultText").text;
                    string value;
                    if (mode == 3) value = expected.NextDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    else if (mode == 4)
                    {
                        var bytes = new byte[8];
                        expected.NextBytes(bytes);
                        value = BitConverter.ToString(bytes);
                    }
                    else value = (mode == 0 ? expected.Next() : mode == 1 ? expected.Next(7) : expected.Next(1, 7)).ToString();
                    Require(result.Contains(mode == 3 ? "GetRandomDouble()：" + value : mode == 4 ? "GetRandomBytes(buffer)：" + value : "GetRandom：" + value + "\n"), "Known seeded sequence mode=" + mode + " draw=" + draw);
                    var marker = Field<RectTransform>(owner, "markerRect");
                    var track = (RectTransform)marker.parent;
                    Require(marker.anchoredPosition.x >= -0.1f && marker.anchoredPosition.x + marker.rect.width <= track.rect.width + 0.1f, "Random marker stays in visible track mode=" + mode);
                }
                Require(!text("seedText").text.Contains("第一次"), "Seed status stays valid after subsequent draws mode=" + mode);
                await Click(button("seedButton"), mouse);
                Require(text("resultText").text == first, "Seed and call order reproduce result mode=" + mode);
            }
            dropdown("mode").value = 1;
            input("maxInput").text = "0";
            await Click(button("drawButton"), mouse);
            Require(text("resultText").text.Contains("GetRandom：0\n"), "Zero upper bound returns zero");
            input("maxInput").text = "-1";
            await Click(button("drawButton"), mouse);
            Require(text("resultText").text.Contains("不能小于 0"), "Negative max rejected visibly");
            dropdown("mode").value = 2;
            input("minInput").text = "-5";
            input("maxInput").text = "-5";
            await Click(button("drawButton"), mouse);
            Require(text("resultText").text.Contains("GetRandom：-5\n"), "Equal integer boundaries return boundary value");
            input("maxInput").text = "-1";
            await Click(button("drawButton"), mouse);
            int drawn = int.Parse(text("resultText").text.Split('\n')[0].Split('：')[1]);
            Require(drawn >= -5 && drawn < -1, "Negative integer range honors both boundaries");
            input("minInput").text = "2";
            await Click(button("drawButton"), mouse);
            Require(text("resultText").text.Contains("不大于 max"), "Reversed range rejected visibly");
            input("seedInput").text = "";
            await Click(button("seedButton"), mouse);
            Require(text("seedText").text.Contains("请输入整数"), "Empty global seed rejected visibly");
            Require(input("minInput").interactable && input("maxInput").interactable, "Range inputs enabled only for range operation");
            dropdown("mode").value = 3;
            Require(!input("minInput").interactable && !input("maxInput").interactable, "Unused range inputs disabled for doubles");
        }
        else if (name == "FindLoadedTypes")
        {
            input("typeInput").text = "UnityQGameFramework.Runtime.BaseComponent";
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.Contains("类型：UnityQGameFramework.Runtime.BaseComponent")
                && text("typeText").text.Contains("程序集：UnityQGameFramework.Runtime") && text("typeText").text.Contains("继承 MonoBehaviour：是"), "Full name resolves Base helper entry type");
            input("typeInput").text = "System.String";
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.Contains("类型：System.String") && text("typeText").text.Contains("继承 MonoBehaviour：否"), "Non-MonoBehaviour loaded type query");
            input("typeInput").text = typeof(BaseComponent).AssemblyQualifiedName;
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.Contains("类型：UnityQGameFramework.Runtime.BaseComponent"), "Assembly-qualified loaded type query");
            input("typeInput").text = "System.String[";
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.Contains("未找到类型") || text("typeText").text.Contains("格式有误"), "Malformed type name stays in visible result");
            input("typeInput").text = "No.Such.QgfType";
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.StartsWith("未找到类型"), "Missing type returns null visibly");
            input("typeInput").text = " ";
            await Click(button("findButton"), mouse);
            Require(text("typeText").text.Contains("包含命名空间"), "Blank type name guidance");
            input("filterInput").text = "UnityQGameFramework.Examples.Modules.DrawRandom";
            await Click(button("listButton"), mouse);
            Require(text("listText").text.Contains("筛选匹配 1 个") && text("listText").text.EndsWith("UnityQGameFramework.Examples.Modules.DrawRandom"), "Type enumeration array/List and filtered match");
            input("filterInput").text = "unityqgameframework.examples.drawrandom";
            await Click(button("listButton"), mouse);
            Require(text("listText").text.Contains("筛选匹配 1 个"), "Case-insensitive name filtering");
            input("filterInput").text = "No.Such.QgfType";
            await Click(button("listButton"), mouse);
            Require(text("listText").text.Contains("筛选匹配 0 个") && !text("listText").text.Contains("Examples.DrawRandom"), "New enumeration replaces prior display");
        }
        else if (name == "ConvertStructure")
        {
            input("idInput").text = "-123";
            input("speedInput").text = "3.5";
            string expected = BitConverter.ToString(BitConverter.GetBytes(-123).Concat(BitConverter.GetBytes(3.5f)).ToArray());
            for (int mode = 0; mode < 3; mode++)
            {
                dropdown("mode").value = mode;
                await Click(button("convertButton"), mouse);
                string result = text("resultText").text;
                Require(result.Contains("结构体 8 字节") && result.Contains(mode == 2 ? "缓冲区：00-00-" + expected : "缓冲区：" + expected), "Fixed blittable layout and exact bytes mode=" + mode);
                Require(result.Contains("还原编号：-123") && result.EndsWith("还原速度：3.5"), "Structure roundtrip mode=" + mode);
            }
            await Click(button("reserveButton"), mouse);
            Require(UnityQGameFramework.Core.Utility.Marshal.CachedHGlobalSize >= 8192, "Explicit cache reserve grows to requested capacity");
            await Click(button("freeButton"), mouse);
            Require(UnityQGameFramework.Core.Utility.Marshal.CachedHGlobalSize == 0 && text("cacheText").text.Contains("：0 字节"), "Explicit free releases shared native cache");
            await Click(button("convertButton"), mouse);
            Require(UnityQGameFramework.Core.Utility.Marshal.CachedHGlobalSize >= 8 && text("resultText").text.EndsWith("还原速度：3.5"), "Conversion reacquires cache after release");
            input("idInput").text = "";
            await Click(button("convertButton"), mouse);
            Require(text("resultText").text.Contains("编号请输入整数"), "Blank record ID rejected visibly");
            input("idInput").text = "42";
            foreach (string invalid in new[] { "NaN", "Infinity", "oops" })
            {
                input("speedInput").text = invalid;
                await Click(button("convertButton"), mouse);
                Require(text("resultText").text.Contains("有限小数"), "Invalid record speed rejected: " + invalid);
            }
        }
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

    private static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
        Require(condition(), message);
    }

    private static async Task Click(Selectable target, Mouse mouse)
    {
        await Until(() => target.interactable, 4000, "Enabled control " + target.name);
        Canvas.ForceUpdateCanvases();
        await Task.Delay(150);
        var rect = (RectTransform)target.transform;
        var scroll = target.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            float offset = scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y;
            if (overflow > 0) scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
        }
        bool received = false;
        UnityEngine.Events.UnityAction observed = () => received = true;
        var button = target as Button;
        var toggle = target as Toggle;
        bool oldValue = toggle != null && toggle.isOn;
        if (button != null) button.onClick.AddListener(observed);
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(150);
            if (!(button != null ? received : toggle != null && toggle.isOn != oldValue))
            {
                await Screenshot("failed-click");
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                throw new Exception("Native click " + target.name + " at " + position + "; raycast " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            }
            Require(true, "Native click " + target.name);
        }
        finally { if (button != null) button.onClick.RemoveListener(observed); }
    }

    private static async Task Screenshot(string name)
    {
        foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }
        Canvas.ForceUpdateCanvases();
        await Task.Delay(160);
        Directory.CreateDirectory("Artifacts~/AdvancedUtilityExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/AdvancedUtilityExamples/" + name + ".png"));
        await Task.Delay(160);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions.Add(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = enabled;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task) throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
