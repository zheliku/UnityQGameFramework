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
using UnityEngine.SceneManagement;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateSceneExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "16_Scene" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("bdd615d3777d46c487aeeb6777122b02");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Scene sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/16_Scene".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(LoadRoomExample).Assembly.GetType("UnityQGameFramework.Examples." + test.name)
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
            modeAndTextSetup = "Dropdown.value and InputField.text set by the test; actions use native mouse clicks",
            settingsUnchanged = true, scenes = completed, assertionCount = assertions.Count };
        Directory.CreateDirectory("Artifacts~/SceneExamples");
        string report = names == null ? "scene-examples-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/SceneExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }



    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        if (name == "LoadRoomExample")
        {
            string room = Field<string>(example, "room");
            Require(dropdown("loadMode").options.Count == 4 && dropdown("unloadMode").options.Count == 2, "Four load and two unload choices");
            Require(QGF.Scene.HasScene(room) && !QGF.Scene.SceneIsLoaded(room), "Resource exists without being loaded");
            Require(!QGF.Scene.HasScene("Assets/QgfSceneMissing.unity"), "Missing resource query");
            Require(button("loadButton").interactable && !button("unloadButton").interactable, "Initial controls");
            for (int load = 0; load < 4; load++)
            for (int unload = 0; unload < 2; unload++)
            {
                dropdown("loadMode").value = load;
                dropdown("unloadMode").value = unload;
                dropdown("queryMode").value = load % 2;
                bool sawLoading = false, sawUnloading = false;
                UnityEngine.Events.UnityAction observeLoad = () => sawLoading = QGF.Scene.SceneIsLoading(room) && QGF.Scene.GetLoadingSceneAssetNames().Contains(room);
                UnityEngine.Events.UnityAction observeUnload = () => sawUnloading = QGF.Scene.SceneIsUnloading(room) && QGF.Scene.GetUnloadingSceneAssetNames().Contains(room);
                button("loadButton").onClick.AddListener(observeLoad);
                button("unloadButton").onClick.AddListener(observeUnload);
                try
                {
                    await Click(button("loadButton"), mouse);
                    await Until(() => QGF.Scene.SceneIsLoaded(room), 5000, "Load overload " + load);
                    await Task.Delay(130);
                    Require(sawLoading, "Pending load query " + load);
                    Require(!QGF.Scene.SceneIsLoading(room) && QGF.Scene.GetLoadedSceneAssetNames().SequenceEqual(new[] { room }), "Loaded collection " + load);
                    Require(SceneManager.sceneCount == 2 && SceneManager.GetSceneByPath(room).isLoaded, "Real additive room loaded " + load);
                    Require(text("stateText").text.Contains(load >= 2 ? "UserData：当前对象" : "UserData：未设置"), "Load user data " + load);
                    Require(((Slider)data.FindProperty("progressSlider").objectReferenceValue).value == 1, "Progress reaches one on success");
                    Require(!button("loadButton").interactable && button("unloadButton").interactable, "Loaded controls");
                    if (load % 2 == 1)
                        Require(Field<List<string>>(example, "loaded").SequenceEqual(new[] { room }), "List query mirrors loaded array");
                    Require(text("roomsText").text.Contains("已加载 1"), "Live scene count");
                    await Screenshot("loaded-room");
                    await Click(button("unloadButton"), mouse);
                    await Until(() => !QGF.Scene.SceneIsLoaded(room) && !QGF.Scene.SceneIsUnloading(room), 5000, "Unload overload " + unload);
                    await Task.Delay(130);
                    Require(sawUnloading, "Pending unload query " + unload);
                    Require(SceneManager.sceneCount == 1 && QGF.Scene.GetLoadedSceneAssetNames().Length == 0, "Room content actually removed");
                    Require(text("stateText").text.Contains(unload == 1 ? "UserData：当前对象" : "UserData：未设置"), "Unload user data " + unload);
                }
                finally
                {
                    button("loadButton").onClick.RemoveListener(observeLoad);
                    button("unloadButton").onClick.RemoveListener(observeUnload);
                }
            }
        }
        else if (name == "SwitchRoomsExample")
        {
            await Until(() => !Field<bool>(example, "changing") && Field<string>(example, "current") != null, 6000, "Initial room ready");
            var events = new List<string>();
            EventHandler<UnityQGameFramework.Core.Event.GameEventArgs> loaded = (sender, args) => events.Add("Load:" + ((LoadSceneSuccessEventArgs)args).SceneAssetName);
            EventHandler<UnityQGameFramework.Core.Event.GameEventArgs> unloaded = (sender, args) => events.Add("Unload:" + ((UnloadSceneSuccessEventArgs)args).SceneAssetName);
            QGF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, loaded);
            QGF.Event.Subscribe(UnloadSceneSuccessEventArgs.EventId, unloaded);
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    string before = Field<string>(example, "current");
                    events.Clear();
                    await Click(button("switchButton"), mouse);
                    await Until(() => !Field<bool>(example, "changing"), 6000, "Switch completes " + index);
                    string after = Field<string>(example, "current");
                    Require(before != after && after != null, "Room changed " + index);
                    Require(events.SequenceEqual(new[] { "Unload:" + before, "Load:" + after }), "Unload precedes load " + index);
                    Require(!QGF.Scene.SceneIsLoaded(before) && QGF.Scene.SceneIsLoaded(after), "Only new room retained " + index);
                    Require(SceneManager.sceneCount == 2, "Launcher plus one room " + index);
                    Require(text("stateText").text.Contains("切换完成"), "Live switch state " + index);
                }
                if (Field<string>(example, "current").EndsWith("BlueRoom.unity", StringComparison.Ordinal))
                {
                    await Click(button("switchButton"), mouse);
                    await Until(() => !Field<bool>(example, "changing"), 6000, "Final orange room for screenshot");
                }
                await Screenshot("switch-room");
            }
            finally
            {
                QGF.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, loaded);
                QGF.Event.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, unloaded);
            }
        }
        else if (name == "SceneOrderExample")
        {
            string blue = Field<string>(example, "blue"), orange = Field<string>(example, "orange");
            await Until(() => QGF.Scene.SceneIsLoaded(blue) && QGF.Scene.SceneIsLoaded(orange), 5000, "Both rooms loaded");
            Require(SceneManager.sceneCount == 3, "Launcher plus two rooms");
            Require(SceneManager.GetActiveScene().path == orange, "Order set during loading chooses maximum");
            foreach (string target in new[] { "blue", "orange" })
            {
                await Click(button(target + "Button"), mouse);
                string path = target == "blue" ? blue : orange;
                Require(SceneManager.GetActiveScene().path == path, "Active order " + target);
                await Click(button("spawnButton"), mouse);
                Require(GameObject.Find("Born in " + SceneComponent.GetSceneName(path)).scene.path == path, "New object belongs to active scene " + target);
                Require(text("stateText").text.Contains(SceneComponent.GetSceneName(path)), "Active scene state visible " + target);
            }
            var camera = Field<Camera>(example, "sceneCamera");
            Require(QGF.Scene.MainCamera == camera, "Main camera cached");
            await Click(toggle("cameraTagToggle"), mouse);
            Require(camera.tag == "Untagged" && QGF.Scene.MainCamera == null, "Refresh drops untagged camera");
            await Click(toggle("cameraTagToggle"), mouse);
            Require(QGF.Scene.MainCamera == camera, "Refresh restores MainCamera");
            await Screenshot("scene-order");
            QGF.Scene.UnloadScene(orange);
            await Until(() => !QGF.Scene.SceneIsUnloading(orange), 4000, "Unload active room");
            Require(SceneManager.GetActiveScene().path == blue, "Fallback to remaining room");
            QGF.Scene.UnloadScene(blue);
            await Until(() => !QGF.Scene.SceneIsUnloading(blue), 4000, "Unload remaining room");
            Require(SceneManager.GetActiveScene().path.EndsWith("SceneOrderExample.unity", StringComparison.Ordinal), "Fallback to framework scene");
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
        Directory.CreateDirectory("Artifacts~/SceneExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/SceneExamples/" + name + ".png"));
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
