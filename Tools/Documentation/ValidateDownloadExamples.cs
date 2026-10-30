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
using FileInfo = System.IO.FileInfo;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateDownloadExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "19_Download" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null)
            cases = cases.Where(test => names.Contains(test.name)).ToList();
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
                string anchor = AssetDatabase.GUIDToAssetPath("c939d5324e4b51d2854cd7c1f8104eef");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported Download sample not found.");
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
                var type = typeof(DownloadFileExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                var runtimeType = example.GetType();
                string ownedPath = runtimeType.GetField("path", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(example) as string;
                string ownedDirectory = runtimeType.GetField("directory", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(example) as string;
                await Play(false);
                Require(ownedPath == null || (!File.Exists(ownedPath) && !File.Exists(ownedPath + ".download")), "Example files cleaned after stop");
                Require(ownedDirectory == null || Directory.GetFiles(ownedDirectory).Length == 0, "Queue files cleaned after stop");
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
        Directory.CreateDirectory("Artifacts~/DownloadExamples");
        string report = names == null ? "download-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/DownloadExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, TMP_Text> text = key => (TMP_Text)data.FindProperty(key).objectReferenceValue;
        Require(QGF.Download.TotalAgentCount == 1 && QGF.Download.GetAllDownloadInfos().Length == 0, "One agent; no implicit downloads");
        string path = name == "DownloadQueue" ? null : Field<string>(example, "path");
        byte[] payload = name == "DownloadQueue" ? null : Field<TextAsset>(example, "payload").bytes;
        if (name == "DownloadFileExample")
        {
            Require(dropdown("requestMode").options.Count == 8, "Eight AddDownload parameter combinations");
            for (int i = 0; i < 8; i++)
            {
                dropdown("requestMode").value = i;
                await Click(button("downloadButton"), mouse);
                await Until(() => Field<int>(example, "serial") == 0, 4000, "File download completes " + i);
                Require(File.ReadAllBytes(path).SequenceEqual(payload) && !File.Exists(path + ".download"), "Exact disk content, temporary promoted to final " + i);
                await Until(() => text("pathText").text.Contains("正式文件：已保存 · 临时文件：无"), 1000, "Saved file state is readable " + i);
                Require(text("contentText").text == Field<TextAsset>(example, "payload").text.Trim(), "Actual file shown in GUI " + i);
            }
            await Screenshot("download-file");
            await Click(button("clearButton"), mouse);
            Require(!File.Exists(path) && !File.Exists(path + ".download"), "Clear removes only example files");
        }
        else if (name == "DownloadProgressExample")
        {
            int original = QGF.Download.FlushSize;
            for (int i = 0; i < 3; i++)
            {
                dropdown("flushMode").value = i;
                await Click(button("downloadButton"), mouse);
                await Until(() => Field<long>(example, "received") > 0, 4000, "Streaming bytes visible");
                long received = Field<long>(example, "received");
                Require(received < payload.Length && File.Exists(path + ".download") && !File.Exists(path), "Live partial file before completion " + i);
                var progress = (Slider)data.FindProperty("progressSlider").objectReferenceValue;
                Require(progress.value > 0 && progress.value < 1 && text("stateText").text.Contains("字节"), "Real percentage and byte count " + i);
                Require(text("fileText").text.Contains("正式文件：尚未生成"), "Partial download state is readable " + i);
                Require(QGF.Download.FlushSize == original, "Global flush restored " + i);
                if (i == 0)
                    await Screenshot("download-progress");
                await Until(() => Field<int>(example, "serial") == 0, 7000, "Streaming completes " + i);
                Require(Field<bool>(example, "matched") && File.ReadAllBytes(path).SequenceEqual(payload), "Payload exact after streamed disk write " + i);
                await Until(() => text("fileText").text.Contains("正式文件：已保存"), 1000, "Completed download state is readable " + i);
            }
        }
        else if (name == "CancelDownloadExample")
        {
            await Click(button("downloadButton"), mouse);
            await Until(() => Field<long>(example, "received") > 16384, 4000, "Received enough data before cancelling");
            int first = Field<int>(example, "serial");
            await Click(button("cancelButton"), mouse);
            long saved = new FileInfo(path + ".download").Length;
            Require(saved > 0 && saved < payload.Length && !QGF.Download.GetDownloadInfo(first).IsValid && !File.Exists(path), "Cancel closes stream and retains partial; serial invalid");
            await Until(() => text("fileText").text.Contains("临时文件：可续传"), 1000, "Retained partial file is visible as resumable");
            await Screenshot("download-cancel");
            await Click(button("downloadButton"), mouse);
            await Until(() => Field<long>(example, "resumedFrom") > 0, 3000, "Resume start event reports existing bytes");
            Require(Field<long>(example, "resumedFrom") == saved && Field<DownloadHttpServer>(example, "server").LastRange == saved, "Real HTTP Range begins at saved length");
            await Until(() => Field<int>(example, "serial") == 0, 7000, "Resume completes");
            Require(Field<bool>(example, "matched") && File.ReadAllBytes(path).SequenceEqual(payload) && !File.Exists(path + ".download"), "Resumed final file exact, no duplicate prefix");
            await Until(() => text("fileText").text.Contains("正式文件：已保存 · 临时文件：无"), 1000, "Resumed file state is readable");
            await Screenshot("download-resume");
            await Click(button("downloadButton"), mouse);
            dropdown("cancelMode").value = 1;
            await Click(button("cancelButton"), mouse);
            Require(!File.Exists(path) && !File.Exists(path + ".download"), "Cancel with cleanup discards partial");
            await Click(button("downloadButton"), mouse);
            await Until(() => Field<DownloadHttpServer>(example, "server").LastRange == -1, 3000, "Clean retry has no Range");
            Require(Field<long>(example, "resumedFrom") == 0, "Clean retry begins at zero");
            await Click(button("cancelButton"), mouse);
        }
        else if (name == "DownloadQueue")
        {
            var toggle = (Toggle)data.FindProperty("pausedToggle").objectReferenceValue;
            await Click(button("enqueueButton"), mouse);
            Require(QGF.Download.Paused && QGF.Download.WorkingAgentCount == 0 && QGF.Download.WaitingTaskCount == 3, "Paused before enqueue, no request starts");
            Require(text("queueText").text.Contains("等待中"), "Paused queue shows waiting states");
            Require(Field<List<TaskInfo>>(example, "tasks").Count == 3, "The queue lists all pending downloads");
            await Click(toggle, mouse);
            await Until(() => text("queueText").text.Contains("执行中"), 1000, "Started task state is readable");
            Require(Field<List<string>>(example, "order").SequenceEqual(new[] { "high" }), "Priority 10 first although added second");
            await Click(toggle, mouse);
            await Task.Delay(1200);
            Require(QGF.Download.Paused && Field<int>(example, "callbacks") == 1 && Field<List<string>>(example, "order").Count == 1,
                "Pause does not stop current HTTP response; next tasks remain queued");
            await Screenshot("download-queue");
            await Click(toggle, mouse);
            await Until(() => QGF.Download.GetAllDownloadInfos().Length == 0, 5000, "Remaining downloads complete");
            Require(string.Join(",", Field<List<string>>(example, "order")) == "high,other,low" && Field<int>(example, "callbacks") == 3, "Actual priority order and three completions");
            string dir = Field<string>(example, "directory");
            Require(new[] { "low", "high", "other" }.All(n => File.ReadAllBytes(Path.Combine(dir, n + ".bin")).SequenceEqual(new byte[] { 1, 2, 3, 4 })), "Each queue task owns separate exact disk file");
            await Click(button("enqueueButton"), mouse);
            Require(QGF.Download.Paused && QGF.Download.WaitingTaskCount == 3, "Second batch waits until resumed");
            await Click(button("cancelButton"), mouse);
            Require(text("resultText").text == "low 任务已移除。", "Serial cancellation result is readable");
            Require(!QGF.Download.GetDownloadInfo(Field<int>(example, "low")).IsValid
                && QGF.Download.GetAllDownloadInfos().Length == 2 && Field<int>(example, "callbacks") == 0,
                "Removing low emits no completion callback and leaves other tasks queued");
            await Click(toggle, mouse);
            await Until(() => QGF.Download.GetAllDownloadInfos().Length == 0, 5000, "Remaining downloads complete after cancellation");
            Require(string.Join(",", Field<List<string>>(example, "order")) == "high,other"
                && Field<int>(example, "callbacks") == 2, "Remaining downloads retain priority order");
        }
        else if (name == "DownloadFailure")
        {
            float previous = QGF.Download.Timeout;
            for (int i = 0; i < 5; i++)
            {
                dropdown("failureMode").value = i;
                await Click(button("downloadButton"), mouse);
                await Until(() => Field<int>(example, "serial") == 0, 5000, "Failure/retry ends " + i);
                await Task.Delay(80); // 等 LateUpdate 把结果值写到界面。
                Require(QGF.Download.Timeout == previous, "Default timeout restored " + i);
                if (i < 4)
                    Require(File.ReadAllText(path) == "existing file", "Failure preserves previous final file " + i);
                string result = text("stateText").text;
                Require(result.Contains(i == 0 ? "404" : i == 1 ? "Timeout" : i == 2 ? "requested byte range" : i == 3 ? "416" : "内容一致"), "Actual error or success reason " + i + ": " + result);
                string expectedFileState = i < 4 ? "正式文件：旧文件仍保留" : "正式文件：新文件已保存";
                Require(text("fileText").text.Contains(expectedFileState), "Final file state is readable " + i);
                if (i == 0)
                    Require(new FileInfo(path + ".download").Length == 0, "404 page never written as partial asset");
                if (i == 1)
                    Require(Field<float>(example, "elapsed") >= .9f, "No-data timeout waits one real second");
                if (i == 2)
                {
                    Require(File.ReadAllBytes(path + ".download").SequenceEqual(payload.Take(8)), "Ignored Range rejected before appending any full-response byte");
                    await Screenshot("download-range-failure");
                }
                if (i == 3)
                    Require(!File.Exists(path + ".download"), "416 deletes unusable temporary file");
                if (i == 4)
                    Require(File.ReadAllBytes(path).SequenceEqual(payload) && !File.Exists(path + ".download"), "Successful retry replaces old final with exact payload");
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
        Directory.CreateDirectory("Artifacts~/DownloadExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/DownloadExamples/" + name + ".png"));
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
