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
public static class ValidateEntityExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "13_Entity" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        cases.AddRange(cases.Select(test => new { test.name, test.layout, mode = 1 }).ToArray());
        cases.AddRange(cases.Where(test => test.mode == 0 && (test.name == "ShowCubeEntity" || test.name == "TwoCubeEntities"))
            .Select(test => new { test.name, test.layout, mode = 2 }).ToArray());
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
        Directory.CreateDirectory("Artifacts~/EntityExamples");
        File.WriteAllText("Artifacts~/EntityExamples/active-run.json", JsonConvert.SerializeObject(new
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
                string anchor = AssetDatabase.GUIDToAssetPath("9338363096095c298fb66e387f546ec1");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported Entity sample not found.");
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
                var type = typeof(ShowCubeEntity).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
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
            File.WriteAllText("Artifacts~/EntityExamples/active-run.json", JsonConvert.SerializeObject(new
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
            modeAndTextSetup = "Dropdown/Slider/InputField values set by test; buttons/toggles use virtual Mouse through EventSystem except pending-ID and group queries invoked directly inside the fixed load window",
            settingsUnchanged = true,
            scenes = completed,
            assertionCount = assertions.Count
        };
        Directory.CreateDirectory("Artifacts~/EntityExamples");
        string report = names == null ? "entity-examples-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/EntityExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        if (mode == 2)
        {
            Require(QGF.Entity.IsLoadingEntity(1), "Owner request still loading before destruction");
            if (name == "TwoCubeEntities")
            {
                Require(QGF.Entity.IsLoadingEntity(2), "Both owner requests still loading");
                button("queryGroupButton").onClick.Invoke();
                Require(text("groupQueryText").text.Contains("编号 1：不存在")
                    && text("groupQueryText").text.Contains("全部：数组 [] / List []"), "Group query excludes pending requests");
                for (int query = 7; query <= 8; query++)
                {
                    dropdown("queryMode").value = query;
                    // 在固定的两秒加载窗口内直接执行查询监听器，避免滚动按钮耗尽窗口。
                    button("queryButton").onClick.Invoke();
                    Require(text("queryText").text.Contains("1") && text("queryText").text.Contains("2"), "Pending IDs query " + query);
                }
            }
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(2300);
            Require(QGF.Entity.EntityCount == 0 && QGF.Entity.GetAllLoadingEntityIds().Length == 0, "Destroy prevents late pending displays");
            return;
        }
        if (mode == 1)
        {
            int[] owned = name == "AttachEntities" ? new[] { 10, 11 } : name == "TwoCubeEntities" ? new[] { 1, 2 } : new[] { 1 };
            await Until(() => owned.All(id => QGF.Entity.HasEntity(id)), 6000, "Owned entities ready before controller destruction");
            var original = QGF.Entity.GetEntity(owned[0]);
            QGF.Entity.ShowEntity(99, original.Logic.GetType(), original.EntityAssetName, original.EntityGroup.Name, Vector3.zero);
            await Until(() => QGF.Entity.HasEntity(99), 6000, "Unrelated entity ready");
            if (name == "AttachEntities")
                await Click(button("attachButton"), mouse);
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(180);
            Require(owned.All(id => !QGF.Entity.HasEntity(id) && !QGF.Entity.IsLoadingEntity(id)), "Destroy controller hides all its live entities");
            Require(QGF.Entity.HasEntity(99) && QGF.Entity.EntityCount == 1, "Unrelated entity survives owner cleanup");
            QGF.Entity.HideEntity(99);
            return;
        }
        if (name == "ShowCubeEntity")
        {
            Require(QGF.Entity.IsLoadingEntity(1), "First request has a visible loading phase");
            Require(!button("showButton").interactable && !button("hideButton").interactable && button("cancelButton").interactable,
                "Loading has cancel enabled and excludes duplicate show/hide");
            await Click(button("cancelButton"), mouse);
            Require(!QGF.Entity.IsLoadingEntity(1), "Native cancel removes pending entity");
            await Task.Delay(2300);
            Require(!QGF.Entity.HasEntity(1) && QGF.Entity.EntityCount == 0, "Cancelled load does not later display an entity");
            for (int index = 0; index < 8; index++)
            {
                dropdown("showMode").value = index;
                dropdown("hideMode").value = index % 4;
                await Click(button("showButton"), mouse);
                await Until(() => QGF.Entity.HasEntity(1), 6000, "Show overload " + index);
                var entity = QGF.Entity.GetEntity(1);
                var logic = (ShowCubeEntityLogic)entity.Logic;
                Require(QGF.Entity.IsValidEntity(entity) && logic.Available && logic.Visible, "Valid visible entity " + index);
                Require(logic.InitCalls == 1 && logic.ShowCalls == index + 1, "OnInit once, OnShow on each reuse " + index);
                Require(logic.HasShowData == (index >= 4), "Show context " + index);
                Require(Vector3.Distance(logic.CachedTransform.position, new Vector3(3, 0, 0)) < 0.01f, "Show position " + index);
                if (index == 4)
                    await Screenshot("01-show");
                await Click(button("hideButton"), mouse);
                Require(!QGF.Entity.HasEntity(1) && !QGF.Entity.IsValidEntity(entity), "Hide removes entity identity " + index);
                Require(!logic.Available && !logic.Visible && logic.HasHideData == (index % 4 == 1 || index % 4 == 3), "Hide context and visibility " + index);
            }
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(150);
            Require(QGF.Event.Count(ShowEntityFailureEventArgs.EventId) == 0, "Failure handler unsubscribed on destroy");
        }
        else if (name == "ReuseCubeEntity")
        {
            await Until(() => QGF.Entity.HasEntity(1), 6000, "Initial reuse entity");
            var slider = (Slider)data.FindProperty("idleSlider").objectReferenceValue;
            var locked = (Toggle)data.FindProperty("lockedToggle").objectReferenceValue;
            int original = QGF.Entity.GetEntity(1).gameObject.GetInstanceID();
            slider.value = 0;
            await Click(button("recycleButton"), mouse);
            await Until(() => QGF.Entity.HasEntity(1) && !Field<bool>(example, "recycling"), 6000, "Immediate reuse finishes");
            Require(QGF.Entity.GetEntity(1).gameObject.GetInstanceID() == original, "Immediate return reuses instance");
            Require(((ReuseCubeEntityLogic)QGF.Entity.GetEntity(1).Logic).ShowCalls == 2, "Reused logic resets through OnShow");
            var reused = (ReuseCubeEntityLogic)QGF.Entity.GetEntity(1).Logic;
            Require(reused.InitCalls == 1 && reused.HideCalls == 1 && reused.RecycleCalls == 1,
                "Reuse keeps initialization and records hide/recycle once");
            slider.value = 2.3f;
            await Click(button("recycleButton"), mouse);
            Require(!QGF.Entity.HasEntity(1) && text("stateText").text.Contains("等待"), "Idle timer shown while entity is absent");
            await Screenshot("02-idle");
            await Until(() => QGF.Entity.HasEntity(1) && !Field<bool>(example, "recycling"), 6000, "Expired entity reloads");
            int rebuilt = QGF.Entity.GetEntity(1).gameObject.GetInstanceID();
            Require(rebuilt != original, "Expired unlocked instance is recreated");
            Require(((ReuseCubeEntityLogic)QGF.Entity.GetEntity(1).Logic).InitCalls == 1
                && ((ReuseCubeEntityLogic)QGF.Entity.GetEntity(1).Logic).ShowCalls == 1, "New instance starts its own lifecycle");
            await Click(locked, mouse);
            await Click(button("recycleButton"), mouse);
            await Until(() => QGF.Entity.HasEntity(1) && !Field<bool>(example, "recycling"), 6000, "Locked entity returns");
            Require(QGF.Entity.GetEntity(1).gameObject.GetInstanceID() == rebuilt, "Lock retains idle instance past expiry");
            Require(text("stateText").text.Contains("本次来源：复用"), "Live identity comparison updated");
            await Screenshot("02-locked");
            await Click(locked, mouse);
            await Click(button("recycleButton"), mouse);
            await Until(() => QGF.Entity.HasEntity(1) && !Field<bool>(example, "recycling"), 6000, "Unlocked entity returns");
            Require(QGF.Entity.GetEntity(1).gameObject.GetInstanceID() != rebuilt, "Unlock restores expiry behavior");
            slider.value = 2.5f;
            await Click(button("recycleButton"), mouse);
            UnityEngine.Object.Destroy(example.gameObject);
            await Task.Delay(2900);
            Require(!QGF.Entity.HasEntity(1) && QGF.Event.Count(ShowEntitySuccessEventArgs.EventId) == 0,
                "Destroy during idle cancels the pending reshow and unsubscribes");
        }
        else if (name == "TwoCubeEntities")
        {
            await Until(() => QGF.Entity.EntityCount == 2, 6000, "Two entities loaded");
            Require(QGF.Entity.EntityGroupCount == 2 && QGF.Entity.GetEntity(1) != QGF.Entity.GetEntity(2), "Independent entity identities and groups");
            Require(QGF.Entity.GetEntityGroup("Left").EntityCount == 1 && QGF.Entity.GetEntityGroup("Right").EntityCount == 1, "Each group owns one entity");
            var idInput = (TMP_InputField)data.FindProperty("entityIdInput").objectReferenceValue;
            for (int groupIndex = 0; groupIndex < 2; groupIndex++)
            {
                int id = groupIndex + 1;
                dropdown("groupDropdown").value = groupIndex;
                idInput.text = id.ToString();
                await Click(button("queryGroupButton"), mouse);
                string result = text("groupQueryText").text;
                Require(result.Contains($"编号 {id}：存在\n资源：存在")
                    && result.Contains($"按编号：{id}；按资源首个：{id}"), "Group ID/asset existence and lookup " + groupIndex);
                Require(result.Contains($"全部：数组 [{id}] / List [{id}]")
                    && result.Contains($"同资源：数组 [{id}] / List [{id}]"), "Group arrays/List and replacement semantics " + groupIndex);
            }
            dropdown("groupDropdown").value = 0;
            idInput.text = "2";
            await Click(button("queryGroupButton"), mouse);
            Require(text("groupQueryText").text.Contains("编号 2：不存在")
                && text("groupQueryText").text.Contains("按编号：无；按资源首个：1"), "Group ID lookup never crosses into Right");
            idInput.text = "404";
            await Click(button("queryGroupButton"), mouse);
            Require(text("groupQueryText").text.Contains("编号 404：不存在") && text("groupQueryText").text.Contains("按编号：无"), "Missing ID returns null visibly");
            idInput.text = "";
            await Click(button("queryGroupButton"), mouse);
            Require(text("groupQueryText").text.Contains("请输入整数"), "Empty ID gives visible input guidance");
            idInput.text = "1";
            for (int index = 0; index < 7; index++)
            {
                dropdown("queryMode").value = index;
                await Click(button("queryButton"), mouse);
                string result = text("queryText").text;
                Require(index < 4 ? result.Contains("实体 1") && result.Contains("实体 2")
                    : index < 6 ? result.Contains("Left：1") && result.Contains("Right：1")
                    : result.Contains("实体 "), "Query array/List/first variant " + index);
            }
            await Click(button("hideFirstButton"), mouse);
            Require(!QGF.Entity.HasEntity(1) && QGF.Entity.HasEntity(2) && QGF.Entity.GetEntityGroup("Left").EntityCount == 0,
                "Hide Left leaves Right visible");
            await Click(button("queryGroupButton"), mouse);
            Require(text("groupQueryText").text.Contains("编号 1：不存在\n资源：不存在")
                && text("groupQueryText").text.Contains("按编号：无；按资源首个：无")
                && text("groupQueryText").text.Contains("全部：数组 [] / List []")
                && text("groupQueryText").text.Contains("同资源：数组 [] / List []"), "Empty Left group has no ID, asset or collection matches");
            dropdown("groupDropdown").value = 1;
            idInput.text = "2";
            await Click(button("queryGroupButton"), mouse);
            Require(text("groupQueryText").text.Contains("编号 2：存在") && text("groupQueryText").text.Contains("全部：数组 [2] / List [2]"), "Right group query remains populated after Left hide");
            dropdown("queryMode").value = 6;
            await Click(button("queryButton"), mouse);
            Require(text("queryText").text.Contains("实体 2") && !text("queryText").text.Contains("实体 1"), "First matching asset now belongs to Right");
            await Screenshot("03-groups");
            await Click(button("hideAllButton"), mouse);
            Require(QGF.Entity.EntityCount == 0 && QGF.Entity.EntityGroupCount == 2, "Hide all preserves empty groups");
            await Click(button("showButton"), mouse);
            await Until(() => QGF.Entity.EntityCount == 2, 6000, "Restore missing entity identities");
            var context = (Toggle)data.FindProperty("contextToggle").objectReferenceValue;
            await Click(context, mouse);
            await Click(button("hideAllButton"), mouse);
            Require(QGF.Entity.EntityCount == 0, "Hide all with context");
            UnityEngine.Object.Destroy(example.gameObject);
        }
        else if (name == "AttachEntities")
        {
            await Until(() => QGF.Entity.EntityCount == 2, 6000, "Parent and child loaded");
            Require(dropdown("attachMode").options.Count == 24 && dropdown("detachMode").options.Count == 8, "Attachment and detach choices");
            for (int index = 0; index < 24; index++)
            {
                dropdown("attachMode").value = index;
                dropdown("detachMode").value = index % 8;
                await Click(button("attachButton"), mouse);
                var parent = QGF.Entity.GetEntity(10);
                var child = QGF.Entity.GetEntity(11);
                var parentLogic = (AttachedCubeLogic)parent.Logic;
                var childLogic = (AttachedCubeLogic)child.Logic;
                Require(QGF.Entity.GetParentEntity(11) == parent && QGF.Entity.GetChildEntityCount(10) == 1, "Registered relationship " + index);
                Require(child.transform.parent == (index % 6 < 2 ? parent.transform : parentLogic.Socket), "Actual transform target " + index);
                Require(childLogic.AttachCalls == index + 1 && childLogic.LastData == (index % 2 == 1 ? "挂接数据" : "无"), "Attach callback and context " + index);
                await Click(button("queryButton"), mouse);
                Require(text("queryText").text.Contains("父实体：10") && text("queryText").text.Contains("首个子实体：11")
                    && text("queryText").text.Contains("数组：1/1") && text("queryText").text.Contains("List：1/1")
                    && text("queryText").text.Contains("查询结果：一致"), "Registered relationship query variants " + index);
                Vector3 attached = child.transform.position;
                await Task.Delay(350);
                Require(Vector3.Distance(attached, child.transform.position) > .05f, "Child moves with rotating parent " + index);
                if (index == 1)
                    await Screenshot("04-attached");
                await Click(button("detachButton"), mouse);
                Require(QGF.Entity.GetParentEntity(child) == null && QGF.Entity.GetChildEntityCount(10) == 0, "Detach relationship " + index);
                Require(childLogic.DetachCalls == index + 1 && childLogic.LastData == (index % 2 == 1 ? "解除数据" : "无"), "Detach callback and context " + index);
                await Click(button("queryButton"), mouse);
                Require(text("queryText").text.Contains("父实体：无") && text("queryText").text.Contains("数组：0/0")
                    && text("queryText").text.Contains("List：0/0"), "Detached relationship queries " + index);
                Vector3 detached = child.transform.position;
                await Task.Delay(300);
                Require(Vector3.Distance(detached, child.transform.position) < .001f, "Detached child remains stationary " + index);
            }
            await Click(button("attachButton"), mouse);
            await Click(button("hideParentButton"), mouse);
            Require(!QGF.Entity.HasEntity(10) && !QGF.Entity.HasEntity(11), "Hiding attached parent also hides child");
            await Click(button("showButton"), mouse);
            await Until(() => QGF.Entity.EntityCount == 2, 6000, "Restore parent and child");
            await Click(button("attachButton"), mouse);
            await Click(button("detachButton"), mouse);
            await Click(button("hideParentButton"), mouse);
            Require(!QGF.Entity.HasEntity(10) && QGF.Entity.HasEntity(11), "Detached child survives parent hide");
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
        Directory.CreateDirectory("Artifacts~/EntityExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/EntityExamples/" + name + ".png"));
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
