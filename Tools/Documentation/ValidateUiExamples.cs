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
public static class ValidateUiExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "14_UI" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("7706eaef9f335c84a3ac20756fdea0de");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported UI sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/01_Modules".Length);
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
                var type = typeof(OpenWindowExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
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
            modeAndTextSetup = "Dropdown.value and InputField.text set by the test; actions use native mouse clicks",
            settingsUnchanged = true,
            scenes = completed,
            assertionCount = assertions.Count
        };
        Directory.CreateDirectory("Artifacts~/UiExamples");
        string report = names == null ? "ui-examples-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/UiExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<int> serial = () => Field<int>(example, "serialId");
        if (name == "OpenWindowExample")
        {
            Require(QGF.UI.IsLoadingUIForm(serial()) && !QGF.UI.HasUIForm(serial()), "Opening is asynchronous");
            Require(button("cancelButton").interactable && !button("closeButton").interactable, "Loading controls");
            await Click(button("cancelButton"), mouse);
            Require(!QGF.UI.IsLoadingUIForm(serial()), "Cancel removes pending request");
            await Task.Delay(2200);
            Require(QGF.UI.GetAllLoadedUIForms().Length == 0, "Cancelled window never appears");
            int firstInstance = 0, previousSerial = serial();
            for (int i = 0; i < 8; i++)
            {
                dropdown("openMode").value = i;
                dropdown("closeMode").value = i % 4;
                await Click(button("openButton"), mouse);
                Require(serial() > previousSerial, "New open serial " + i);
                previousSerial = serial();
                await Until(() => QGF.UI.HasUIForm(serial()), 6000, "Open overload " + i);
                var form = QGF.UI.GetUIForm(serial());
                var view = (OpenWindowExampleView)form.Logic;
                Require(QGF.UI.IsValidUIForm(form) && view.Available && view.Visible, "Valid window " + i);
                Require(view.InitCalls == 1 && view.OpenCalls == i + 1, "One init and one open per request " + i);
                Require(view.HasOpenData == new[] { 3, 5, 6, 7 }.Contains(i), "Open user data " + i);
                Require(form.PauseCoveredUIForm == new[] { 2, 4, 6, 7 }.Contains(i), "Pause-covered parameter " + i);
                if (firstInstance == 0)
                    firstInstance = form.gameObject.GetInstanceID();
                Require(form.gameObject.GetInstanceID() == firstInstance, "Closed window reused " + i);
                if (i == 7)
                    await Screenshot("01-open");
                await Click(button("closeButton"), mouse);
                Require(!QGF.UI.HasUIForm(previousSerial) && !QGF.UI.IsValidUIForm(form), "Close removes identity " + i);
                Require(!view.Available && !view.Visible && view.CloseData == (i % 2 == 1 ? "保存背包" : null), "Close lifecycle data " + i);
                Require(text("resultText").text.Contains(i % 2 == 1 ? "保存背包" : "无"), "Close event data " + i);
            }
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(100);
            Require(QGF.Event.Count(OpenUIFormFailureEventArgs.EventId) == 0 && QGF.Event.Count(CloseUIFormCompleteEventArgs.EventId) == 0,
                "Window event handlers removed on destroy");
        }
        else if (name == "ReopenWindowExample")
        {
            await Until(() => QGF.UI.HasUIForm(serial()), 6000, "Initial reuse window");
            await Until(() => text("stateText").text.Contains("首次创建"), 1000, "Initial creation shown in UI");
            var slider = (Slider)data.FindProperty("idleSlider").objectReferenceValue;
            int oldSerial = serial(), instance = QGF.UI.GetUIForm(serial()).gameObject.GetInstanceID();
            slider.value = 0;
            await Click(button("reopenButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(serial()) && !Field<bool>(example, "changing"), 6000, "Immediate reopen");
            Require(serial() != oldSerial && QGF.UI.GetUIForm(serial()).gameObject.GetInstanceID() == instance, "New serial, same instance");
            Require(((ReopenWindowExampleView)QGF.UI.GetUIForm(serial()).Logic).OpenCalls == 2, "Reused window receives OnOpen");
            await Until(() => text("stateText").text.Contains("复用上次实例"), 1000, "Instance reuse shown in UI");
            var reused = (ReopenWindowExampleView)QGF.UI.GetUIForm(serial()).Logic;
            Require(reused.InitCalls == 1 && reused.CloseCalls == 1 && reused.RecycleCalls == 1,
                "Reopen keeps initialization and records close/recycle once");
            slider.value = 2.3f;
            await Click(button("reopenButton"), mouse);
            Require(!QGF.UI.HasUIForm(serial()) && text("stateText").text.Contains("等待"), "Closed during idle");
            await Until(() => QGF.UI.HasUIForm(serial()) && !Field<bool>(example, "changing"), 6000, "Expired window reopens");
            int rebuilt = QGF.UI.GetUIForm(serial()).gameObject.GetInstanceID();
            Require(rebuilt != instance && ((ReopenWindowExampleView)QGF.UI.GetUIForm(serial()).Logic).OpenCalls == 1, "Expired instance replaced");
            await Until(() => text("stateText").text.Contains("新建实例"), 1000, "Expired instance replacement shown in UI");
            await Click(toggle("lockedToggle"), mouse);
            await Click(button("reopenButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(serial()) && !Field<bool>(example, "changing"), 6000, "Locked window reopens");
            Require(QGF.UI.GetUIForm(serial()).gameObject.GetInstanceID() == rebuilt, "Lock prevents idle expiry");
            await Screenshot("02-reused");
            await Click(toggle("lockedToggle"), mouse);
            await Click(button("reopenButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(serial()) && !Field<bool>(example, "changing"), 6000, "Unlocked window reopens");
            Require(QGF.UI.GetUIForm(serial()).gameObject.GetInstanceID() != rebuilt, "Unlock restores expiry");
            await Click(button("reopenButton"), mouse);
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(2800);
            Require(QGF.UI.GetAllLoadedUIForms().Length == 0 && QGF.UI.GetAllLoadingUIFormSerialIds().Length == 0, "Destroy cancels idle reshow");
        }
        else if (name == "CoverWindowExample")
        {
            int bottom = Field<int>(example, "bottom");
            await Until(() => QGF.UI.HasUIForm(bottom), 6000, "Bottom window opens");
            var lower = QGF.UI.GetUIForm(bottom);
            var view = (CoverWindowExampleView)lower.Logic;
            int initialReveals = view.RevealCalls, initialResumes = view.ResumeCalls;
            toggle("pauseToggle").isOn = false;
            await Click(button("openButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(Field<int>(example, "top")), 6000, "Top window opens without pause");
            var upper = QGF.UI.GetUIForm(Field<int>(example, "top"));
            float elapsed = view.ActiveSeconds;
            await Task.Delay(220);
            Require(view.Visible && view.ActiveSeconds > elapsed && view.CoverCalls == 1 && view.PauseCalls == 0, "Covered window keeps updating when not paused");
            Require(text("stateText").text.Contains("背包：显示") && text("callbacksText").text.Contains("覆盖 / 揭开"), "Covered window state shown in UI");
            Require(upper.DepthInUIGroup > lower.DepthInUIGroup, "Top window canvas depth");
            await Screenshot("03-covered");
            await Click(button("closeButton"), mouse);
            Require(view.Visible && view.RevealCalls == initialReveals + 1 && QGF.UI.HasUIForm(bottom), "Close reveals existing bottom");
            toggle("pauseToggle").isOn = true;
            await Click(button("openButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(Field<int>(example, "top")), 6000, "Top window opens with pause");
            elapsed = view.ActiveSeconds;
            await Task.Delay(220);
            Require(!view.Visible && view.ActiveSeconds == elapsed && view.PauseCalls == 1, "Paused bottom hides and stops OnUpdate");
            Require(text("stateText").text.Contains("背包：隐藏"), "Paused window hidden state shown in UI");
            await Screenshot("03-paused");
            await Click(button("refocusButton"), mouse);
            Require(view.Visible && view.RefocusCalls == 1 && view.RefocusData == "再次查看背包" && view.ResumeCalls == initialResumes + 1, "Refocus resumes and passes data");
            Require(QGF.UI.GetUIGroup("Example").CurrentUIForm.SerialId == bottom && lower.SerialId == bottom, "Refocus changes ordering, preserves identity");
            await Click(button("closeButton"), mouse);
            await Click(button("openButton"), mouse);
            await Until(() => QGF.UI.HasUIForm(Field<int>(example, "top")), 6000, "Top opens for no-data refocus");
            await Click(toggle("contextToggle"), mouse);
            await Click(button("refocusButton"), mouse);
            Require(view.RefocusCalls == 2 && view.RefocusData == null, "No-data refocus overload");
        }
        else if (name == "InspectWindows")
        {
            await Until(() => QGF.UI.GetAllLoadedUIForms().Length == 2, 6000, "Two grouped windows");
            int hud = Field<int>(example, "hud"), popup = Field<int>(example, "popup");
            var lower = QGF.UI.GetUIForm(hud);
            var upper = QGF.UI.GetUIForm(popup);
            Require(QGF.UI.UIGroupCount == 2 && lower.UIGroup != upper.UIGroup && lower.Logic.Visible && upper.Logic.Visible, "Two independent groups");
            for (int i = 0; i < 14; i++)
            {
                dropdown("queryMode").value = i;
                await Click(button("queryButton"), mouse);
                string result = text("queryText").text;
                if (i < 4)
                    Require(result.Contains("Hud") && result.Contains("Popup"), "Array/List loaded query " + i);
                else if (i == 4 || i == 11)
                    Require(result.Contains("已打开") && result.Contains((i == 4 ? hud : popup).ToString()), "Identity and asset queries " + i);
                else if (i < 7)
                    Require(result.Contains("Hud：1") && result.Contains("Popup：1")
                    && result.Contains("DefaultUIGroupHelper"), "Array/List group query and actual helper " + i);
                else if (i < 11)
                    Require(result.Contains("Popup") && !result.Contains("Hud"), "Group-scoped query " + i);
                else
                    Require(result == "待加载：无", "Empty loading snapshot " + i);
            }
            await Click(toggle("pauseToggle"), mouse);
            Require(!upper.Logic.Visible && lower.Logic.Visible, "Pause affects only Popup group");
            await Click(toggle("pauseToggle"), mouse);
            Require(upper.Logic.Visible, "Group resume restores Popup");
            await Click(toggle("depthToggle"), mouse);
            Require(upper.GetComponent<Canvas>().sortingOrder < lower.GetComponent<Canvas>().sortingOrder, "Group depth changes canvas order");
            await Screenshot("04-depth");
            await Click(toggle("depthToggle"), mouse);
            Require(upper.GetComponent<Canvas>().sortingOrder > lower.GetComponent<Canvas>().sortingOrder, "Group depth restored");
            foreach (bool context in new[] { true, false })
            {
                toggle("contextToggle").isOn = context;
                var views = QGF.UI.GetAllLoadedUIForms().Select(f => (InspectWindowsView)f.Logic).ToArray();
                await Click(button("closeButton"), mouse);
                Require(QGF.UI.GetAllLoadedUIForms().Length == 0 && QGF.UI.UIGroupCount == 2, "Close-all keeps empty groups");
                Require(views.All(v => v.CloseData == (context ? "退出菜单" : null)), "Close-all user data " + context);
                await Click(button("restoreButton"), mouse);
                await Until(() => QGF.UI.GetAllLoadedUIForms().Length == 2, 6000, "Restore grouped windows");
            }
        }
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
        Require(target.interactable, "Enabled control " + target.name);
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
        Directory.CreateDirectory("Artifacts~/UiExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/UiExamples/" + name + ".png"));
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
