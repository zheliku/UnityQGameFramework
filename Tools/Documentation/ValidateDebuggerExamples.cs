using TMPro;
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
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateDebuggerExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use the isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "21_Debugger" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null)
            cases = cases.Where(test => names.Contains(test.name)).ToList();
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
                string anchor = AssetDatabase.GUIDToAssetPath("16e9a88cf2495a198884a80a014df5ef");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported Debugger sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/01_Modules".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(InspectDebuggerExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                var owned = test.name == "DebuggerWindowLifecycle" ? Field<object>(example, "window") : null;
                int beforeShutdown = owned == null ? 0 : Property<int>(owned, "ShutdownCalls");
                await Play(false);
                if (owned != null)
                    Require(Property<int>(owned, "ShutdownCalls") == beforeShutdown + 1, "Scene stop unregisters owned window exactly once");
                Require(GameEntry.GetComponent<DebuggerComponent>() == null, "No retained debugger component after stop");
                Require(errors.Count == 0, test.name + " runtime errors: " + string.Join("\n", errors));
                completed.Add(new
                {
                    example = test.name,
                    scene = script.Substring(0, script.Length - 3) + ".unity",
                    startMode = test.mode,
                    assertions = assertions.Skip(start).ToArray()
                });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled))
                InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = background;
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown, Slider and TMP_InputField values set by the test; buttons and toggles use native mouse clicks",
            logScope = "Conditional calls checked against example assembly symbols; with Info disabled, three Unity log fixtures exercise collection and list queries",
            settingsUnchanged = true,
            scenes = completed,
            assertionCount = assertions.Count
        };
        Directory.CreateDirectory("Artifacts~/DebuggerExamples");
        string report = names == null ? "debugger-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/DebuggerExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            result.unity,
            scenes = completed.Count,
            result.assertionCount,
            result.settingsUnchanged
        };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_Dropdown> dropdown = key => (TMP_Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_Text> text = key => (TMP_Text)data.FindProperty(key).objectReferenceValue;
        var debugger = QGF.Debugger;
        Require(debugger.ActiveWindow && debugger.ShowFullWindow, "Visible full debugger");
        CheckPlacement(debugger);
        if (name == "InspectDebuggerExample")
        {
            var manager = GameFrameworkEntry.GetModule<UnityQGameFramework.Core.IDebuggerManager>();
            foreach (int index in new[] { 1, 2, 0 })
            {
                dropdown("pageDropdown").value = index;
                string path = new[] { "Information/Other/Time", "Information/Other/Scene", "Information/Environment" }[index];
                Require(ReferenceEquals(SelectedLeaf(manager.DebuggerWindowRoot), debugger.GetDebuggerWindow(path)), "Select " + path);
            }
            var previous = SelectedLeaf(manager.DebuggerWindowRoot);
            dropdown("pageDropdown").value = 3;
            Require(ReferenceEquals(previous, SelectedLeaf(manager.DebuggerWindowRoot)) && text("resultText").text.Contains("未找到"), "Missing path leaves selection unchanged");
            dropdown("pageDropdown").value = 0;
            await Click(toggle("activeToggle"), mouse);
            Require(!debugger.ActiveWindow && !debugger.enabled, "Disable debugger entry");
            await Click(toggle("activeToggle"), mouse);
            Require(debugger.ActiveWindow && debugger.enabled, "Re-enable debugger entry");
            await Click(toggle("fullToggle"), mouse);
            Require(!debugger.ShowFullWindow && debugger.IconRect.width > 0, "Collapse to icon");
            await Click(toggle("fullToggle"), mouse);
            slider("scaleSlider").value = .8f;
            await Task.Delay(200);
            Require(debugger.WindowScale > 0 && debugger.WindowScale <= .8f + .001f, "Requested scale respects available width");
            CheckPlacement(debugger);
            await Click(button("resetButton"), mouse);
            Require(debugger.WindowScale == 1 && Math.Abs(debugger.WindowRect.x - 10) < 1 && debugger.IconRect == new Rect(10, 10, 60, 60), "ResetLayout restores defaults");
            await Click(button("positionButton"), mouse);
            CheckPlacement(debugger);
            await Click(button("pauseButton"), mouse);
            Require(QGF.Base.IsGamePaused && Time.timeScale == 0, "Pause visible game time");
            await Click(button("pauseButton"), mouse);
            Require(!QGF.Base.IsGamePaused && Time.timeScale > 0, "Resume game time");
            await Screenshot("debugger-layout");
            float originalSpeed = Field<float>(example, "originalSpeed");
            await Click(button("pauseButton"), mouse);
            UnityEngine.Object.Destroy(example);
            await Task.Delay(200);
            Require(QGF.Base.GameSpeed == originalSpeed && Time.timeScale == originalSpeed,
                "Destroying paused controller restores both Base and Unity time state");
        }
        else if (name == "DebuggerWindowLifecycle")
        {
            var window = Field<object>(example, "window");
            Require(debugger.GetDebuggerWindow("Examples/Counter") == null && !button("unregisterButton").interactable, "Window starts unregistered");
            slider("initialValueSlider").value = 7;
            await Click(button("registerButton"), mouse);
            Require(ReferenceEquals(debugger.GetDebuggerWindow("Examples/Counter"), window), "Query returns registered object");
            Require(Property<int>(window, "Count") == 7 && Property<int>(window, "InitializeCalls") == 1, "Initialize receives registration arguments once");
            Require(!button("registerButton").interactable, "Registered path cannot be registered twice");
            Require(Property<int>(window, "EnterCalls") == 0 && Property<int>(window, "LeaveCalls") == 0, "Programmatic initial selection does not invoke tab callbacks");
            await Click(button("incrementButton"), mouse);
            Require(Property<int>(window, "Count") == 8 && text("stateText").text.Contains("计数 8"), "uGUI updates live debugger state");
            await Click(button("timeButton"), mouse);
            float stopped = Property<float>(window, "ActiveSeconds");
            await Task.Delay(350);
            Require(Property<float>(window, "ActiveSeconds") == stopped, "Unselected window receives no OnUpdate");
            await Click(button("selectButton"), mouse);
            await Task.Delay(250);
            Require(Property<float>(window, "ActiveSeconds") > stopped, "Selected window resumes OnUpdate");
            Require(Property<int>(window, "EnterCalls") == 0 && Property<int>(window, "LeaveCalls") == 0, "Programmatic page changes leave tab counters unchanged");
            await Screenshot("debugger-counter");
            await Click(button("unregisterButton"), mouse);
            Require(debugger.GetDebuggerWindow("Examples/Counter") == null && Property<int>(window, "ShutdownCalls") == 1, "Unregister shuts down and removes exact path");
            Require(!debugger.SelectDebuggerWindow("Examples/Counter") && !debugger.UnregisterDebuggerWindow("Examples/Counter"), "Missing selection and unregister return false");
            slider("initialValueSlider").value = 2;
            await Click(button("registerButton"), mouse);
            Require(Property<int>(window, "Count") == 2 && Property<int>(window, "InitializeCalls") == 2, "Re-register resets state with new arguments");
            await Click(button("unregisterButton"), mouse);
            Require(Property<int>(window, "ShutdownCalls") == 2, "Second lifetime shuts down once");
            await Click(button("registerButton"), mouse); // 保留一份注册，检查场景销毁时的清理。
        }
        else if (name == "RecentDebuggerLogs")
        {
            var input = (TMP_InputField)data.FindProperty("messageInput").objectReferenceValue;
            var logs = new List<DebuggerComponent.LogNode>();
            bool infoCompiled = LogCompiled(example, "Info"), warningCompiled = LogCompiled(example, "Warning");
            input.text = "dbg-test-中文 + & =";
            await Click(button("infoButton"), mouse);
            debugger.GetRecentLogsNonAlloc(logs);
            Require(logs.Any(entry => entry.LogMessage == input.text && entry.LogType == LogType.Log) == infoCompiled,
                "QGF Info follows example assembly compilation symbols");
            input.text = "dbg-test-warning";
            await Click(button("warningButton"), mouse);
            debugger.GetRecentLogsNonAlloc(logs);
            Require(logs.Any(entry => entry.LogMessage == input.text && entry.LogType == LogType.Warning) == warningCompiled,
                "QGF Warning follows example assembly compilation symbols");
            if (logs.Count == 0)
                Require(text("logsText").text.Contains("QGF > Logging"), "Empty log view explains logging configuration");
            for (int index = 0; index < 3; index++)
            {
                input.text = "dbg-test-order-" + index;
                if (infoCompiled)
                    await Click(button("infoButton"), mouse);
                else
                {
                    // 日志宏关闭的宿主仍验证收集与查询；这三条由检查工具提供，不计作示例 Log 调用。
                    UnityEngine.Debug.Log(input.text);
                    await Task.Delay(150);
                }
            }
            slider("countSlider").value = 2;
            await Task.Delay(150);
            logs.Add(new DebuggerComponent.LogNode());
            debugger.GetRecentLogsNonAlloc(logs, 2);
            Require(logs.Count == 2 && logs[0].LogMessage == "dbg-test-order-1" && logs[1].LogMessage == "dbg-test-order-2", "Count query replaces list with latest entries in order");
            Require(logs.All(entry => entry.LogFrameCount > 0 && Math.Abs((entry.LogTime - DateTime.UtcNow).TotalSeconds) < 30), "Captured frame and UTC time");
            Require(text("logsText").text.Contains("dbg-test-order-2") && !text("logsText").text.Contains("dbg-test-order-0"), "Live bounded log view replaces old results");
            dropdown("queryMode").value = 1;
            await Task.Delay(150);
            debugger.GetRecentLogsNonAlloc(logs);
            Require(logs.Count(entry => entry.LogMessage.StartsWith("dbg-test-")) == 3 + (infoCompiled ? 1 : 0) + (warningCompiled ? 1 : 0)
                && text("logsText").text.Contains("dbg-test-中文") == infoCompiled, "All query matches enabled log levels and fixture entries");
            Require(!slider("countSlider").interactable, "All log mode disables the count control");
            Require(Field<List<DebuggerComponent.LogNode>>(example, "logs").Count == 0, "Example retains copied display text only");
            await Screenshot("debugger-logs");
        }
    }

    private static bool LogCompiled(MonoBehaviour example, string method)
    {
        var defines = UnityEditor.Compilation.CompilationPipeline.GetAssemblies()
            .Single(assembly => assembly.name == example.GetType().Assembly.GetName().Name).defines;
        var conditions = typeof(Log).GetMethod(method, new[] { typeof(string) })
            .GetCustomAttributes<System.Diagnostics.ConditionalAttribute>().Select(attribute => attribute.ConditionString);
        return conditions.Any(defines.Contains);
    }

    private static void CheckPlacement(DebuggerComponent debugger)
    {
        var rect = debugger.WindowRect;
        float scale = debugger.WindowScale;
        Require(rect.xMin * scale >= -1 && rect.xMax * scale <= Screen.width + 2
            && rect.yMin * scale >= -1 && rect.yMax * scale <= Screen.height + 2, "Rendered debugger fits viewport");
        Require(Screen.width < Screen.height ? rect.yMin * scale >= Screen.height * .5f
            : rect.xMin * scale >= Screen.width * .5f, "Debugger is separate from uGUI controls");
    }

    private static T Property<T>(object instance, string name) => (T)instance.GetType().GetProperty(name).GetValue(instance);

    private static UnityQGameFramework.Core.IDebuggerWindow SelectedLeaf(UnityQGameFramework.Core.IDebuggerWindowGroup root)
    {
        UnityQGameFramework.Core.IDebuggerWindow window = root;
        while (window is UnityQGameFramework.Core.IDebuggerWindowGroup group)
            window = group.SelectedWindow;
        return window;
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

    private static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
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
            if (overflow > 0)
                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
        }
        bool received = false;
        UnityEngine.Events.UnityAction observed = () => received = true;
        var button = target as Button;
        var toggle = target as Toggle;
        bool oldValue = toggle != null && toggle.isOn;
        if (button != null)
            button.onClick.AddListener(observed);
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
        Directory.CreateDirectory("Artifacts~/DebuggerExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/DebuggerExamples/" + name + ".png"));
        await Task.Delay(160);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        assertions.Add(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
                completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = enabled;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
