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
public static class ValidateFlowExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var modules = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
        var cases = layouts.Properties().Where(pair => new[] { "05_Fsm", "06_Procedure", "07_ReferencePool" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        foreach (string name in new[] { "InspectFsmStates", "CoreProcedureStart" })
            cases.Add(new
            {
                name,
                layout = layouts[name],
                mode = 1
            });
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
                string source = (string)test.layout["script"];
                string folder = "01_Modules/" + source.Split('/')[3];
                string anchor = AssetDatabase.GUIDToAssetPath((string)modules.Single(module => (string)module["folder"] == folder)["anchorGuid"]);
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported module not found: " + folder);
                string script = Path.GetDirectoryName(anchor).Replace('\\', '/') + source.Substring("Assets/Examples/01_Modules".Length);
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
                var type = typeof(TwoStates).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
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
        Directory.CreateDirectory("Artifacts~/ExamplesCoverage");
        string report = names == null ? "flow-examples-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/ExamplesCoverage/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, Button> button = field => (Button)data.FindProperty(field).objectReferenceValue;
        Func<string, TMP_Text> text = field => (TMP_Text)data.FindProperty(field).objectReferenceValue;
        Func<string, TMP_Dropdown> dropdown = field => (TMP_Dropdown)data.FindProperty(field).objectReferenceValue;
        Func<string, TMP_InputField> input = field => (TMP_InputField)data.FindProperty(field).objectReferenceValue;
        if (name == "TwoStates")
        {
            var fsm = QGF.Fsm.GetFsm<TwoStates>();
            Require(fsm.CurrentState.GetType().Name == "Waiting", "Waiting at startup");
            float before = fsm.CurrentStateTime;
            await Task.Delay(180);
            Require(fsm.CurrentStateTime > before, "Live FSM clock");
            await Click(button("workButton"), mouse);
            Require(fsm.CurrentState.GetType().Name == "Working" && !button("workButton").interactable, "Working disables duplicate request");
            Require(((Slider)data.FindProperty("progress").objectReferenceValue).value > 0, "Visible work progress");
            await Until(() => text("completedText").text.Contains("1"), 3500, "First work completed");
            Require(fsm.CurrentState.GetType().Name == "Waiting", "Working returns to Waiting");
            await Click((Toggle)data.FindProperty("automatic").objectReferenceValue, mouse);
            await Until(() => text("completedText").text.Contains("2"), 3500, "Automatic second work completed");
            await Screenshot(name);
        }
        else if (name == "FsmStateData")
        {
            var fsm = QGF.Fsm.GetFsm<FsmStateData>();
            for (int writer = 0; writer < 2; writer++)
            {
                dropdown("writeMode").value = writer;
                input("jobsInput").text = "2";
                await Click(button("loadButton"), mouse);
                Require(fsm.HasData("Jobs") && fsm.GetData<VarInt32>("Jobs").Value == 2
                    && ReferenceEquals(fsm.GetData("Jobs"), fsm.GetData<VarInt32>("Jobs")), "Writer " + writer + " typed/untyped data");
                await Click(button("runButton"), mouse);
                Require(fsm.CurrentState.GetType().Name == "Execute" && text("batchText").text.Contains("2"), "Writer " + writer + " handoff");
                await Until(() => fsm.HasData("Jobs") && fsm.GetData<VarInt32>("Jobs").Value == 1, 1800, "Live remaining jobs");
                await Until(() => !fsm.HasData("Jobs"), 1800, "Completed jobs removed");
                Require(fsm.CurrentState.GetType().Name == "Prepare" && !button("runButton").interactable, "Prepare requires new data");
                await Click(button("loadButton"), mouse);
                await Click(button("clearButton"), mouse);
                Require(!fsm.HasData("Jobs"), "Explicit RemoveData writer " + writer);
            }
        }
        else if (name == "NamedMachines")
        {
            var right = QGF.Fsm.GetFsm<NamedMachines>("Right");
            for (int query = 0; query < 2; query++)
            {
                dropdown("queryMode").value = query;
                await Task.Delay(80);
                Require(text("machinesText").text.Contains("Left") && text("machinesText").text.Contains("Right"), "Enumeration mode " + query);
                var snapshots = QGF.Fsm.GetAllFsms().Where(machine => machine.OwnerType == typeof(NamedMachines)).ToArray();
                Require(snapshots.Length == 2 && snapshots.All(machine => text("machinesText").text.Contains(machine.FullName)
                    && text("machinesText").text.Contains(machine.CurrentStateName) && machine.FsmStateCount == 1 && machine.IsRunning),
                    "Non-generic identity and running-state snapshots " + query);
            }
            for (int destroy = 0; destroy < 4; destroy++)
            {
                dropdown("destroyMode").value = destroy;
                float before = right.CurrentStateTime;
                await Click(button("destroyButton"), mouse);
                Require(!QGF.Fsm.HasFsm<NamedMachines>("Left") && QGF.Fsm.HasFsm(typeof(NamedMachines), "Right")
                    && ReferenceEquals(right, QGF.Fsm.GetFsm(typeof(NamedMachines), "Right")) && right.CurrentStateTime > before,
                    "Named destroy overload " + destroy + " preserves Right");
                Require(text("resultText").text.Contains("Left：已销毁"), "Immediate destroyed flag snapshot " + destroy);
                if (destroy == 0)
                    await Screenshot(name);
                await Click(button("recreateButton"), mouse);
                Require(QGF.Fsm.GetFsm<NamedMachines>("Left").CurrentStateTime < right.CurrentStateTime, "Recreated Left has independent clock");
            }
        }
        else if (name == "InspectFsmStates")
        {
            var fsm = QGF.Fsm.GetFsm<InspectFsmStates>();
            Require(!fsm.IsRunning && fsm.CurrentState == null && fsm.FsmStateCount == 2 && ReferenceEquals(fsm.Owner, example), "Registered before starting");
            Require(fsm.Name.Length == 0 && text("stateText").text.Contains("未命名"), "Unnamed generic FSM identity");
            dropdown("startMode").value = mode;
            for (int query = 0; query < 2; query++)
            {
                dropdown("listMode").value = query;
                await Click(button("inspectButton"), mouse);
                Require(text("statesText").text.Contains("Idle") && text("statesText").text.Contains("Walk")
                    && text("lookupText").text.Contains("HasFsm 泛型/Type：存在 / 存在")
                    && text("lookupText").text.Contains("同一实例") && !fsm.IsRunning, "State list mode " + query + " has no lifecycle side effect");
            }
            await Click(button("startButton"), mouse);
            Require(fsm.IsRunning && fsm.CurrentState.GetType().Name == "Idle" && !button("startButton").interactable, "Start overload " + mode);
            await Click(button("walkButton"), mouse);
            Require(fsm.CurrentState.GetType().Name == "Walk" && !button("walkButton").interactable, "ChangeState(Type)");
        }
        else if (name == "FsmLifecycle")
        {
            for (int destroy = 0; destroy < 4; destroy++)
            {
                dropdown("createMode").value = destroy % 2;
                dropdown("destroyMode").value = destroy;
                await Click(button("createButton"), mouse);
                Require(QGF.Fsm.HasFsm<FsmLifecycle>() && Field<int>(example, "initialized") == destroy + 1
                    && Field<int>(example, "entered") == destroy + 1, "Create overload " + destroy % 2 + " callbacks");
                await Click(button("destroyButton"), mouse);
                Require(!QGF.Fsm.HasFsm(typeof(FsmLifecycle)) && Field<int>(example, "left") == destroy + 1
                    && Field<int>(example, "destroyed") == destroy + 1 && Field<object>(example, "fsm") == null,
                    "Unnamed destroy overload " + destroy + " leaves/destroys exactly once");
            }
            await Screenshot(name);
        }
        else if (name == "ProcedureEntryExample")
        {
            Require(QGF.Procedure.CurrentProcedure is ExampleLaunch01 && Field<int>(example, "Entered") == 1, "Inspector entrance OnEnter once");
            float before = QGF.Procedure.CurrentProcedureTime;
            await Task.Delay(200);
            Require(QGF.Procedure.CurrentProcedureTime > before && text("elapsedText").text.Contains("秒"), "Procedure live clock");
        }
        else if (name == "ChangeProcedureExample")
        {
            await Until(() => QGF.Procedure.CurrentProcedure is ExampleMenu02, 2000, "Launch to Menu");
            for (int cycle = 0; cycle < 2; cycle++)
            {
                await Click(button("enterButton"), mouse);
                Require(QGF.Procedure.CurrentProcedure is ExamplePlay02 && button("backButton").interactable, "Menu to Play cycle " + cycle);
                await Click(button("backButton"), mouse);
                Require(QGF.Procedure.CurrentProcedure is ExampleMenu02 && Field<int>(example, "PlayVisits") == cycle + 1
                    && Field<int>(example, "MenuVisits") == cycle + 2, "Play to Menu resets requests cycle " + cycle);
            }
        }
        else if (name == "ProcedureDataExample")
        {
            var fsm = QGF.Fsm.GetFsm<IProcedureManager>();
            for (int level = 0; level < 2; level++)
            {
                dropdown("levelChoice").value = level;
                string selected = dropdown("levelChoice").options[level].text;
                await Click(button("confirmButton"), mouse);
                Require(QGF.Procedure.CurrentProcedure is ExampleLoad03 && fsm.GetData<VarString>("Level").Value == selected
                    && text("dataText").text.Contains(selected), "Level handoff " + selected);
                await Click(button("finishButton"), mouse);
                Require(QGF.Procedure.CurrentProcedure is ExampleSelect03 && !fsm.HasData("Level")
                    && text("dataText").text.Contains("流程数据 Level：已移除")
                    && !Field<bool>(example, "HasLevelData"), "Level data removed after use");
            }
        }
        else if (name == "InspectProcedures")
        {
            for (int query = 0; query < 4; query++)
            {
                dropdown("queryMode").value = query;
                await Click(button("queryButton"), mouse);
                Require(text("resultText").text.Contains(query < 2 ? "注册状态：已注册" : "注册状态：未注册")
                    && (query < 2 || text("resultText").text.Contains("无实例")), "Procedure query mode " + query);
            }
            var before = QGF.Procedure.CurrentProcedure;
            await Until(() => QGF.Procedure.CurrentProcedure != before, 3000, "Timed procedure alternation");
            await Screenshot(name);
        }
        else if (name == "CoreProcedureStart")
        {
            Require(GameEntry.GetComponent<ProcedureComponent>() == null, "Core example has no ProcedureComponent");
            dropdown("startMode").value = mode;
            await Click(button("initializeButton"), mouse);
            var manager = GameFrameworkEntry.GetModule<IProcedureManager>();
            Require(manager.CurrentProcedure == null && manager.HasProcedure<CoreLaunch05>()
                && manager.HasProcedure(typeof(CoreReady05)) && !button("initializeButton").interactable, "Core Initialize registers without starting");
            await Click(button("startButton"), mouse);
            Require(manager.CurrentProcedure is CoreLaunch05 && !button("startButton").interactable, "Core StartProcedure overload " + mode);
            await Until(() => manager.CurrentProcedure is CoreReady05, 2500, "Core launch to ready");
            Require(text("queryText").text.Contains("CoreLaunch：已注册") && text("queryText").text.Contains("CoreReady：已注册")
                && ReferenceEquals(manager.GetProcedure<CoreReady05>(), manager.CurrentProcedure), "Core generic/Type query view");
        }
        else if (name == "ReuseReference")
        {
            await Click(button("acquireButton"), mouse);
            int id = Field<ReuseReference.Cargo>(example, "held").Id;
            Require(Pool<ReuseReference.Cargo>().UsingReferenceCount == 1, "Borrowed cargo counted");
            await Click(button("releaseButton"), mouse);
            Require(Field<ReuseReference.Cargo>(example, "held") == null && Pool<ReuseReference.Cargo>().UsingReferenceCount == 0, "Release drops local reference");
            await Click(button("acquireButton"), mouse);
            Require(Field<ReuseReference.Cargo>(example, "held").Id == id && text("previousText").text.Contains(id.ToString()), "Reacquire reuses cargo");
            await Screenshot(name);
        }
        else if (name == "ResetReference")
        {
            for (int acquire = 0; acquire < 2; acquire++)
            {
                dropdown("acquireMode").value = acquire;
                await Click(button("acquireButton"), mouse);
                var reward = Field<ResetReference.Reward>(example, "held");
                int id = reward.Id;
                input("coinsInput").text = "42";
                await Click(button("setButton"), mouse);
                Require(reward.Coins == 42, "Editable reward coins");
                await Click(button("releaseButton"), mouse);
                Require(Field<ResetReference.Reward>(example, "held") == null, "Returned reward no longer held");
                await Click(button("acquireButton"), mouse);
                reward = Field<ResetReference.Reward>(example, "held");
                Require(reward.Id == id && reward.Coins == 0 && reward.ClearCalls > 0, "Acquire overload " + acquire + " observes Clear before reuse");
                await Click(button("releaseButton"), mouse);
            }
        }
        else if (name == "ReferenceOwnership")
        {
            await Click(button("acquireButton"), mouse);
            await Click(button("releaseButton"), mouse);
            Require(Field<VarInt32>(example, "local") == null, "Local ownership release");
            await Click(button("acquireButton"), mouse);
            var owned = Field<VarInt32>(example, "local");
            await Click(button("transferButton"), mouse);
            Require(Field<VarInt32>(example, "local") == null && ReferenceEquals(QGF.DataNode.GetData<VarInt32>("Example/Owned"), owned)
                && Pool<VarInt32>().UsingReferenceCount == 1, "Transfer preserves same wrapper with one owner");
            await Screenshot(name);
            await Click(button("removeButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Owned") == null && Pool<VarInt32>().UsingReferenceCount == 0, "RemoveNode returns wrapper exactly once");
        }
        else if (name == "ManageReferences")
        {
            for (int api = 0; api < 2; api++)
            {
                dropdown("apiMode").value = api;
                await Click(button("addButton"), mouse);
                var baseline = Pool<ManageReferences.CapacityEntry>();
                Require(baseline.UnusedReferenceCount == 3 && baseline.UsingReferenceCount == 0, "Prewarm overload " + api);
                await Click(button("borrowButton"), mouse);
                int id = Field<ManageReferences.CapacityEntry>(example, "held").Id;
                await Click(button("removeButton"), mouse);
                Require(Pool<ManageReferences.CapacityEntry>().UnusedReferenceCount == 0
                    && Pool<ManageReferences.CapacityEntry>().UsingReferenceCount == 1, "Remove overload " + api + " only idle entries");
                await Click(button("addButton"), mouse);
                await Click(button("clearButton"), mouse);
                Require(Pool<ManageReferences.CapacityEntry>().UnusedReferenceCount == 0
                    && Field<ManageReferences.CapacityEntry>(example, "held").Id == id, "RemoveAll overload " + api + " preserves borrower");
                await Click(button("borrowButton"), mouse);
                await Click(button("removeButton"), mouse);
                var final = Pool<ManageReferences.CapacityEntry>();
                Require(final.UnusedReferenceCount == 0 && final.UsingReferenceCount == 0
                    && final.AcquireReferenceCount == api + 1 && final.ReleaseReferenceCount == api + 1
                    && final.AddReferenceCount == (api + 1) * 6 && final.RemoveReferenceCount == (api + 1) * 6, "Reference counters mode " + api);
            }
            await Screenshot(name);
        }
        else if (name == "ReferencePoolChecks")
        {
            var toggle = (Toggle)data.FindProperty("strictToggle").objectReferenceValue;
            if (toggle.isOn)
                await Click(toggle, mouse);
            Require(!button("checkButton").interactable, "Unsafe duplicate probe disabled without strict mode");
            await Click(toggle, mouse);
            Require(ReferencePool.EnableStrictCheck, "Strict toggle");
            await Click(button("checkButton"), mouse);
            Require(Pool<ReferencePoolChecks.CheckedEntry>().UnusedReferenceCount == 1
                && Pool<ReferencePoolChecks.CheckedEntry>().UsingReferenceCount == 0 && text("resultText").text.Contains("拒绝"), "Duplicate release caught without corrupting queue");
            Require(button("clearButton").interactable, "All references returned before global ClearAll: " + text("poolsText").text);
            await Click(button("clearButton"), mouse);
            Require(ReferencePool.Count == 0, "ClearAll after all borrowers returned");
        }
        else
            throw new Exception("Missing check: " + name);
    }

    private static ReferencePoolInfo Pool<T>() where T : class, IReference => ReferencePool.GetAllReferencePoolInfos().Single(info => info.Type == typeof(T));
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
        var rect = (RectTransform)target.transform;
        var scroll = target.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            if (!scroll.viewport.rect.Contains(local))
            {
                float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
                float offset = scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y;
                if (overflow > 0)
                    scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
                Canvas.ForceUpdateCanvases();
                await Task.Delay(100);
            }
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
            Require(button != null ? received : toggle != null && toggle.isOn != oldValue, "Native click " + target.name);
        }
        finally { if (button != null) button.onClick.RemoveListener(observed); }
    }

    private static async Task Screenshot(string name)
    {
        Directory.CreateDirectory("Artifacts~/ExamplesCoverage");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ExamplesCoverage/" + name + ".png"));
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
