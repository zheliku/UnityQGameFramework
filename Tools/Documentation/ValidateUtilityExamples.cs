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
public static class ValidateUtilityExamples
{
    private static readonly List<string> assertions = new List<string>();
    private static readonly string[] Names = { "TextAndPaths", "ConvertBytes", "CheckAndXor", "CompressData", "LoggingExample" };

    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Use the isolated idle validation project.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message + "\n" + stack);
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
        Directory.CreateDirectory("Artifacts~/UtilityExamples");
        File.WriteAllText("Artifacts~/UtilityExamples/active-run.json", JsonConvert.SerializeObject(new
        {
            active = true,
            startedUtc = DateTime.UtcNow,
            enabled,
            options,
            background,
            inputBackground = oldBackground,
            inputEditor = oldEditorInput,
            originalScene = setup
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
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Base sample not imported.");
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
                completed.Add(new
                {
                    name,
                    scene = script.Substring(0, script.Length - 3) + ".unity",
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
            inputSettings.backgroundBehavior = oldBackground;
            inputSettings.editorInputBehaviorInPlayMode = oldEditorInput;
            EditorSettings.enterPlayModeOptionsEnabled = enabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            Application.logMessageReceived -= capture;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Artifacts~/UtilityExamples/active-run.json", JsonConvert.SerializeObject(new
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
            assertionCount = assertions.Count,
            scenes = completed,
            nativeButtons = true,
            testInputs = "Dropdown and InputField values assigned by test; Error log button not emitted",
            settingsUnchanged = true
        };
        Directory.CreateDirectory("Artifacts~/UtilityExamples");
        File.WriteAllText("Artifacts~/UtilityExamples/utility-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            result.unity,
            result.assertionCount,
            scenes = completed.Count,
            result.settingsUnchanged
        };
    }

    private static async Task Check(string name, MonoBehaviour owner, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = field => (Button)data.FindProperty(field).objectReferenceValue;
        Func<string, Dropdown> dropdown = field => (Dropdown)data.FindProperty(field).objectReferenceValue;
        Func<string, InputField> input = field => (InputField)data.FindProperty(field).objectReferenceValue;
        Func<string, Text> text = field => (Text)data.FindProperty(field).objectReferenceValue;
        if (name == "TextAndPaths")
        {
            input("nameInput").text = "Nova";
            input("countInput").text = "7";
            input("pathInput").text = @"C:\Game\Config.toml";
            await Click(button("formatButton"), mouse);
            Require(text("resultText").text.Contains("Nova 获得 7") && text("resultText").text.Contains("7 × 10 = 70"), "One/two/three argument formats");
            Require(text("resultText").text.Contains("file:///C:/Game/Config.toml"), "Local remote path and regular slashes");
            input("pathInput").text = "https://example.org/data.bin";
            await Click(button("formatButton"), mouse);
            Require(text("resultText").text.EndsWith("https://example.org/data.bin"), "Existing URI scheme preserved");
            input("countInput").text = "invalid";
            await Click(button("formatButton"), mouse);
            Require(text("resultText").text.Contains("整数"), "Invalid count produces visible guidance");
            input("countInput").text = "7";
            await Click(button("formatButton"), mouse);
            await Click(button("directoryButton"), mouse);
            string directory = Field<string>(owner, "directory");
            Require(directory.StartsWith(Application.temporaryCachePath, StringComparison.Ordinal) && directory.Contains("QgfPath-"), "Owned unique temporary directory");
            Require(text("directoryText").text.Contains("根目录：含文件，保留")
                && text("directoryText").text.Contains("空目录：已移除")
                && text("directoryText").text.Contains("含文件目录：已保留"), "Directory cleanup states are readable");
            Require(!Directory.Exists(Path.Combine(directory, "Empty")) && File.Exists(Path.Combine(directory, "WithFile/keep.txt")), "Empty child removed; file and root retained");
            string foreignFile = Path.Combine(directory, "untouched.txt");
            File.WriteAllText(foreignFile, "test-owned file outside example cleanup list");
            await Click(button("cleanupButton"), mouse);
            Require(File.Exists(foreignFile) && Directory.Exists(directory) && !File.Exists(Path.Combine(directory, "WithFile/keep.txt")), "Cleanup deletes only its own known file");
            File.Delete(foreignFile);
            await Click(button("cleanupButton"), mouse);
            Require(!Directory.Exists(directory), "Known file removal allows empty root cleanup");
            await Click(button("directoryButton"), mouse);
            directory = Field<string>(owner, "directory");
            UnityEngine.Object.Destroy(owner);
            await Task.Delay(160);
            Require(!Directory.Exists(directory), "Controller destruction cleans owned temporary files");
        }
        else if (name == "ConvertBytes")
        {
            string[] values = { "-42", "3.5", "true", "中", "-32768", "65535", "-1234567890123", "18446744073709551615", "4294967295", "3.141592653589793", "飞船 QGF", "飞船 QGF" };
            int[] sizes = { 4, 4, 1, 2, 2, 2, 8, 8, 4, 8, 10, 12 };
            Require(dropdown("valueType").options.Count == 12 && dropdown("writeMode").options.Count == 3, "Ten primitive types and two text encodings grouped");
            for (int type = 0; type < values.Length; type++)
            {
                dropdown("valueType").value = type;
                input("valueInput").text = values[type];
                for (int mode = 0; mode < 3; mode++)
                {
                    dropdown("writeMode").value = mode;
                    await Click(button("convertButton"), mouse);
                    string result = text("resultText").text;
                    string displayed = result.Substring(result.LastIndexOf("还原值：", StringComparison.Ordinal) + "还原值：".Length);
                    bool roundtrip = type == 9
                        ? double.Parse(displayed, System.Globalization.CultureInfo.InvariantCulture) == double.Parse(values[type], System.Globalization.CultureInfo.InvariantCulture)
                        : displayed == (type == 2 ? "True" : values[type]);
                    Require(roundtrip, "Primitive/text roundtrip type=" + type + " mode=" + mode + "; actual=" + displayed);
                    Require(result.Contains($"偏移：{(mode == 2 ? 2 : 0)}；数据长度：{sizes[type]}"), "Byte length and symmetric offset type=" + type + " mode=" + mode);
                }
            }
            dropdown("valueType").value = 0;
            input("valueInput").text = "2147483648";
            await Click(button("convertButton"), mouse);
            Require(text("resultText").text.Contains("数值范围"), "Int32 overflow visible");
            input("valueInput").text = "oops";
            await Click(button("convertButton"), mouse);
            Require(text("resultText").text.Contains("类型不符"), "Invalid primitive visible");
            dropdown("valueType").value = 10;
            input("valueInput").text = "";
            await Click(button("convertButton"), mouse);
            Require(text("resultText").text.EndsWith("还原值：") && text("resultText").text.Contains("数据长度：0"), "Empty UTF8 with nonzero offset");
        }
        else if (name == "CheckAndXor")
        {
            input("contentInput").text = "123456789";
            for (int mode = 0; mode < 3; mode++)
            {
                dropdown("crcMode").value = mode;
                await Click(button("crcButton"), mouse);
                string expected = mode == 0 ? "CB-F4-39-26" : "4E-33-72-67";
                Require(text("crcText").text.Contains(expected) && text("crcText").text.Contains("00-00-" + expected), "Known CRC32 and big-endian output mode=" + mode);
            }
            await Click(button("longTextButton"), mouse);
            Require(input("contentInput").text.Length == 300, "Long text preset");
            for (int mode = 0; mode < 6; mode++)
            {
                dropdown("xorMode").value = mode;
                await Click(button("xorButton"), mouse);
                int changed = mode % 3 == 0 ? 300 : mode % 3 == 1 ? 296 : 220;
                Require(text("xorText").text.Contains($"改变 {changed} / 300") && text("xorText").text.Contains("还原结果：一致"), "XOR range and roundtrip mode=" + mode);
                Require(text("xorText").text.Contains("返回数组：" + (mode >= 3 ? "原数组" : "新数组")), "Copy versus in-place array ownership mode=" + mode);
            }
            input("keyInput").text = "";
            await Click(button("xorButton"), mouse);
            Require(text("xorText").text.Contains("不能为空"), "Empty XOR key rejected visibly");
            input("keyInput").text = "QGF";
            input("contentInput").text = "";
            for (int mode = 0; mode < 6; mode++)
            {
                dropdown("xorMode").value = mode;
                await Click(button("xorButton"), mouse);
                Require(text("xorText").text.Contains("还原结果：一致"), "Empty byte transform mode=" + mode);
            }
        }
        else if (name == "CompressData")
        {
            string initial = input("contentInput").text;
            foreach (string value in new[] { initial, "中文 QGF", "" })
            {
                input("contentInput").text = value;
                for (int mode = 0; mode < 6; mode++)
                {
                    dropdown("mode").value = mode;
                    await Click(button("compressButton"), mouse);
                    Require(text("resultText").text.Contains("还原结果：一致") && text("resultText").text.EndsWith("还原内容：" + value), "GZip roundtrip length=" + value.Length + " mode=" + mode);
                }
            }
            await Click(button("invalidButton"), mouse);
            Require(text("resultText").text.Contains("数组接口：返回 null")
                && text("resultText").text.Contains("输出流接口：返回失败（false）"), "Malformed GZip returns null or false without exception");
        }
        else if (name == "LoggingExample")
        {
            var defines = UnityEditor.Compilation.CompilationPipeline.GetAssemblies().Single(a => a.name == owner.GetType().Assembly.GetName().Name).defines;
            foreach (string level in new[] { "Debug", "Info", "Warning", "Error" })
            {
                bool compiled = typeof(Log).GetMethod(level, new[] { typeof(string) }).GetCustomAttributes<System.Diagnostics.ConditionalAttribute>()
                    .Any(attribute => defines.Contains(attribute.ConditionString));
                Require(button(char.ToLowerInvariant(level[0]) + level.Substring(1) + "Button").interactable == compiled, "Log button follows caller compilation level=" + level);
                Require(text("statusText").text.Contains(level + " " + (compiled ? "可用" : "未编译")), "Log compilation status rendered level=" + level);
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
        foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }
        Canvas.ForceUpdateCanvases();
        await Task.Delay(160);
        Directory.CreateDirectory("Artifacts~/UtilityExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/UtilityExamples/" + name + ".png"));
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
