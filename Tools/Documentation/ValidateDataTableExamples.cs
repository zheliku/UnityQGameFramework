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
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Examples.TableData;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateDataTableExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "11_DataTable" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("9cb40d2fa7d54547ad62f12dee11fa0a");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Debugger sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/11_DataTable".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(LoadCatalogExample).Assembly.GetType("UnityQGameFramework.Examples." + test.name)
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
                Require(GameEntry.GetComponent<DataTableComponent>() == null, "No retained data-table component after stop");
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
        Directory.CreateDirectory("Artifacts~/DataTableExamples");
        string report = names == null ? "data-table-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/DataTableExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, InputField> input = key => (InputField)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        var component = QGF.DataTable;
        Require(component.Catalog != null, "Shared Catalog assigned");
        if (name == "LoadCatalogExample")
        {
            Require(toggle("contextToggle").isOn, "Inspector load starts with context enabled");
            Require(component.Count == 0 && component.LubanTables == null && !component.IsLoading, "Loader starts empty");
            for (int context = 0; context < 2; context++)
            {
                if (toggle("contextToggle").isOn != (context == 1)) await Click(toggle("contextToggle"), mouse);
                int before = Field<int>(example, "callbackCount");
                await Click(button("loadButton"), mouse);
                Require(component.Count == 1 && !component.IsLoading && component.GetLubanTables<Tables>() != null,
                    "Inspector Catalog publishes a complete table set");
                Require(Field<int>(example, "callbackCount") == before + 1 && text("contextText").text.Contains(context == 1 ? "UserData：当前对象" : "UserData：未设置"),
                    "Queued Inspector load result retains its context");
                Require(component.LubanTables.SchemaHash == component.Catalog.SchemaHash && !button("loadButton").interactable,
                    "Matching schema and duplicate load prevention");
                await Click(button("unloadButton"), mouse);
                Require(component.Count == 0 && component.GetLubanTables<Tables>() == null && !component.HasDataTable<DRItem>(),
                    "Unload removes the complete native and QGF set");
            }
            dropdown("loadMode").value = 1;
            slider("prioritySlider").value = 7;
            await Click(button("loadButton"), mouse);
            Require(component.IsLoading && component.Count == 0 && !button("loadButton").interactable, "Resource load is pending");
            int cancelled = Field<int>(example, "requestId"), callbacks = Field<int>(example, "callbackCount");
            await Click(button("cancelButton"), mouse);
            Require(!component.IsLoading && component.Count == 0, "Cancel clears pending state immediately");
            await Task.Delay(2300);
            Require(component.Count == 0 && component.LubanTables == null && Field<int>(example, "callbackCount") == callbacks,
                "Cancelled late callback cannot publish data or result events");
            await Click(button("loadButton"), mouse);
            await Click(button("unloadButton"), mouse);
            await Task.Delay(2300);
            Require(component.Count == 0 && !component.IsLoading && Field<int>(example, "callbackCount") == callbacks,
                "Unload also invalidates an in-flight request");
            await Click(button("loadButton"), mouse);
            await Click(button("cancelButton"), mouse);
            await Click(button("loadButton"), mouse);
            Require(Field<int>(example, "requestId") > cancelled, "Replacement request has a newer serial");
            await Until(() => !component.IsLoading, 6000, "Replacement request completes");
            Require(component.Count == 1 && Field<int>(example, "callbackCount") == callbacks + 1,
                "Only the replacement request publishes its set");
            await Click(button("unloadButton"), mouse);
            await Click(toggle("failureToggle"), mouse);
            await Click(button("loadButton"), mouse);
            await Until(() => !component.IsLoading, 6000, "Bad schema failure");
            Require(component.Count == 0 && component.LubanTables == null && text("stateText").text.Contains("加载失败")
                && text("contextText").text.Contains("失败回调"), "Bad schema fails without partial table registration");
            await Click(toggle("failureToggle"), mouse);
            await Click(toggle("contextToggle"), mouse);
            await Click(button("loadButton"), mouse);
            await Until(() => !component.IsLoading, 6000, "Retry succeeds");
            Require(component.Count == 1 && text("contextText").text.Contains("UserData：未设置"), "Retry without context restores data");
            Require(component.GetDataTable<DRItem>().Count > 1 && text("rowsText").text.Contains(component.GetDataTable<DRItem>().MinIdDataRow.Name),
                "Live view shows real generated records");
            await Screenshot("data-table-load");
            await Click(button("unloadButton"), mouse);
        }
        else if (name == "QueryRowsExample")
        {
            var view = component.GetDataTable<DRItem>();
            Require(view != null && view.Count > 1 && view.Name == string.Empty, "Typed shared table view loaded");
            var original = view.GetAllDataRows();
            int firstId = original[0].Id;
            input("idInput").text = firstId.ToString();
            await Task.Delay(120);
            Require(text("selectedText").text.Contains(original[0].Name), "GetDataRow by ID");
            await Click(toggle("indexerToggle"), mouse);
            Require(text("selectedText").text.Contains(original[0].Name) && ReferenceEquals(view[firstId], view.GetDataRow(firstId)),
                "Indexer and ID lookup share records");
            input("idInput").text = "999999";
            await Task.Delay(120);
            Require(text("selectedText").text.Contains("不存在") && text("selectedText").text.Contains("查询结果：无"), "Missing ID is safe");
            input("idInput").text = "not-an-id";
            await Task.Delay(120);
            Require(text("selectedText").text.Contains("请输入整数"), "Invalid ID is rejected before querying");
            input("idInput").text = firstId.ToString();
            slider("priceSlider").value = original.OrderBy(row => row.Price).ElementAt(original.Length / 2).Price;
            for (int direction = 0; direction < 2; direction++)
            {
                if (toggle("descendingToggle").isOn != (direction == 1)) await Click(toggle("descendingToggle"), mouse);
                for (int query = 0; query < 8; query++)
                {
                    dropdown("queryMode").value = query;
                    await Task.Delay(120);
                    var rows = Field<List<DRItem>>(example, "results");
                    bool filtered = query == 2 || query == 3 || query >= 6;
                    var expected = original.Where(row => !filtered || row.Price <= slider("priceSlider").value).ToArray();
                    Require(rows.Select(row => row.Id).OrderBy(id => id).SequenceEqual(expected.Select(row => row.Id).OrderBy(id => id)),
                        "Query entry " + query + " returns matching records without accumulating results");
                    if (query >= 4)
                    {
                        var prices = expected.Select(row => row.Price).OrderBy(price => price).ToArray();
                        if (direction == 1) Array.Reverse(prices);
                        Require(rows.Select(row => row.Price).SequenceEqual(prices), "Requested price order " + query + ":" + direction);
                    }
                }
            }
            Require(component.GetLubanTables<Tables>().TbItem.DataList.Select(row => row.Id).SequenceEqual(original.Select(row => row.Id)),
                "Sorted queries preserve native row order");
            Require(view.MinIdDataRow.Id == original.Min(row => row.Id) && view.MaxIdDataRow.Id == original.Max(row => row.Id), "ID bounds describe actual records");
            slider("priceSlider").value = 0;
            dropdown("queryMode").value = 7;
            await Task.Delay(120);
            Require(!view.HasDataRow(row => row.Price <= 0) && view.GetDataRow(row => row.Price <= 0) == null
                && Field<List<DRItem>>(example, "results").Count == 0, "Predicate miss and empty list");
            slider("priceSlider").value = 100;
            await Screenshot("data-table-query");
        }
        else if (name == "NativeTableExample")
        {
            var tables = component.GetLubanTables<Tables>();
            var view = component.GetDataTable<DRItem>();
            Require(tables != null && ReferenceEquals(tables, component.LubanTables), "Typed and untyped native set identity");
            Require(tables.TbItem.DataList.Count == view.Count && tables.TbItem.DataMap.Count == view.Count, "Native indexes and QGF view agree on count");
            var first = tables.TbItem.DataList[0];
            foreach (int entry in new[] { 0, 1, 2 })
            {
                dropdown("nativeMode").value = entry;
                input("idInput").text = first.Id.ToString();
                await Task.Delay(120);
                Require(text("resultText").text.Contains(first.Name) && text("identityText").text.Contains("共用同一记录"), "Native entry " + entry + " shares its row");
                input("idInput").text = "999999";
                await Task.Delay(120);
                Require(text("resultText").text.Contains(entry == 0 ? "null" : "需要有效主键"), "Missing key behavior " + entry);
            }
            input("idInput").text = "x";
            await Task.Delay(120);
            Require(text("resultText").text.Contains("请输入整数"), "Invalid native key stays in the UI");
            input("idInput").text = first.Id.ToString();
            await Screenshot("data-table-native");
        }
        else if (name == "TableRegistry")
        {
            Require(toggle("namedToggle").isOn && input("nameInput").interactable, "Named registry starts ready for a name");
            Require(component.Count == 0, "Registry starts empty");
            foreach (int style in new[] { 0, 1 })
                foreach (bool named in new[] { false, true })
                {
                    dropdown("apiMode").value = style;
                    if (toggle("namedToggle").isOn != named) await Click(toggle("namedToggle"), mouse);
                    input("nameInput").text = "Preview";
                    await Click(button("createButton"), mouse);
                    var selected = Field<IDataTable<TableRegistry.PreviewRow>>(example, "selected");
                    Require(selected != null && selected.Count == 0 && selected.Name == (named ? "Preview" : string.Empty),
                        "Create and lookup entry " + style + ":" + named);
                    Require(selected.Type == typeof(TableRegistry.PreviewRow) && selected.FullName.Contains("PreviewRow")
                        && !button("createButton").interactable && !button("registerButton").interactable, "Registry metadata and duplicate prevention");
                    await Click(button("destroyButton"), mouse);
                    Require(component.Count == 0 && Field<IDataTable<TableRegistry.PreviewRow>>(example, "selected") == null, "Destroy by type/name entry " + style + ":" + named);
                }
            foreach (int destruction in new[] { 1, 2 })
            {
                if (!toggle("namedToggle").isOn) await Click(toggle("namedToggle"), mouse);
                dropdown("destroyMode").value = destruction;
                await Click(button("registerButton"), mouse);
                var selected = Field<IDataTable<TableRegistry.PreviewRow>>(example, "selected");
                Require(selected.Count == 3 && selected.GetDataRow(1).Name == "药水", "Prepared read-only rows registered through Core");
                await Click(button("destroyButton"), mouse);
                Require(component.Count == 0, "Destroy table instance entry " + destruction);
            }
            dropdown("destroyMode").value = 0;
            foreach (string nameValue in new[] { "Preview", "Second" })
            {
                input("nameInput").text = nameValue;
                await Click(button("registerButton"), mouse);
            }
            await Click(toggle("namedToggle"), mouse);
            await Click(button("registerButton"), mouse);
            Require(component.Count == 3, "Same row type supports two named and one unnamed table");
            bool rejected = false;
            try
            {
                FrameworkEntry.GetModule<IDataTableManager>().RegisterDataTables(new DataTableBase[]
                {
                    new DataTable<TableRegistry.PreviewRow>("MustNotAppear"),
                    new DataTable<TableRegistry.PreviewRow>("Preview")
                });
            }
            catch (FrameworkException) { rejected = true; }
            Require(rejected && component.Count == 3 && !component.HasDataTable<TableRegistry.PreviewRow>("MustNotAppear"),
                "A duplicate in a registration batch publishes none of the pending tables");
            for (int query = 0; query < 2; query++)
            {
                dropdown("queryMode").value = query;
                await Task.Delay(120);
                var all = Field<List<DataTableBase>>(example, "tables");
                Require(all.Count == 3 && all.Select(table => table.Name).OrderBy(value => value)
                    .SequenceEqual(new[] { "", "Preview", "Second" }.OrderBy(value => value)), "Table collection query " + query);
                Require(all.All(table => table.HasDataRow(1) && table.Count == 3), "Base table view exposes records");
            }
            await Click(toggle("namedToggle"), mouse);
            input("nameInput").text = "";
            await Task.Delay(120);
            Require(!button("createButton").interactable && !button("registerButton").interactable && !button("destroyButton").interactable,
                "Named operations require a nonempty name");
            input("nameInput").text = "Second";
            await Task.Delay(120);
            Require(text("rowsText").text.Contains("推进器"), "Selected table rows displayed live");
            await Screenshot("data-table-registry");
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
        Directory.CreateDirectory("Artifacts~/DataTableExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/DataTableExamples/" + name + ".png"));
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
