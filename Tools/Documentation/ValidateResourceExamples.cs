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

/// <summary>
/// 用输入系统的鼠标事件检查资源示例，不随 Sample 导入。
/// </summary>
public static class ValidateResourceExamples
{
    private static readonly List<string> assertions = new List<string>();
    private static readonly List<string> expectedHttpErrors = new List<string>();
    private static string expectedHttpErrorPrefix;

    /// <summary>
    /// 检查全部资源示例。
    /// </summary>
    /// <returns>资源示例检查结果。</returns>
    public static Task<object> Run()
    {
        return RunOnly(null);
    }

    /// <summary>
    /// 检查指定资源示例。
    /// </summary>
    /// <param name="names">示例名称，为空时检查全部示例。</param>
    /// <returns>资源示例检查结果。</returns>
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
        expectedHttpErrors.Clear();
        expectedHttpErrorPrefix = null;
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "12_Resource" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .SelectMany(pair => Enumerable.Range(0, pair.Name == "UpdateRemoteResources" ? 3 : 1)
                .Select(mode => new { name = pair.Name, layout = pair.Value, mode })).ToList();
        if (names != null)
            cases = cases.Where(test => names.Contains(test.name)).ToList();
        if (cases.Any(test => test.name == "UpdateRemoteResources"))
        {
            UnityQGameFramework.Examples.Modules.Editor.UpdateRemoteResourcesInspector.BuildResources();
        }
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
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        File.WriteAllText("Artifacts~/ResourceExamples/active-run.json", JsonConvert.SerializeObject(new
        {
            active = true,
            startedUtc = DateTime.UtcNow,
            optionsEnabled,
            options,
            originalBackground,
            inputBackground = background,
            inputEditor = editorInput,
            originalScene = setup
        }, Formatting.Indented));
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
            {
                // 只接受故障开关期间、本次本地服务器返回的 Bundle 404；其他错误仍使检查失败。
                if (kind == LogType.Error && expectedHttpErrorPrefix != null
                    && message.Contains("[UnityWebRequestFile] Request failed.")
                    && message.Contains("URL: '" + expectedHttpErrorPrefix)
                    && message.Contains(".bundle', HttpCode=404,"))
                {
                    expectedHttpErrors.Add(message);
                    return;
                }
                errors.Add(message + "\n" + stack);
            }
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
                string anchor = AssetDatabase.GUIDToAssetPath("1fde9459f6765bdeacb1340f310a5a10");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported Resource sample not found.");
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
                var type = typeof(LoadResourcePackage).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
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
                Require(GameEntry.GetComponent<ResourceComponent>() == null, "No retained resource component after stop");
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
            expectedHttpErrorPrefix = null;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Artifacts~/ResourceExamples/active-run.json", JsonConvert.SerializeObject(new
            {
                active = false,
                stoppedUtc = DateTime.UtcNow
            }, Formatting.Indented));
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown, Slider and InputField values set by the test; buttons and toggles use native mouse clicks",
            settingsUnchanged = true,
            scenes = completed,
            assertionCount = assertions.Count,
            expectedHttp404Errors = expectedHttpErrors
        };
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        string report = names == null ? "resource-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/ResourceExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        var resource = QGF.Resource;
        if (name == "LoadCubePrefab")
        {
            await Until(() => Field<GameObject>(example, "asset") != null, 10000, "Path load returns the prefab asset");
            Require(Field<GameObject>(example, "first") != null && Field<GameObject>(example, "second") != null,
                "One borrowed asset creates two scene instances");
            await Click(button("removeButton"), mouse);
            Require(Field<GameObject>(example, "first") == null && Field<GameObject>(example, "second") != null,
                "Destroying one instance preserves the other");
            await Screenshot("resource-prefab-instances");
            await Click(button("releaseButton"), mouse);
            await Until(() => Field<GameObject>(example, "asset") == null, 4000, "Prefab borrow returned");
            Require(Field<GameObject>(example, "second") == null, "Instances destroyed before returning the prefab");
            return;
        }
        if (name == "LoadAddressRules")
        {
            string[] addresses = { "Cube", "Art_Cube", "Demo_Cube" };
            for (int index = 0; index < addresses.Length; index++)
            {
                dropdown("ruleDropdown").value = index;
                await Click(button("loadButton"), mouse);
                await Until(() => Field<GameObject>(example, "asset") != null, 10000, "String address resolves: " + addresses[index]);
                var instance = Field<GameObject>(example, "instance");
                Require(instance != null && instance.GetComponent<Renderer>().sharedMaterial.shader.isSupported,
                    "Address creates an instance with a supported shader");
                Require(text("stateText").text.Contains(addresses[index]), "Resolved string address is visible");
                await Screenshot("resource-address-" + index);
                await Click(button("releaseButton"), mouse);
                await Until(() => Field<GameObject>(example, "asset") == null, 4000, "Borrow returned to the selected package");
                Require(Field<GameObject>(example, "instance") == null, "Address instance destroyed");
            }
            return;
        }
        if (name == "LoadResourcePackage")
        {
            await Click(button("initializeButton"), mouse);
            await Until(() => Field<bool>(example, "initialized"), 10000, "Package initialized");
            await Click(button("loadButton"), mouse);
            await Until(() => Field<YooAsset.AssetHandle>(example, "handle") != null, 10000, "Explicit handle acquired");
            var handle = Field<YooAsset.AssetHandle>(example, "handle");
            Require(handle.IsValid && handle.AssetObject != Field<GameObject>(example, "instance"), "Asset and scene instance are distinct");
            await Click(button("initializeButton"), mouse);
            Require(Field<bool>(example, "initialized") && handle.IsValid, "Destroying a package with an active handle is rejected");
            await Screenshot("resource-package-handle");
            await Click(button("releaseButton"), mouse);
            await Until(() => Field<YooAsset.AssetHandle>(example, "handle") == null, 4000, "Explicit handle released");
            Require(Field<GameObject>(example, "instance") == null, "Instance destroyed before releasing the handle");
            await Click(button("initializeButton"), mouse);
            await Until(() => !Field<bool>(example, "initialized"), 10000, "Package destroyed after release");
            await Click(button("initializeButton"), mouse);
            await Until(() => Field<bool>(example, "initialized"), 10000, "Destroyed package can initialize again");
            return;
        }
        if (name == "ReadBinaryResource")
        {
            await Click(button("initializeButton"), mouse);
            await Until(() => Field<bool>(example, "initialized"), 10000, "Raw package initialized");
            await Click(button("readButton"), mouse);
            await Until(() => Field<byte[]>(example, "result") != null, 10000, "Raw bytes read through QGF");
            byte[] original = File.ReadAllBytes("Assets/Examples/01_Modules/12_Resource/Common/Art/Archive.bytes");
            Require(Field<byte[]>(example, "result").SequenceEqual(original.Skip(8).Take(16)), "Exact raw file segment at offset 8");
            await Screenshot("resource-binary");
            return;
        }
        if (name != "UpdateRemoteResources")
        {
            throw new NotSupportedException("Missing resource example checks: " + name);
        }

        var server = Field<ResourceHttpServer>(example, "server");
        Require(server != null, "Local HTTP resource fixture is available");
        toggle("autoLoadToggle").isOn = false;
        dropdown("groupMode").value = 1;
        await Click(button("versionButton"), mouse);
        await Until(() => Field<YooAsset.ResourcePackage>(example, "package") != null && !Field<bool>(example, "busy"), 15000,
            "Host package version and manifest initialized");
        var package = Field<YooAsset.ResourcePackage>(example, "package");
        Require(package.GetAssetInfos("Base").Length > 0 && package.GetAssetInfos("Data").Length > 0, "Manifest preserves Collector tags");
        Require(package.GetDownloadSize(ResourceExamplePaths.RemoteCubeAddress) > 0, "Initialization does not download Cube bundles");
        if (mode == 1)
        {
            int previousHttpErrors = expectedHttpErrors.Count;
            await Click(toggle("failureToggle"), mouse);
            expectedHttpErrorPrefix = server.Url + "/";
            try
            {
                await Click(button("updateButton"), mouse);
                await Until(() => !Field<bool>(example, "busy"), 25000, "404 download attempt ends");
            }
            finally
            {
                expectedHttpErrorPrefix = null;
            }
            Require(expectedHttpErrors.Count > previousHttpErrors, "Injected 404 is reported by YooAsset");
            Require(Field<YooAsset.ResourceDownloaderOperation>(example, "downloader").Status == YooAsset.EOperationStatus.Failed
                && text("stateText").text.Contains("下载失败"), "404 failure is visible and retry remains available");
            await Click(toggle("failureToggle"), mouse);
        }
        else if (mode == 2)
        {
            await Click(button("updateButton"), mouse);
            Require(!Field<YooAsset.ResourceDownloaderOperation>(example, "downloader").IsDone, "Download is pending before cancellation");
            await Click(button("stopButton"), mouse);
            await Until(() => !Field<bool>(example, "busy"), 10000, "Cancelled download completes");
            Require(Field<bool>(example, "cancelled") && text("stateText").text.Contains("已取消"), "Cancellation is visible and retry remains available");
        }
        await Click(button("updateButton"), mouse);
        await Until(() => !Field<bool>(example, "busy"), 25000, "Base tag download completes");
        var baseDownload = Field<YooAsset.ResourceDownloaderOperation>(example, "downloader");
        Require(baseDownload.Status == YooAsset.EOperationStatus.Succeeded && baseDownload.TotalDownloadCount > 0, "Base tag downloads missing files");
        Require(package.GetDownloadSize(ResourceExamplePaths.RemoteCubeAddress) == 0, "Cube is cached after Base download");
        Require(package.GetAssetInfos("Data").Any(info => package.GetDownloadSize(info.AssetPath) > 0), "Data tag remains undownloaded");
        await Click(button("loadButton"), mouse);
        await Until(() => Field<GameObject>(example, "instance") != null && !Field<bool>(example, "busy"), 10000, "Address loads the downloaded Cube");
        await Screenshot("resource-host-" + mode);
        await Click(button("checkButton"), mouse);
        await Until(() => Field<GameObject>(example, "asset") == null && !Field<bool>(example, "busy"), 10000, "Instance and borrowed asset released");
        await Click(button("updateButton"), mouse);
        await Until(() => !Field<bool>(example, "busy"), 10000, "Cached Base download completes");
        Require(Field<YooAsset.ResourceDownloaderOperation>(example, "downloader").TotalDownloadCount == 0, "Memory cleanup preserves the disk cache");
        dropdown("groupMode").value = 2;
        await Click(button("updateButton"), mouse);
        await Until(() => !Field<bool>(example, "busy"), 25000, "Data tag downloads separately");
        Require(Field<YooAsset.ResourceDownloaderOperation>(example, "downloader").Status == YooAsset.EOperationStatus.Succeeded,
            "Data tag succeeds");
        dropdown("groupMode").value = 0;
        await Click(button("updateButton"), mouse);
        await Until(() => !Field<bool>(example, "busy"), 10000, "All-tags cache check completes");
        Require(Field<YooAsset.ResourceDownloaderOperation>(example, "downloader").TotalDownloadCount == 0, "All tags are cached");
    }

    private static T Field<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
    }

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
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ResourceExamples/" + name + ".png"));
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
