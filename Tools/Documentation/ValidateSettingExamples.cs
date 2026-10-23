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
public static class ValidateSettingExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "09_Setting" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null) cases = cases.Where(test => names.Contains(test.name)).ToList();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool originalBackground = Application.runInBackground;
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
                string anchor = AssetDatabase.GUIDToAssetPath("636842cd577841a587a4696ab4a69cb9");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Setting sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/09_Setting".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(SaveCounter).Assembly.GetType("UnityQGameFramework.Examples." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                string file = SettingExampleHelper.ExamplePath;
                QGF.Setting.SetString("ShutdownCheck", test.name);
                await Play(false);
                Require(File.Exists(file), "Normal shutdown saves the settings file");
                await Play(true);
                await Task.Delay(400);
                Require(QGF.Setting.GetString("ShutdownCheck", "") == test.name, "Next startup loads the shutdown value");
                await Play(false);
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
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new { passed = true, unity = Application.unityVersion, nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown, Slider and InputField values set by the test; buttons and toggles use native mouse clicks",
            settingsUnchanged = true, scenes = completed, assertionCount = assertions.Count };
        Directory.CreateDirectory("Artifacts~/SettingExamples");
        string report = names == null ? "setting-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/SettingExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, InputField> input = key => (InputField)data.FindProperty(key).objectReferenceValue;
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        var settings = QGF.Setting;
        var manager = FrameworkEntry.GetModule<UnityQGameFramework.Core.Setting.ISettingManager>();
        if (name == "SaveCounter")
        {
            Require(settings.GetInt("LaunchCount", 0) > 0, "Startup counter was loaded and incremented");
            input("nameInput").text = "飞行员 + &";
            slider("volumeSlider").value = .25f;
            var muted = (Toggle)data.FindProperty("muteToggle").objectReferenceValue;
            if (!muted.isOn) await Click(muted, mouse);
            await Click(button("saveButton"), mouse);
            Require(settings.GetString("PlayerName") == "飞行员 + &" && settings.GetBool("Muted") && settings.GetFloat("Volume") == .25f,
                "String, bool and float saved through UI");
            int count = settings.GetInt("LaunchCount");
            input("nameInput").text = "未保存的姓名";
            slider("volumeSlider").value = .75f;
            if (muted.isOn) await Click(muted, mouse);
            await Task.Delay(100);
            Require(text("valuesText").text.Contains("当前设置：飞行员 + & · 静音 开 · 音量")
                && text("valuesText").text.Contains("输入框：未保存的姓名 · 静音 关 · 音量"),
                "Saved settings and unsaved input stay visible together");
            settings.SetFloat("Volume", .9f);
            await Click(button("reloadButton"), mouse);
            Require(settings.GetFloat("Volume") == .25f && input("nameInput").text == "飞行员 + &"
                && slider("volumeSlider").value == .25f && muted.isOn, "Reload button restores saved data and all input controls");
            Require(settings.GetInt("LaunchCount") == count && text("resultText").text.Contains("重新读取"),
                "Reload keeps startup counter and shows result");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var temporary = UnityEngine.SceneManagement.SceneManager.CreateScene("SettingsProbe_" + Guid.NewGuid().ToString("N"));
            try
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(temporary);
                settings.SetFloat("Volume", .9f);
                await Click(button("reloadButton"), mouse);
                Require(settings.GetFloat("Volume") == .25f && slider("volumeSlider").value == .25f,
                    "Reload keeps the original file when another scene becomes active");
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
                var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(temporary);
                while (unload != null && !unload.isDone) await Task.Delay(25);
            }
            await Click(button("resetVolumeButton"), mouse);
            Require(!settings.HasSetting("Volume") && settings.GetFloat("Volume", .5f) == .5f && slider("volumeSlider").value == .5f,
                "Removing the key restores its explicit default");
            slider("volumeSlider").value = .9f;
            await Click(button("reloadButton"), mouse);
            Require(!settings.HasSetting("Volume") && slider("volumeSlider").value == .5f, "Reload preserves removed key and refreshes its default");
            await Click(button("resetButton"), mouse);
            Require(!settings.HasSetting("LaunchCount"), "Reset removes the counter only");
            Require(settings.GetString("PlayerName") == "飞行员 + &", "Reset preserves unrelated preferences");
            await Screenshot("setting-values");
        }
        else if (name == "SaveSettingObject")
        {
            settings.RemoveSetting("Pilot");
            await Task.Delay(120);
            foreach (int writer in new[] { 0, 1 })
            {
                dropdown("writeMode").value = writer;
                input("nameInput").text = "对象" + writer;
                slider("levelSlider").value = 7 + writer;
                await Click(button("saveButton"), mouse);
                Require(manager.Load(), "Object file reloads");
                for (int reader = 0; reader < 4; reader++)
                {
                    dropdown("readMode").value = reader;
                    await Click(button("readButton"), mouse);
                    var loaded = Field<SaveSettingObject.Pilot>(example, "loaded");
                    Require(loaded.Name == "对象" + writer && loaded.Level == 7 + writer,
                        "Object writer " + writer + " reader " + reader);
                    loaded.Level = 99;
                    Require(settings.GetObject<SaveSettingObject.Pilot>("Pilot").Level == 7 + writer,
                        "GetObject returns a new deserialized object");
                }
            }
            await Click(button("removeButton"), mouse);
            foreach (int reader in new[] { 0, 1 })
            {
                dropdown("readMode").value = reader;
                await Task.Delay(120);
                Require(!button("readButton").interactable, "Missing object prevents a read without a default");
            }
            foreach (int reader in new[] { 2, 3 })
            {
                dropdown("readMode").value = reader;
                await Click(button("readButton"), mouse);
                Require(ReferenceEquals(Field<SaveSettingObject.Pilot>(example, "loaded"), Field<SaveSettingObject.Pilot>(example, "fallback")),
                    "Default object is returned by reference");
                Require(!settings.HasSetting("Pilot"), "Default reads do not create a key");
            }
            await Screenshot("setting-object");
        }
        else if (name == "ManageSettings")
        {
            settings.RemoveAllSettings();
            settings.Save();
            input("keyInput").text = "";
            await Task.Delay(120);
            Require(!button("writeButton").interactable && !button("removeButton").interactable, "Empty key controls disabled");
            foreach (string key in new[] { "Language", "Music" })
            {
                input("keyInput").text = key;
                input("valueInput").text = "原始值";
                await Click(button("writeButton"), mouse);
            }
            Require(settings.Count == 2 && text("keysText").text.Contains("Language"), "Live names and count reflect memory");
            Require(manager.Load() && settings.Count == 0, "Set changes memory before Save");
            await Click(button("writeButton"), mouse);
            input("valueInput").text = "覆盖值";
            await Click(button("writeButton"), mouse);
            Require(settings.Count == 1 && settings.GetString("Music") == "覆盖值", "Set overwrites an existing key");
            Require(text("keysText").text.Contains("Music = 覆盖值") && !text("keysText").text.Contains("原始值"),
                "The key list shows the current value after overwrite");
            input("keyInput").text = "Language";
            await Click(button("writeButton"), mouse);
            foreach (int query in new[] { 0, 1 })
            {
                dropdown("queryMode").value = query;
                await Task.Delay(120);
                Require(Field<List<string>>(example, "names").SequenceEqual(new[] { "Language", "Music" }), "Array and List query match");
            }
            await Click(button("saveButton"), mouse);
            Require(text("stateText").text.Contains("已保存当前设置"), "Save provides visible feedback");
            settings.RemoveAllSettings();
            Require(manager.Load() && settings.Count == 2, "Save persists both keys");
            await Click(button("removeButton"), mouse);
            Require(!settings.HasSetting("Language") && settings.HasSetting("Music"), "Remove only selected key");
            Require(text("stateText").text.Contains("已移除：Language") && !text("keysText").text.Contains("Language ="),
                "Removal updates the action and key list");
            await Screenshot("setting-keys");
            await Click(button("clearButton"), mouse);
            Require(settings.Count == 0, "RemoveAllSettings clears the dictionary");
            Require(text("stateText").text.Contains("已清空") && text("keysText").text == "当前没有设置。",
                "Clear replaces the previous action and values");
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
        Directory.CreateDirectory("Artifacts~/SettingExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/SettingExamples/" + name + ".png"));
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
