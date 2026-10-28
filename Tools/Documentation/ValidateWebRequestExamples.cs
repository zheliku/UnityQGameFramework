using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
public static class ValidateWebRequestExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "18_WebRequest" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("4db2bf82437d5556b15f973d14498ab1");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported WebRequest sample not found.");
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
                var type = typeof(GetWebRequestExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
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
        Directory.CreateDirectory("Artifacts~/WebRequestExamples");
        string report = names == null ? "web-request-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/WebRequestExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        Require(QGF.WebRequest.TotalAgentCount == 1, "One real HTTP agent");
        Require(QGF.WebRequest.GetAllWebRequestInfos().Length == 0 && UsedForms() == 0, "No implicit requests or held contexts");
        if (name == "GetWebRequestExample")
        {
            string expected = Field<TextAsset>(example, "response").text.Trim();
            Require(dropdown("requestMode").options.Count == 8, "All eight GET parameter combinations available");
            for (int i = 0; i < 8; i++)
            {
                dropdown("requestMode").value = i;
                await Click(button("requestButton"), mouse);
                await Until(() => Field<int>(example, "serial") == 0, 4000, "GET completes " + i);
                Require(text("responseText").text == expected && text("stateText").text.Contains("完成"), "GET body is exact JSON " + i);
                Require(UsedForms() == 0, "GET releases context " + i);
            }
            await Screenshot("web-get");
        }
        else if (name == "FailedWebRequestExample")
        {
            float original = QGF.WebRequest.Timeout;
            for (int i = 0; i < 3; i++)
            {
                dropdown("failureMode").value = i;
                await Click(button("requestButton"), mouse);
                await Until(() => Field<int>(example, "serial") == 0, 5000, "Failure/retry completes " + i);
                Require(QGF.WebRequest.Timeout == original, "Global timeout restored " + i);
                Require(text("resultText").text.Contains(i == 0 ? "404" : i == 1 ? "Timeout" : "服务器已响应"), "Actual HTTP/timeout/retry result " + i);
                if (i == 1)
                    Require(Field<float>(example, "elapsed") >= .9f && Field<float>(example, "elapsed") < 4, "Timeout uses one second of actual waiting");
                Require(UsedForms() == 0, "Failure/success releases context " + i);
                if (i == 1)
                    await Screenshot("web-timeout");
            }
        }
        else if (name == "CancelWebRequestExample")
        {
            await Click(button("enqueueButton"), mouse);
            await Until(() => QGF.WebRequest.WaitingTaskCount == 3, 3000, "Three tasks queued behind hold");
            int hold = Field<int>(example, "hold");
            Require(QGF.WebRequest.GetWebRequestInfo(hold).Status == UnityQGameFramework.Core.TaskStatus.Doing, "Hold already running before high priority is added");
            Require(text("queueText").text.Contains("执行中") && text("queueText").text.Contains("等待中"), "Queue shows running and waiting states");
            Require(QGF.WebRequest.GetWebRequestInfos("Example.Queue").Length == 3, "Tagged query contains running and queued tasks");
            await Until(() => Field<List<TaskInfo>>(example, "tasks").Count == 4, 1000, "All active requests are listed");
            await Screenshot("web-queue");
            await Click(button("cancelButton"), mouse);
            Require(text("resultText").text == "hold 请求已取消。", "Serial cancellation result is readable");
            Require(!QGF.WebRequest.GetWebRequestInfo(hold).IsValid, "Cancelled serial invalid");
            await Until(() => QGF.WebRequest.GetAllWebRequestInfos().Length == 0, 6500, "Remaining tasks finish by priority");
            Require(string.Join(",", Field<List<string>>(example, "order")) == "hold,high,other,low", "High priority does not preempt hold; waiting tasks ordered high/other/low");
            Require(Field<int>(example, "callbacks") == 3 && UsedForms() == 0, "Cancelled hold has no result; three completions return all contexts");
        }
        else if (name == "PostWebRequest")
        {
            var input = (InputField)data.FindProperty("messageInput").objectReferenceValue;
            string special = "中文🚀 + & = % # ?\n第二行";
            input.text = special;
            Require(dropdown("requestMode").options.Count == 8, "Eight POST parameter combinations for both body kinds");
            for (int entry = 0; entry < 8; entry++)
            {
                dropdown("requestMode").value = entry;
                bool withTag = new[] { 1, 4, 5, 7 }.Contains(entry);
                bool withPriority = new[] { 2, 4, 6, 7 }.Contains(entry);
                bool withContext = new[] { 3, 5, 6, 7 }.Contains(entry);
                foreach (string kind in new[] { "bytesButton", "formButton" })
                {
                    int completed = Field<int>(example, "completed");
                    await Click(button(kind), mouse);
                    await Until(() => Field<int>(example, "serial") == 0, 4000, "POST completes " + kind + " entry " + entry);
                    string response = text("responseText").text;
                    Require(response.Contains("POST /echo") && response.EndsWith("解码内容：" + special), "Server received unchanged Unicode, newline and form punctuation " + kind + " entry " + entry);
                    Require(response.Contains(kind == "bytesButton" ? "application/octet-stream" : "application/x-www-form-urlencoded"), "POST content type " + kind);
                    Require(text("requestText").text.Contains("标签：" + (withTag ? "Example.Post" : "未设置"))
                        && text("requestText").text.Contains("优先级：" + (withPriority ? 10 : 0)), "Actual task metadata matches selected overload " + kind + " entry " + entry);
                    Require(text("contextText").text.Contains(withContext ? "上下文：当前组件" : "上下文：null（未传入）")
                        && Field<int>(example, "completed") == completed + 1, "Callback returns the selected business context once " + kind + " entry " + entry);
                    Require(UsedForms() == 0, "POST releases context " + kind + " entry " + entry);
                }
            }
            foreach (string message in new[] { "", "second value" })
            {
                input.text = message;
                foreach (string kind in new[] { "bytesButton", "formButton" })
                {
                    await Click(button(kind), mouse);
                    await Until(() => Field<int>(example, "serial") == 0, 4000, "Empty/plain POST " + kind);
                    Require(text("responseText").text.EndsWith("解码内容：" + message), "Empty/plain body preserves submitted value " + kind);
                }
            }
            await Click(toggle("slowToggle"), mouse);
            foreach (int entry in new[] { 0, 7 })
            {
                dropdown("requestMode").value = entry;
                foreach (string kind in new[] { "bytesButton", "formButton" })
                {
                    int completed = Field<int>(example, "completed");
                    await Click(button(kind), mouse);
                    int pending = Field<int>(example, "serial");
                    await Until(() => QGF.WebRequest.GetWebRequestInfo(pending).IsValid
                        && QGF.WebRequest.GetWebRequestInfo(pending).Status == UnityQGameFramework.Core.TaskStatus.Doing,
                        2000, "Slow POST running before cancellation " + entry);
                    Require(!button("bytesButton").interactable && !button("formButton").interactable && !dropdown("requestMode").interactable,
                        "In-flight POST locks body and parameter selection");
                    if (entry == 0 && kind == "bytesButton")
                        await Screenshot("web-post-pending");
                    await Click(button("cancelButton"), mouse);
                    Require(Field<int>(example, "serial") == 0 && !QGF.WebRequest.GetWebRequestInfo(pending).IsValid && UsedForms() == 0,
                        "Cancel by serial clears tagged and untagged POST contexts " + kind + " entry " + entry);
                    await Task.Delay(3200);
                    Require(Field<int>(example, "completed") == completed && text("stateText").text.Contains("已取消"),
                        "Cancelled POST has no result callback or stale response " + kind + " entry " + entry);
                }
            }
            await Click(toggle("slowToggle"), mouse);
            dropdown("requestMode").value = 7;
            input.text = "你好 QGF + &= ?";
            await Click(button("formButton"), mouse);
            await Until(() => Field<int>(example, "serial") == 0, 4000, "Form screenshot response");
            await Screenshot("web-post");
            var server = Field<LocalHttpServer>(example, "server");
            byte[] sent = { 0, 255, 128, 38, 61, 43, 10, 0 };
            byte[] received = null;
            int id = 0;
            EventHandler<GameEventArgs> handler = (sender, args) =>
            {
                var result = (WebRequestSuccessEventArgs)args;
                if (result.SerialId == id)
                    received = (byte[])result.GetWebResponseBytes().Clone();
            };
            QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, handler);
            try
            {
                id = QGF.WebRequest.AddWebRequest(server.Url("/echo-bytes"), sent);
                await Until(() => received != null, 4000, "Raw binary roundtrip completes");
                Require(sent.SequenceEqual(received), "Raw POST preserves zero, invalid UTF-8 and form punctuation bytes");
            }
            finally { QGF.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, handler); }
            int before = UsedForms();
            for (int i = 0; i < 8; i++)
            {
                int pending = QGF.WebRequest.AddWebRequest(server.Url("/hold"), new WWWForm(), new object());
                Require(QGF.WebRequest.RemoveWebRequest(pending) && UsedForms() == before, "Cancel pending form releases internal entry " + i);
            }
            try
            {
                QGF.WebRequest.AddWebRequest("", new WWWForm(), new object());
            }
            catch (GameFrameworkException) { }
            Require(UsedForms() == before, "Failed Add returns its context");

            // 只销毁请求拥有者，框架继续运行；其他业务的独立请求仍应完成。
            await Click(toggle("slowToggle"), mouse);
            dropdown("requestMode").value = 0;
            await Click(button("bytesButton"), mouse);
            int owned = Field<int>(example, "serial");
            bool otherCompleted = false;
            int other = 0;
            EventHandler<GameEventArgs> otherHandler = (sender, args) =>
            {
                var success = (WebRequestSuccessEventArgs)args;
                if (success.SerialId == other)
                    otherCompleted = Encoding.UTF8.GetString(success.GetWebResponseBytes()) == "other owner";
            };
            using (var independent = new LocalHttpServer(Encoding.UTF8.GetBytes("other owner")))
            {
                QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, otherHandler);
                try
                {
                    other = QGF.WebRequest.AddWebRequest(independent.Url("/status.json"), "Other.Owner");
                    UnityEngine.Object.Destroy(example.gameObject);
                    await Until(() => example == null, 2000, "POST owner destroyed while framework remains active");
                    await Until(() => !QGF.WebRequest.GetWebRequestInfo(owned).IsValid && otherCompleted, 5000,
                        "Owner cleanup removes its request and preserves another owner's request");
                    Require(QGF.WebRequest.GetAllWebRequestInfos().Length == 0 && UsedForms() == 0, "Owner cleanup leaves no requests or pooled contexts");
                }
                finally { QGF.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, otherHandler); }
            }
        }
    }

    private static int UsedForms() => ReferencePool.GetAllReferencePoolInfos().Where(info => info.Type.Name == "WWWFormInfo").Sum(info => info.UsingReferenceCount);

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
        Directory.CreateDirectory("Artifacts~/WebRequestExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/WebRequestExamples/" + name + ".png"));
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
