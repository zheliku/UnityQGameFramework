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
public static class ValidateObjectPoolExamples
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
        var cases = layouts.Properties().Where(pair => new[] { "08_ObjectPool" }
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
                string anchor = AssetDatabase.GUIDToAssetPath("e8e1b0be9acd59a289bd6828e1bbe300");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported ObjectPool sample not found.");
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
                var type = typeof(SpawnObject).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                var targets = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Select(r => r.gameObject).ToArray();
                await Play(false);
                Require(targets.All(g => g == null), "All real Cube targets cleaned after scene stop");
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
        Directory.CreateDirectory("Artifacts~/ObjectPoolVisual");
        string report = names == null ? "object-pool-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/ObjectPoolVisual/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
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
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        Require(QGF.ObjectPool.GetAllObjectPools().All(p => p.Count == 0), "No implicit Cube before input");
        // UI 初始化时已经创建本例的空池；管理器示例则等按钮创建三份。
        if (name == "SpawnObject")
        {
            var pool = Field<IObjectPool<SpawnObjectItem>>(example, "pool");
            Require(dropdown("releaseArgument").options.Count == 2, "Release entry and Target options available");
            for (int argument = 0; argument < 2; argument++)
            {
                dropdown("releaseArgument").value = argument;
                await Click(button("createButton"), mouse);
                var cube = Field<GameObject>(example, "cube");
                int id = cube.GetInstanceID();
                var item = Field<SpawnObjectItem>(example, "item");
                Require(ReferenceEquals(item.Target, cube) && pool.Count == 1 && cube.transform.position.x == 2, "ObjectBase holds the exact visible Target");
                await Click(button("spawnButton"), mouse);
                Require(cube.transform.position.x == 4 && !pool.CanSpawn() && ReferenceEquals(Field<SpawnObjectItem>(example, "borrowed"), item), "Single Spawn moves same Cube to use area");
                await Click(button("spawnButton"), mouse);
                Require(pool.Count == 1 && text("resultText").text.Contains("返回 null") && cube.GetInstanceID() == id, "Occupied Spawn does not create a Cube");
                await Click(button("releaseButton"), mouse);
                Require(pool.Count == 1 && cube != null && text("resultText").text.Contains("拒绝释放"), "Borrowed Target cannot be released");
                await Click(button("returnButton"), mouse);
                Require(cube.transform.position.x == 2 && pool.CanSpawn() && cube.GetInstanceID() == id && cube.transform.rotation == Quaternion.identity, "Unspawn resets and retains actual Cube");
                await Click(button("spawnButton"), mouse);
                Require(cube.GetInstanceID() == id, "Second use reuses same GameObject");
                if (argument == 1)
                    await Screenshot("pool-lifecycle");
                await Click(button("returnButton"), mouse);
                await Click(button("releaseButton"), mouse);
                Require(cube == null && pool.Count == 0 && Field<SpawnObjectItem>(example, "item") == null, "Final Release destroys Cube and discards pooled wrapper reference");
            }
        }
        else if (name == "SpawnOrCreateCube")
        {
            var pool = Field<IObjectPool<GrowingCubeItem>>(example, "pool");
            var held = Field<List<GrowingCubeItem>>(example, "borrowed");
            Require(pool.Count == 0 && pool.Capacity == 2, "Factory starts with empty pool and capacity 2");
            for (int i = 0; i < 4; i++)
                await Click(button("spawnButton"), mouse);
            Require(pool.Count == 4 && held.Count == 4 && held.Select(i => i.Target).Distinct().Count() == 4 && Field<int>(example, "created") == 4, "Empty factory creates four real Cubes beyond Capacity");
            Require(((ObjectPoolBase)pool).GetAllObjectInfos().All(i => i.SpawnCount == 1), "Register(true) records exactly one borrow");
            var last = (GameObject)held[3].Target;
            await Click(button("returnButton"), mouse);
            Require(last == null && pool.Count == 3 && held.Count == 3, "Above capacity, returning Cube is destroyed");
            last = (GameObject)held[2].Target;
            await Click(button("returnButton"), mouse);
            Require(last == null && pool.Count == 2, "Shrinks excess as targets become unused");
            var reusable = (GameObject)held[1].Target;
            int reusedId = reusable.GetInstanceID();
            await Click(button("returnButton"), mouse);
            Require(reusable != null && reusable.transform.position.y == -2 && pool.Count == 2, "Within capacity returns actual Cube to cache area");
            await Click(button("spawnButton"), mouse);
            Require(((GameObject)held[1].Target).GetInstanceID() == reusedId && Field<int>(example, "created") == 4 && Field<int>(example, "reused") == 1, "Factory skips Instantiate for cached target");
            slider("capacitySlider").value = 6;
            for (int i = held.Count; i < 6; i++)
                await Click(button("spawnButton"), mouse);
            Require(pool.Count == 6 && !button("spawnButton").interactable, "Scene display limit is six, capacity can be selected");
            await Screenshot("pool-factory");
            while (held.Count > 0)
                await Click(button("returnButton"), mouse);
            Require(pool.Count == 6 && pool.CanReleaseCount == 6, "Capacity 6 keeps six idle targets");
            await Click(button("clearButton"), mouse);
            Require(pool.Count == 0, "Explicit release clears actual cached Cubes");
            slider("capacitySlider").value = 0;
            await Click(button("spawnButton"), mouse);
            Require(pool.Count == 1 && held.Count == 1, "Zero Capacity still permits borrowing a new target");
            await Click(button("returnButton"), mouse);
            Require(pool.Count == 0, "Zero Capacity releases target on return");
        }
        else if (name == "MultiSpawn")
        {
            var pool = Field<IObjectPool<SharedCubeItem>>(example, "pool");
            var users = Field<List<SharedCubeItem>>(example, "users");
            await Click(button("createButton"), mouse);
            var cube = Field<GameObject>(example, "cube");
            Require(pool.AllowMultiSpawn && pool.CanSpawn("Cube") && !((ObjectPoolBase)pool).GetAllObjectInfos()[0].IsInUse, "Shared entry initially idle and borrowable by name");
            for (int i = 0; i < 3; i++)
                await Click(button("borrowButton"), mouse);
            Require(users.All(i => ReferenceEquals(i.Target, cube)) && pool.Count == 1 && ((ObjectPoolBase)pool).GetAllObjectInfos()[0].SpawnCount == 3, "Three users share one exact Target");
            Require(pool.CanSpawn("Cube") && ((ObjectPoolBase)pool).GetAllObjectInfos()[0].IsInUse
                && text("stateText").text.Contains("允许共享：是") && text("stateText").text.Contains("当前状态：使用中"), "In-use shared entry remains borrowable");
            await Click(button("returnButton"), mouse);
            Require(cube.transform.rotation != Quaternion.identity && users.Count == 2, "First return keeps shared target state");
            await Click(button("releaseButton"), mouse);
            Require(cube != null && pool.Count == 1, "Shared target retained while users remain");
            await Screenshot("pool-shared");
            await Click(button("returnButton"), mouse);
            await Click(button("returnButton"), mouse);
            Require(cube.transform.rotation == Quaternion.identity && pool.CanReleaseCount == 1, "Last return resets shared target");
            Require(!((ObjectPoolBase)pool).GetAllObjectInfos()[0].IsInUse, "Last return clears occupancy snapshot");
            await Click(button("releaseButton"), mouse);
            Require(cube == null && pool.Count == 0, "Shared target finally destroyed");
        }
        else if (name == "ObjectPoolPolicy")
        {
            var pool = Field<IObjectPool<PolicyCubeItem>>(example, "pool");
            var items = Field<PolicyCubeItem[]>(example, "items");
            await Click(button("createButton"), mouse);
            var a = (GameObject)items[0].Target;
            var b = (GameObject)items[1].Target;
            var keep = (GameObject)items[2].Target;
            Require(text("stateText").text.Contains(pool.Name) && text("stateText").text.Contains(pool.ObjectType.Name)
                && text("stateText").text.Contains("每 0.25 秒检查"), "Live typed pool identity and check interval shown");
            Require(text("resultText").text.Contains("PolicyCubeItem.Policy")
                && text("resultText").text.Contains(items[0].LastUseTime.ToString("HH:mm:ss")), "Full typed identity and live entry last-use time shown");
            Require(pool.Priority == 0 && pool.AutoReleaseInterval == .25f, "Typed properties reflect creation arguments");
            Require(dropdown("policyMode").options.Count == 7, "Policy choices include default Release");
            Require(dropdown("argumentMode").options.Count == 2, "Lock and priority expose entry and Target options");
            for (int argument = 0; argument < 2; argument++)
            {
                dropdown("argumentMode").value = argument;
                await Click(toggle("lockedToggle"), mouse);
                Require(items[0].Locked && !items[1].Locked && !items[2].Locked, "Lock changes only selected entry, argument " + argument);
                await Click(toggle("lockedToggle"), mouse);
                Require(!items[0].Locked, "Unlock through same overload, argument " + argument);
                slider("prioritySlider").value = 8 + argument;
                await Task.Delay(100);
                Require(items[0].Priority == 8 + argument && items[1].Priority == 5 && items[2].Priority == 10,
                    "Priority changes only selected entry, argument " + argument);
                slider("prioritySlider").value = 0;
            }
            Require(items[0].CustomCanReleaseFlag && toggle("releaseAllowedToggle").isOn, "UI reads ObjectBase custom release flag");
            dropdown("policyMode").value = 6;
            await Click(button("applyButton"), mouse);
            Require(pool.Count == 3 && a != null && b != null && keep != null, "Default Release retains unexpired entries within capacity");
            await Click(toggle("lockedToggle"), mouse);
            dropdown("selectedCube").value = 1;
            await Click(toggle("releaseAllowedToggle"), mouse);
            dropdown("policyMode").value = 0;
            await Click(button("applyButton"), mouse);
            Require(a != null && b != null && keep == null && pool.Count == 2, "Capacity can be exceeded by locked/custom-protected targets");
            Require(items[2] == null && !toggle("lockedToggle").isOn && !toggle("releaseAllowedToggle").isOn, "Selection reads protection state without changing values");
            await Click(toggle("releaseAllowedToggle"), mouse);
            dropdown("selectedCube").value = 0;
            await Click(toggle("lockedToggle"), mouse);
            dropdown("policyMode").value = 3;
            await Click(button("applyButton"), mouse);
            Require(pool.Count == 0 && a == null && b == null, "Removing guards permits final release");
            await Click(button("createButton"), mouse);
            dropdown("selectedCube").value = 0;
            slider("prioritySlider").value = 10;
            b = (GameObject)items[1].Target;
            dropdown("policyMode").value = 2;
            await Click(button("applyButton"), mouse);
            Require(b == null && pool.Count == 2, "Changing Target priority alters lowest-priority victim");
            dropdown("policyMode").value = 3;
            await Click(button("applyButton"), mouse);
            await Click(button("createButton"), mouse);
            keep = (GameObject)items[2].Target;
            dropdown("policyMode").value = 5;
            await Click(button("applyButton"), mouse);
            Require(pool.Count == 2 && keep != null, "Custom release one only selects temporary target");
            dropdown("policyMode").value = 4;
            await Click(button("applyButton"), mouse);
            Require(pool.Count == 1 && keep != null && ((ObjectPoolBase)pool).GetAllObjectInfos()[0].Name == "Keep", "Custom filter keeps persistent target");
            await Screenshot("pool-policy");
            dropdown("policyMode").value = 3;
            await Click(button("applyButton"), mouse);
            await Click(button("createButton"), mouse);
            a = (GameObject)items[0].Target;
            b = (GameObject)items[1].Target;
            keep = (GameObject)items[2].Target;
            dropdown("policyMode").value = 4;
            await Click(button("applyButton"), mouse);
            Require(pool.Count == 1 && a == null && b == null && keep != null, "All-Temp callback releases both matching entries in one request");
            dropdown("policyMode").value = 3;
            await Click(button("applyButton"), mouse);
            await Click(button("createButton"), mouse);
            dropdown("policyMode").value = 1;
            await Click(button("applyButton"), mouse);
            await Until(() => pool.Count == 0, 4000, "Automatic idle expiry removes Cube targets");
        }
        else if (name == "InspectObjectPools")
        {
            await Click(button("createButton"), mouse);
            Require(QGF.ObjectPool.Count == 3 && QGF.ObjectPool.GetAllObjectPools().All(p => p.Count == 1), "Three pools own three real targets");
            for (int i = 0; i < dropdown("queryMode").options.Count; i++)
            {
                dropdown("queryMode").value = i;
                await Click(button("queryButton"), mouse);
                Require(text("resultText").text.Contains("查到"), "Manager query " + i);
                if (i < 5)
                {
                    Require(text("resultText").text.Contains(i < 2 ? "标识：RegistryItem" : "标识：RegistryItem.Left") && text("resultText").text.Contains("单次借用")
                        && text("resultText").text.Contains("可释放 1/1，容量 8")
                        && text("resultText").text.Contains("不自动检查；不过期"), "Base pool configuration in query " + i);
                }
            }
            var left = QGF.ObjectPool.GetObjectPool(typeof(RegistryItem), "Left");
            Require(dropdown("poolReleaseMode").options.Count == 3, "Three per-pool release operations available");
            for (int release = 0; release < 3; release++)
            {
                dropdown("poolReleaseMode").value = release;
                var target = (GameObject)QGF.ObjectPool.GetObjectPool<RegistryItem>("Left").Spawn().Target;
                QGF.ObjectPool.GetObjectPool<RegistryItem>("Left").Unspawn(target);
                await Click(button("releasePoolButton"), mouse);
                Require(QGF.ObjectPool.Count == 3 && left.Count == (release == 0 ? 1 : 0), "Base release preserves pool; operation " + release);
                Require((target != null) == (release == 0), "Base release destroys only eligible target; operation " + release);
                Require(QGF.ObjectPool.GetObjectPool<RegistryItem>().Count == 1
                    && QGF.ObjectPool.GetObjectPool<RegistryItem>("Right").Count == 1, "Per-pool release leaves other targets; operation " + release);
                if (release == 1)
                {
                    Require(text("resultText").text.Contains("剩余 0"), "Per-pool result visible");
                    QGF.ObjectPool.DestroyObjectPool(left);
                    await Click(button("createButton"), mouse);
                    left = QGF.ObjectPool.GetObjectPool(typeof(RegistryItem), "Left");
                }
            }
            Require(left.CanReleaseCount == 0 && left.Capacity == 8 && !left.AllowMultiSpawn
                && left.AutoReleaseInterval == float.MaxValue && left.ExpireTime == float.MaxValue, "Base properties remain valid after content release");
            QGF.ObjectPool.DestroyObjectPool(left);
            await Click(button("createButton"), mouse);
            await Screenshot("pool-manager");
            for (int i = 0; i < dropdown("destroyMode").options.Count; i++)
            {
                dropdown("destroyMode").value = i;
                await Click(button("createButton"), mouse);
                await Click(button("destroyButton"), mouse);
                Require(QGF.ObjectPool.Count == 2 && text("resultText").text.Contains("目标池已销毁"), "Manager destroy overload deletes one pool " + i);
                if (i >= 2)
                    Require(!button("releasePoolButton").interactable, "Missing Left disables its release button " + i);
            }
            await Click(button("clearButton"), mouse);
            Require(QGF.ObjectPool.GetAllObjectPools().All(p => p.Count == 0), "Manager release clears Target contents, preserves pools");
            int baseline = QGF.ObjectPool.Count;
            Require(dropdown("creationKind").options.Count == 4 && dropdown("creationArguments").options.Count == 15,
                "Four creation routes share fifteen parameter groups");
            Require(!button("removeComparisonButton").interactable, "No comparison pool before input");
            var named = new HashSet<int> { 1, 4, 5, 9, 10, 11, 13, 14 };
            var capacity = new HashSet<int> { 2, 4, 6, 7, 9, 10, 12, 13, 14 };
            var expiry = new HashSet<int> { 3, 5, 6, 8, 9, 11, 12, 13, 14 };
            var priority = new HashSet<int> { 7, 8, 10, 11, 12, 13, 14 };
            ObjectPoolBase previous = null;
            for (int route = 0; route < 4; route++)
                for (int arguments = 0; arguments < 15; arguments++)
                {
                    dropdown("creationKind").value = route;
                    dropdown("creationArguments").value = arguments;
                    await Click(button("compareButton"), mouse);
                    var created = Field<ObjectPoolBase>(example, "comparisonPool");
                    Require(QGF.ObjectPool.Count == baseline + 1 && created.Count == 0 && created.ObjectType == typeof(PolicyCubeItem),
                        "Comparison adds exactly one empty typed pool " + route + ":" + arguments);
                    Require(created.Name == (named.Contains(arguments) ? "Compare" : string.Empty) && created.AllowMultiSpawn == (route >= 2),
                        "Creation identity and spawn mode " + route + ":" + arguments);
                    Require(created.Capacity == (capacity.Contains(arguments) ? 8 : int.MaxValue)
                        && created.Priority == (priority.Contains(arguments) ? 5 : 0), "Capacity and pool priority " + route + ":" + arguments);
                    float expectedExpiry = expiry.Contains(arguments) ? 4f : float.MaxValue;
                    Require(created.ExpireTime == expectedExpiry && created.AutoReleaseInterval == (arguments == 14 ? .25f : expectedExpiry),
                        "Explicit/default expiry and check interval " + route + ":" + arguments);
                    Require(text("comparisonText").text.Contains("登记目标：0") && text("comparisonText").text.Contains(arguments == 14 ? "检查间隔：0.25 秒"
                        : expiry.Contains(arguments) ? "检查间隔：4 秒" : "检查间隔：不自动检查"), "Visible creation result " + route + ":" + arguments);
                    if (previous != null)
                        Require(!QGF.ObjectPool.GetAllObjectPools().Contains(previous), "Recreation removes previous comparison pool");
                    previous = created;
                }
            await Click(button("removeComparisonButton"), mouse);
            Require(QGF.ObjectPool.Count == baseline && !button("removeComparisonButton").interactable, "Comparison removal retains other pools");
            await Click(button("compareButton"), mouse);
            UnityEngine.Object.Destroy(example);
            await Until(() => QGF.ObjectPool.Count == 0, 4000, "Controller cleans its registry and comparison pools");
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
        // 按钮点击可能滚动过面板，截图前回到顶部，保留完整示例标题。
        foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }
        Canvas.ForceUpdateCanvases();
        await Task.Delay(160);
        Directory.CreateDirectory("Artifacts~/ObjectPoolVisual");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ObjectPoolVisual/" + name + ".png"));
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
