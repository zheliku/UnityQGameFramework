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
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateLocalizationExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use the isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "10_Localization" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null) cases = cases.Where(test => names.Contains(test.name)).ToList();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        var inputSettings = InputSystem.settings;
        var background = inputSettings.backgroundBehavior;
        var editorInput = inputSettings.editorInputBehaviorInPlayMode;
        var disabled = new HashSet<InputDevice>();
        var errors = new List<string>();
        var completed = new List<object>();
        Mouse mouse = null;
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (var test in cases)
            {
                string sourcePath = (string)test.layout["script"];
                string anchor = AssetDatabase.GUIDToAssetPath("546e1e2e8f2a49bdaeb03ad61edce340");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Debugger sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/10_Localization".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(ReadTranslation).Assembly.GetType("UnityQGameFramework.Examples." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                await Play(false);
                Require(GameEntry.GetComponent<LocalizationComponent>() == null, "No retained localization component after stop");
                Require(errors.Count == 0, test.name + " runtime errors: " + string.Join("\n", errors));
                completed.Add(new { example = test.name, scene = script.Substring(0, script.Length - 3) + ".unity", startMode = test.mode, assertions = assertions.Skip(start).ToArray() });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled)) InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = background;
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new { passed = true, unity = Application.unityVersion, nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown, Slider and InputField values set by the test; buttons and toggles use native mouse clicks",
            settingsUnchanged = true, scenes = completed, assertionCount = assertions.Count };
        Directory.CreateDirectory("Artifacts~/LocalizationExamples");
        string report = names == null ? "localization-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/LocalizationExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, InputField> input = key => (InputField)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        var localization = QGF.Localization;
        if (name == "ReadTranslation")
        {
            Require(localization.DictionaryCount == 18 && text("titleText").text == "我的飞行游戏", "Shared TOML parsed for current language");
            input("nameInput").text = "飞行员 + &";
            await Click(button("rewardButton"), mouse);
            await Click(button("rewardButton"), mouse);
            Require(text("rewardText").text == "飞行员 + & 获得 20 金币", "Two-argument translation updates live");
            Require(text("missingText").text.Contains("<NoKey>Unknown.Key"), "Missing translation marker visible");
            for (int count = 1; count <= 16; count++)
            {
                dropdown("argumentCount").value = count - 1;
                await Task.Delay(100);
                var expected = new List<string> { input("nameInput").text };
                if (count > 1) expected.Add("20");
                for (int index = 3; index <= count; index++) expected.Add(index.ToString());
                Require(text("formatText").text == string.Join(" | ", expected), "GetString overload with " + count + " arguments");
            }
            await Screenshot("localization-formatting");
        }
        else if (name == "SwitchLanguage")
        {
            Require(toggle("reloadToggle").isOn, "Language changes reload by default");
            await Click(button("action1Button"), mouse);
            Require(localization.GetString("Game.Name") == "My Flying Game", "English dictionary loaded");
            await Click(toggle("reloadToggle"), mouse);
            await Click(button("action2Button"), mouse);
            Require(localization.Language == UnityQGameFramework.Core.Localization.Language.ChineseSimplified
                && localization.GetString("Game.Name") == "My Flying Game", "Language selection alone preserves old strings");
            await Click(toggle("reloadToggle"), mouse);
            await Click(button("action2Button"), mouse);
            Require(localization.GetString("Game.Name") == "我的飞行游戏" && localization.DictionaryCount == 18,
                "Clear and parse replaces the active language without duplicate keys");
            await Screenshot("localization-language");
        }
        else if (name == "ParseDictionary")
        {
            int events = 0;
            EventHandler<UnityQGameFramework.Core.Event.GameEventArgs> listener = (_, args) => events++;
            QGF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, listener);
            try
            {
                for (int entry = 0; entry < 6; entry++)
                {
                    dropdown("inputMode").value = entry;
                    await Click(button("parseButton"), mouse);
                    Require(localization.DictionaryCount == 18 && text("stateText").text.Contains("解析完成"), "ParseData entry " + entry);
                    Require(localization.GetRawString("Reward.Coins") == "{0} 获得 {1} 金币", "Raw text retains placeholders");
                }
                Require(events == 0, "Synchronous ParseData does not raise ReadData events");
            }
            finally { QGF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, listener); }
            await Click(button("removeButton"), mouse);
            Require(!localization.HasRawString("Reward.Coins") && localization.DictionaryCount == 17
                && !button("removeButton").interactable, "Remove updates existence and count");
            await Click(button("reserveButton"), mouse);
            Require(localization.CachedBytesSize >= 4096, "EnsureCachedBytesSize reserves shared memory");
            await Click(button("freeButton"), mouse);
            Require(localization.CachedBytesSize == 0, "FreeCachedBytes releases the buffer");
            await Click(button("clearButton"), mouse);
            Require(localization.DictionaryCount == 0 && text("rawText").text.Contains("没有"), "Clear updates the raw view");
            await Click(button("parseButton"), mouse);
            await Screenshot("localization-parse");
        }
        else if (name == "EditDictionary")
        {
            input("keyInput").text = "";
            await Task.Delay(120);
            Require(!button("addButton").interactable && !button("removeButton").interactable, "Empty keys cannot be submitted");
            input("keyInput").text = "Runtime.Welcome";
            input("valueInput").text = "你好，{0}！";
            input("argumentInput").text = "飞行员";
            await Click(button("addButton"), mouse);
            Require(localization.HasRawString("Runtime.Welcome") && localization.DictionaryCount == 1, "Core manager shares the component dictionary");
            Require(text("stateText").text.Contains("字典 1 项") && text("stateText").text.Contains("已添加：Runtime.Welcome"),
                "Runtime insertion shows the live count and affected key");
            Require(text("rawText").text.Contains("{0}") && text("formattedText").text == "你好，飞行员！", "Raw and formatted values compared live");
            input("valueInput").text = "替换文本";
            await Click(button("addButton"), mouse);
            Require(localization.GetRawString("Runtime.Welcome") == "你好，{0}！" && text("stateText").text.Contains("已存在"), "Duplicate insertion preserves original value");
            await Click(button("removeButton"), mouse);
            Require(localization.GetRawString("Runtime.Welcome") == null && localization.DictionaryCount == 0, "Removed raw key returns null");
            Require(text("stateText").text.Contains("字典 0 项") && text("stateText").text.Contains("已移除：Runtime.Welcome")
                && text("formattedText").text.Length == 0, "Removal clears the formatted value and replaces the action");
            await Click(button("addButton"), mouse);
            Require(localization.GetString("Runtime.Welcome") == "替换文本", "Remove then add changes text");
            await Screenshot("localization-keys");
            await Click(button("clearButton"), mouse);
            Require(localization.DictionaryCount == 0, "Clear runtime keys");
            Require(text("stateText").text.Contains("已清空运行时字典") && !text("stateText").text.Contains("已添加"),
                "Clear replaces the insertion feedback");
            input("valueInput").text = "缺少参数 {1}";
            await Click(button("addButton"), mouse);
            Require(text("formattedText").text == "格式不匹配，请检查 {0} 占位符。", "Bad placeholder shows a compact diagnostic");
        }
        else if (name == "LoadDictionary")
        {
            for (int entry = 0; entry < 4; entry++)
            {
                dropdown("loadMode").value = entry;
                dropdown("languageMode").value = entry % 2;
                await Click(button("loadButton"), mouse);
                Require(Field<bool>(example, "loading") && !button("loadButton").interactable && localization.DictionaryCount == 0,
                    "ReadData is asynchronous and locks controls");
                await Until(() => !Field<bool>(example, "loading"), 6000, "Dictionary load " + entry);
                string expected = entry % 2 == 0 ? "我的飞行游戏" : "My Flying Game";
                Require(localization.GetString("Game.Name") == expected && text("stateText").text.Contains(expected), "ReadData result matches requested language");
                Require(text("contextText").text.Contains(entry >= 2 ? "UserData：当前对象" : "UserData：未设置"), "ReadData context entry " + entry);
            }
            await Click(toggle("failureToggle"), mouse);
            await Click(button("loadButton"), mouse);
            await Until(() => !Field<bool>(example, "loading"), 6000, "Malformed dictionary fails");
            Require(text("stateText").text.Contains("读取失败") && localization.DictionaryCount == 0 && text("contextText").text.Contains("失败回调"),
                "Parse failure raises matching failure event without partial keys");
            await Click(toggle("failureToggle"), mouse);
            await Click(button("loadButton"), mouse);
            await Until(() => !Field<bool>(example, "loading"), 6000, "Dictionary retry succeeds");
            Require(localization.DictionaryCount == 18 && button("loadButton").interactable, "Retry restores the dictionary and controls");
            await Screenshot("localization-load");
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(150);
            Require(QGF.Event.Count(LoadDictionarySuccessEventArgs.EventId) == 0 && QGF.Event.Count(LoadDictionaryFailureEventArgs.EventId) == 0,
                "Scene script releases success and failure subscriptions");
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
        Directory.CreateDirectory("Artifacts~/LocalizationExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/LocalizationExamples/" + name + ".png"));
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
