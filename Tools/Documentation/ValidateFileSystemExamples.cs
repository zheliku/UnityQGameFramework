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
using System.Text;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateFileSystemExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "17_FileSystem" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("72a626e7d8b55b6d848ea03709e2cdfb");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported FileSystem sample not found.");
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
                var type = typeof(WriteReadFileExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                string ownedPath = Field<string>(example, "path");
                string auxiliary = test.name == "WriteReadFileExample" ? Field<string>(example, "sourcePath")
                    : test.name == "ExportLogicalFile" ? Field<string>(example, "exported") : null;
                await Play(false);
                Require(!File.Exists(ownedPath) && (auxiliary == null || !File.Exists(auxiliary)), "Example files cleaned after stop: " + test.name);
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
        Directory.CreateDirectory("Artifacts~/FileSystemExamples");
        string report = names == null ? "file-system-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/FileSystemExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, TMP_Text> text = key => (TMP_Text)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_Dropdown> dropdown = key => (TMP_Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_InputField> input = key => (TMP_InputField)data.FindProperty(key).objectReferenceValue;
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        var container = Field<IFileSystem>(example, "container");
        Require(GameEntry.GetComponent<ResourceComponent>() == null, "Only Base and FileSystem started");
        if (name == "WriteReadFileExample")
        {
            Require(dropdown("writeMode").options.Count == 5 && dropdown("readMode").options.Count == 5, "Five write and five read choices");
            foreach (string value in new[] { "飞行员 QGF", string.Empty })
                for (int write = 0; write < 5; write++)
                {
                    input("contentInput").text = value;
                    dropdown("writeMode").value = write;
                    await Click(button("writeButton"), mouse);
                    Require(Encoding.UTF8.GetString(container.ReadFile("player/name.txt")) == value, "Write overload " + write + " UTF-8/empty");
                    Require(container.FileCount == 1 && container.GetFileInfo("player/name.txt").Length == Encoding.UTF8.GetByteCount(value), "Overwrite retains one logical entry");
                    for (int read = 0; read < 5; read++)
                    {
                        dropdown("readMode").value = read;
                        await Click(button("readButton"), mouse);
                        Require(text("resultText").text.EndsWith("内容：" + value, StringComparison.Ordinal), "Read overload " + read + " exact content");
                        Require(text("resultText").text.Contains("读取 " + Encoding.UTF8.GetByteCount(value) + " 字节"), "Read reports actual byte count");
                    }
                }
            input("contentInput").text = "QGF Player";
            await Click(button("writeButton"), mouse);
            await Click(button("readButton"), mouse);
            await Screenshot("file-read-write");
        }
        else if (name == "ReopenFileExample")
        {
            string path = Field<string>(example, "path");
            for (int access = 0; access < 3; access++)
            {
                await Click(button("closeButton"), mouse);
                Require(File.Exists(path) && !QGF.FileSystem.HasFileSystem(path) && QGF.FileSystem.GetFileSystem(path) == null, "Close retains disk and removes handle");
                Require(text("stateText").text.Contains("磁盘文件：仍存在"), "Closed handle keeps disk file visible");
                for (int query = 0; query < 2; query++)
                {
                    dropdown("queryMode").value = query;
                    await Click(button("queryButton"), mouse);
                    Require(QGF.FileSystem.Count == 0 && text("managerText").text.Contains("查询得到 0 个"), "Closed manager query " + query);
                }
                dropdown("accessMode").value = access;
                await Click(button("reopenButton"), mouse);
                container = Field<IFileSystem>(example, "container");
                var expected = access == 0 ? FileSystemAccess.Read : access == 1 ? FileSystemAccess.Write : FileSystemAccess.ReadWrite;
                string accessName = access == 0 ? "只读" : access == 1 ? "只写" : "读写";
                Require(container.Access == expected && container.FileCount == 1 && container.FullPath == path.Replace('\\', '/'), "Reopen exact access and persisted metadata " + access);
                Require(text("resultText").text.Contains($"已按 {accessName} 重开")
                    && text("stateText").text.Contains($"已打开：{accessName}"), "Visible access state " + access);
                Require(button("writeButton").interactable == (access != 0) && button("readButton").interactable == (access != 1), "Permission controls " + access);
                if (access != 0)
                    await Click(button("writeButton"), mouse);
                else
                {
                    bool rejected = false;
                    try
                    {
                        container.WriteFile("forbidden", new byte[0]);
                    }
                    catch (GameFrameworkException) { rejected = true; }
                    Require(rejected, "Read-only API rejects write");
                }
                if (access != 1)
                    await Click(button("readButton"), mouse);
                else
                {
                    bool rejected = false;
                    try
                    {
                        container.ReadFile("level.txt");
                    }
                    catch (GameFrameworkException) { rejected = true; }
                    Require(rejected, "Write-only API still rejects business read");
                }
                for (int query = 0; query < 2; query++)
                {
                    dropdown("queryMode").value = query;
                    await Click(button("queryButton"), mouse);
                    Require(QGF.FileSystem.Count == 1 && text("managerText").text.Contains("查询得到 1 个") && text("managerText").text.Contains("当前句柄：匹配"), "Open manager query " + query);
                }
            }
            Require(Encoding.UTF8.GetString(container.ReadFile("level.txt")) == "关卡 2", "Write-only update survives ReadWrite reopen");
            string other = Path.Combine(Path.GetDirectoryName(path), "write-only.dat");
            var writer = QGF.FileSystem.CreateFileSystem(other, FileSystemAccess.Write, 2, 8);
            try
            {
                Require(writer.WriteFile("data", new byte[] { 1, 2, 3 }), "Create and write Write-only container");
            }
            finally { QGF.FileSystem.DestroyFileSystem(writer, false); }
            writer = QGF.FileSystem.LoadFileSystem(other, FileSystemAccess.Write);
            try
            {
                Require(writer.GetFileInfo("data").Length == 3 && writer.WriteFile("data", new byte[] { 4 }), "Write-only reopens and updates existing index");
            }
            finally { QGF.FileSystem.DestroyFileSystem(writer, true); }
            await Screenshot("file-reopen");
            await Click(button("closeButton"), mouse);
        }
        else if (name == "DeleteLogicalFileExample")
        {
            Require(container.FileCount == container.MaxFileCount && container.MaxFileCount == 2, "Two-entry capacity");
            var original = container.GetFileInfo("temp.txt");
            byte[] originalBytes = container.ReadFile("temp.txt");
            await Click(button("addButton"), mouse);
            Require(!container.HasFile("third.txt") && text("resultText").text.Contains("达到上限"), "Full container rejects third entry visibly");
            input("newNameInput").text = "saved.txt";
            await Click(button("renameButton"), mouse);
            Require(container.HasFile("temp.txt") && container.HasFile("saved.txt") && text("resultText").text.Contains("失败"), "Conflicting rename preserves both records");
            input("newNameInput").text = "renamed.txt";
            await Click(button("renameButton"), mouse);
            var renamed = container.GetFileInfo("renamed.txt");
            Require(!container.HasFile("temp.txt") && renamed.IsValid && renamed.Offset == original.Offset && renamed.Length == original.Length
                && container.ReadFile("renamed.txt").SequenceEqual(originalBytes), "Rename preserves location and bytes");
            for (int query = 0; query < 2; query++)
            {
                dropdown("queryMode").value = query;
                await Click(button("inspectButton"), mouse);
                Require(text("entriesText").text.Contains("saved.txt") && text("entriesText").text.Contains("renamed.txt") && text("infoText").text.Contains("文件信息：有效"), "Index query array/List " + query);
            }
            await Screenshot("file-index");
            await Click(button("deleteButton"), mouse);
            Require(!container.HasFile("renamed.txt") && container.HasFile("saved.txt") && container.FileCount == 1, "Delete preserves sibling");
            await Click(button("deleteButton"), mouse);
            Require(text("resultText").text.Contains("键不存在") && text("infoText").text.Contains("文件信息：无效"), "Missing delete and info are visible");
            await Click(button("addButton"), mouse);
            Require(container.HasFile("third.txt") && container.FileCount == 2, "Freed capacity accepts third entry");
            await Click(button("resetButton"), mouse);
            Require(container.HasFile("temp.txt") && container.HasFile("saved.txt") && !container.HasFile("third.txt"), "Restore deterministic entries");
            input("nameInput").text = " ";
            input("newNameInput").text = "";
            await Task.Delay(80);
            Require(!button("inspectButton").interactable && !button("renameButton").interactable && !button("deleteButton").interactable, "Empty names disable invalid operations");
        }
        else if (name == "ReadFileSegments")
        {
            Require(dropdown("readMode").options.Count == 10, "Ten segment overloads");
            string source = "0123456789ABCDEF";
            foreach (var range in new[] { new[] { 4, 6 }, new[] { 15, 6 }, new[] { 20, 6 }, new[] { 0, 0 } })
                for (int read = 0; read < 10; read++)
                {
                    dropdown("readMode").value = read;
                    slider("offsetSlider").value = range[0];
                    slider("lengthSlider").value = range[1];
                    int offset = read == 1 || (read >= 5 && read <= 7) || read == 9 ? range[0] : 0;
                    int capacity = read == 4 || read == 7 ? 10 : 12;
                    int requested = read == 2 || read == 5 ? capacity : read >= 2 && read <= 7 ? Math.Min(range[1], capacity) : range[1];
                    int count = Math.Min(Math.Max(0, source.Length - offset), requested);
                    string expected = count == 0 ? "" : source.Substring(offset, count);
                    await Click(button("readButton"), mouse);
                    string output = text("resultText").text;
                    Require(output.Contains("实际读取 " + count + " 字节") && output.Contains("内容：" + expected + "\n"), "Segment bytes and EOF " + read + "/" + range[0] + "/" + range[1]);
                    if (read == 4 || read == 7)
                        Require(output.Contains("目标位置 2") && output.Contains("目标缓冲区：.." + expected), "Destination start distinct from source offset");
                }
            var missing = (Toggle)data.FindProperty("missingToggle").objectReferenceValue;
            await Click(missing, mouse);
            for (int read = 0; read < 10; read++)
            {
                dropdown("readMode").value = read;
                await Click(button("readButton"), mouse);
                Require(text("resultText").text.Contains("实际读取 0 字节") && text("resultText").text.Contains("内容：\n"), "Missing key segment overload " + read);
            }
            await Click(missing, mouse);
            dropdown("readMode").value = 7;
            slider("offsetSlider").value = 4;
            slider("lengthSlider").value = 6;
            await Click(button("readButton"), mouse);
            await Screenshot("file-segment");
        }
        else if (name == "ExportLogicalFile")
        {
            string exported = Field<string>(example, "exported");
            Require(!button("importButton").interactable && !container.HasFile("player.txt"), "No imported or logical data initially");
            input("contentInput").text = "飞行员 QGF";
            await Click(button("exportButton"), mouse);
            Require(File.ReadAllText(exported) == "飞行员 QGF" && container.HasFile("player.txt"), "Export creates independent physical file");
            await Click(button("deleteButton"), mouse);
            Require(!container.HasFile("player.txt") && File.ReadAllText(exported) == "飞行员 QGF", "Logical deletion preserves exported bytes");
            input("contentInput").text = "未导出的修改";
            await Click(button("importButton"), mouse);
            Require(Encoding.UTF8.GetString(container.ReadFile("player.txt")) == "飞行员 QGF" && text("resultText").text.Contains("已导入：飞行员 QGF"), "Import uses physical file rather than input or path text");
            input("contentInput").text = "覆盖后的导出";
            await Click(button("exportButton"), mouse);
            Require(File.ReadAllText(exported) == "覆盖后的导出", "Export overwrites old physical content");
            await Screenshot("file-export");
            input("contentInput").text = "";
            await Click(button("exportButton"), mouse);
            Require(new System.IO.FileInfo(exported).Length == 0 && container.GetFileInfo("player.txt").Length == 0, "Empty file export truncates existing content");
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
        Directory.CreateDirectory("Artifacts~/FileSystemExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/FileSystemExamples/" + name + ".png"));
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
