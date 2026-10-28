using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public static class ValidateUguiExamples
{
    private static int assertionCount;
    public static Task<object> Run() => RunModules(null);

    public static Task<object> RunModules(string[] folders) => RunSelected(folders, null);

    public static Task<object> RunOnly(string[] names) => RunSelected(null, names);

    private static async Task<object> RunSelected(string[] folders, string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene");
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var modules = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool originalBackground = Application.runInBackground;
        var inputSettings = InputSystem.settings;
        var backgroundBehavior = inputSettings.backgroundBehavior;
        var editorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
        var disabledPointers = new HashSet<InputDevice>();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        var errors = new List<string>();
        var completed = new List<object>();
        assertionCount = 0;
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
            // A hidden automation editor must keep processing the injected device.
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (var pair in layouts)
            {
                if (names != null && !names.Contains(pair.Key))
                    continue;
                string script = (string)pair.Value["script"];
                if (folders != null && !folders.Any(folder => script.StartsWith("Assets/Examples/" + folder + "/", StringComparison.Ordinal)))
                    continue;
                var definition = modules.Single(module => script.StartsWith(
                    "Assets/Examples/" + (string)module["folder"] + "/", StringComparison.Ordinal));
                string folder = (string)definition["folder"];
                string anchor = AssetDatabase.GUIDToAssetPath((string)definition["anchorGuid"]);
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string relocated = moduleRoot + script.Substring("Assets/Examples/01_Modules".Length);
                string scene = relocated.Substring(0, relocated.Length - 3) + ".unity";
                EditorSceneManager.OpenScene(scene);
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                // Prevent physical pointer movement from replacing the test pointer mid-click.
                foreach (var pointer in InputSystem.devices.OfType<Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabledPointers.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(500);
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + pair.Key)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, pair.Key + " component");
                Require(EventSystem.current != null && EventSystem.current.currentInputModule != null, pair.Key + " native input module");
                Require(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length > 0, pair.Key + " Canvas");
                var serialized = new SerializedObject(example);
                foreach (JObject control in (JArray)pair.Value["controls"])
                {
                    string field = (string)control["field"];
                    if (field == null)
                        continue;
                    Require(serialized.FindProperty(field)?.objectReferenceValue != null, pair.Key + "." + field + " reference");
                }
                int clicked = 0;
                foreach (JObject control in (JArray)pair.Value["controls"])
                {
                    if (new[] { "JsonUtilityExample", "ReadConfigValues", "LoadConfigAsset", "RuntimeConfigValues", "SaveCounter", "ReadTranslation", "LoadCubePrefab", "LoadRoomExample" }.Contains(pair.Key))
                        continue;
                    if ((string)control["type"] != "Button" || (string)control["field"] == "errorButton")
                        continue;
                    var button = (Button)serialized.FindProperty((string)control["field"]).objectReferenceValue;
                    if (!button.interactable)
                        continue;
                    await Click(button, mouse);
                    clicked++;
                }
                await CheckState(pair.Key, example, mouse);
                if (new[] { "GameSpeedExample", "MultipleEventHandlers" }.Contains(pair.Key))
                {
                    Directory.CreateDirectory("Artifacts~/ExamplesUgui");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ExamplesUgui/" + pair.Key + ".png"));
                    await Task.Delay(200);
                }
                await Play(false);
                Require(errors.Count == 0, pair.Key + ": " + string.Join("\n", errors));
                completed.Add(new
                {
                    example = pair.Key,
                    introductoryButtons = clicked,
                    semanticChecks = "CheckState also clicks controls for the merged examples"
                });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
            foreach (var pointer in disabledPointers.Where(device => device.added && !device.enabled))
                InputSystem.EnableDevice(pointer);
            inputSettings.backgroundBehavior = backgroundBehavior;
            inputSettings.editorInputBehaviorInPlayMode = editorInputBehavior;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host project settings unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            nativeMouseInput = true,
            settingsUnchanged = true,
            scenes = completed,
            assertionCount
        };
        Directory.CreateDirectory("Artifacts~/ExamplesUgui");
        string report = names != null ? string.Join("-", names) + "-check.json" : folders == null ? "ugui-check.json" : string.Join("-", folders) + "-check.json";
        File.WriteAllText("Artifacts~/ExamplesUgui/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static async Task CheckState(string name, MonoBehaviour example, Mouse mouse)
    {
        var data = new SerializedObject(example);
        Func<string, Button> button = field => (Button)data.FindProperty(field).objectReferenceValue;
        Func<string, Text> text = field => (Text)data.FindProperty(field).objectReferenceValue;
        Func<string, Dropdown> dropdown = field => (Dropdown)data.FindProperty(field).objectReferenceValue;
        if (name == "GameSpeedExample")
        {
            Require(Math.Abs(QGF.Base.GameSpeed - 2) < 0.01f, "Speed buttons");
            Require(text("speedValueText").text.Contains("2.00"), "Live speed label");
        }
        else if (name == "PauseGameExample")
        {
            Require(QGF.Base.IsGamePaused, "Pause button");
            string before = text("gameTimeText").text;
            await Task.Delay(250);
            Require(text("gameTimeText").text == before, "Paused game clock");
            await Click(button("pauseButton"), mouse);
            Require(!QGF.Base.IsGamePaused, "Resume button");
        }
        else if (name == "NodePaths")
        {
            await Click(button("absoluteButton"), mouse);
            Require(QGF.DataNode.GetData<VarInt32>("Example/Battle/Score").Value == 100, "Root path writes score");
            await Click(button("relativeButton"), mouse);
            Require(text("treeText").text.Contains("Score = 250") && text("compareText").text.Contains("250"),
                "Relative write updates the same score and both query labels");
            await Click(button("relativeTypedButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Battle/Score").GetData<VarInt32>().Value == 500
                && text("pathText").text.Contains("Battle"), "Typed relative write and node path");
        }
        else if (name == "ReplaceNodeValue")
        {
            await Click(button("resetButton"), mouse);
            await Click(button("addButton"), mouse);
            Require(QGF.DataNode.GetData<VarInt32>("Example/Score").Value == 10, "Add to integer node");
            await Click(button("textButton"), mouse);
            Require(QGF.DataNode.GetData("Example/Score") is VarString && !button("addButton").interactable
                && text("valueText").text.Contains("已结算"), "String replacement is shown and disables numeric action");
            await Click(button("resetButton"), mouse);
            Require(QGF.DataNode.GetData<VarInt32>("Example/Score").Value == 0 && button("addButton").interactable,
                "Reset restores numeric data and interaction");
        }
        else if (name == "RemoveNodeBranch")
        {
            await Click(button("removeButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Battle") == null
                && QGF.DataNode.GetData<VarString>("Example/Player/Name").Value == "Pilot"
                && button("restoreButton").interactable, "Removing a subtree preserves its sibling");
            await Click(button("restoreButton"), mouse);
            Require(QGF.DataNode.GetData<VarInt32>("Example/Battle/Score").Value == 100, "Restore creates missing path");
            await Click(button("relativeButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Battle") == null && text("treeText").text.Contains("Battle 已移除"),
                "Relative removal updates the visible tree");
        }
        else if (name == "ManageNodeChildren")
        {
            for (int i = 0; i < 7; i++)
                await Click(button("addButton"), mouse);
            var inventory = QGF.DataNode.GetNode("Example/Inventory");
            Require(inventory.ChildCount == 7 && text("countText").text.Contains("子节点：7"), "All seven added nodes remain in DataNode");
            Require(text("childrenText").text.Contains("另有 3 个节点")
                && text("childrenText").text.Split('\n').Length == 6, "Long lists show four previews and the hidden count");
            await Screenshot(name);
            await Click(button("removeNameButton"), mouse);
            await Click(button("removeIndexButton"), mouse);
            Require(inventory.ChildCount == 5, "Name and index removal both remove one child");
            await Click(button("clearNodeButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Inventory") == inventory && inventory.ChildCount == 0,
                "Clearing contents keeps the Inventory node");
            await Click(button("clearAllButton"), mouse);
            Require(QGF.DataNode.Root.ChildCount == 0 && !button("clearNodeButton").interactable
                && !button("clearAllButton").interactable, "Clearing the tree disables empty cleanup actions");
            await Click(button("addButton"), mouse);
            Require(QGF.DataNode.GetNode("Example/Inventory").ChildCount == 1 && button("clearNodeButton").interactable,
                "Adding after a full clear recreates the path");
        }
        else if (name == "JsonUtilityExample")
        {
            var nameInput = (InputField)data.FindProperty("nameInput").objectReferenceValue;
            var jsonInput = (InputField)data.FindProperty("jsonInput").objectReferenceValue;
            var playerField = example.GetType().GetField("player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Require(dropdown("readMode").options.Count == 2 && nameInput.text == "小飞行员" && text("levelText").text == "等级：3",
                "Referenced TextAsset initializes the player; both JSON read modes available");
            for (int mode = 0; mode < 2; mode++)
            {
                dropdown("readMode").value = mode;
                jsonInput.text = "{\"Name\":\"飞行员 " + mode + "\",\"Level\":" + (10 + mode) + "}";
                await Click(button("deserializeButton"), mouse);
                Require(nameInput.text == "飞行员 " + mode && text("levelText").text == "等级：" + (10 + mode)
                    && text("resultText").text.Contains("写回角色"), "JSON read overload " + mode);
                var current = playerField.GetValue(example);
                foreach (string invalid in new[] { "{", "", "null" })
                {
                    jsonInput.text = invalid;
                    await Click(button("deserializeButton"), mouse);
                    Require(ReferenceEquals(current, playerField.GetValue(example)) && nameInput.text == "飞行员 " + mode
                        && text("levelText").text == "等级：" + (10 + mode) && text("resultText").text.Contains("保持原值"),
                        "Malformed or empty input preserves the exact previous player, mode " + mode);
                }
                jsonInput.text = "{}";
                await Click(button("deserializeButton"), mouse);
                Require(nameInput.text == "" && text("levelText").text == "等级：0", "Missing fields use default values, mode " + mode);
            }
            nameInput.text = "飞行员 \"一号\"";
            await Click(button("levelUpButton"), mouse);
            await Click(button("serializeButton"), mouse);
            var snapshot = JObject.Parse(jsonInput.text);
            Require((string)snapshot["Name"] == nameInput.text && (int)snapshot["Level"] == 1
                && text("resultText").text.Contains("写入 JSON"), "Serialize edited UTF-8 text and level with escaped quotes");
            await Screenshot("JsonUtilityExample");
        }
        else if (name == "ReadConfigValues")
        {
            var config = Field<TextAsset>(example, "config");
            Require(config != null, "Inspector TOML asset is assigned");
            await Click(button("parseButton"), mouse);
            Require(QGF.Config.GetString("Game.Title") == "配置入门"
                && QGF.Config.GetBool("Game.FeatureEnabled")
                && QGF.Config.GetInt("Player.MaxHp") == 100
                && Mathf.Approximately(QGF.Config.GetFloat("Player.Speed"), 4.5f),
                "Nested TOML values are available through typed QGF getters");
            Require(text("valuesText").text.Contains("配置入门") && text("valuesText").text.Contains("生命：100")
                && text("valuesText").text.Contains("速度：4.5"), "The scene displays the parsed values");
            await Screenshot("ReadConfigValues");
        }
        else if (name == "LoadConfigAsset")
        {
            await Click(button("reserveButton"), mouse);
            Require(QGF.Config.CachedBytesSize >= 4096, "Reserve read buffer");
            await Click(button("loadButton"), mouse);
            await Until(() => QGF.Config.HasConfig("Level.Name") && text("stateText").text.StartsWith("已读取"),
                4000, "TOML asset is read asynchronously");
            Require(text("contextText").text.Contains("UserData：当前对象"), "ReadData returns the request owner");
            await Click(button("freeButton"), mouse);
            Require(QGF.Config.CachedBytesSize == 0, "Free read buffer");
        }
        else if (name == "RuntimeConfigValues")
        {
            await Click(button("addButton"), mouse);
            Require(QGF.Config.Count == 1 && QGF.Config.GetString("Runtime.Mode") == "训练", "Add runtime value");
            await Click(button("addButton"), mouse);
            Require(QGF.Config.Count == 1 && text("resultText").text.Contains("已存在"), "Duplicate keeps old runtime value");
            await Click(button("removeButton"), mouse);
            Require(!QGF.Config.HasConfig("Runtime.Mode"), "Remove runtime value");
            await Click(button("addButton"), mouse);
            await Click(button("clearButton"), mouse);
            Require(QGF.Config.Count == 0, "Clear runtime values");
            var input = (InputField)data.FindProperty("valueInput").objectReferenceValue;
            Require(dropdown("addMode").options.Count == 2 && !input.interactable, "Typed/default and Core string input options");
            dropdown("addMode").value = 1;
            Require(input.interactable, "String conversion enables input");
            string[] values = { "42", "true", "文本", "", 1.5f.ToString(System.Globalization.CultureInfo.CurrentCulture) };
            for (int index = 0; index < values.Length; index++)
            {
                input.text = values[index];
                await Click(button("addButton"), mouse);
                Require(QGF.Config.Count == 1 && QGF.Config.GetString("Runtime.Mode") == values[index]
                    && QGF.Config.GetBool("Runtime.Mode") == (index == 1)
                    && QGF.Config.GetInt("Runtime.Mode") == (index == 0 ? 42 : 0)
                    && QGF.Config.GetFloat("Runtime.Mode") == (index == 0 ? 42 : index == 4 ? 1.5f : 0), "Shared Core conversion result " + index);
                Require(text("valuesText").text.Contains("Runtime.Mode") && text("countText").text.Contains("1"), "Visible converted value " + index);
                input.text = "changed";
                await Click(button("addButton"), mouse);
                Require(QGF.Config.GetString("Runtime.Mode") == values[index] && text("resultText").text.Contains("已存在"), "String duplicate preserves value " + index);
                await Click(button("removeButton"), mouse);
                Require(QGF.Config.Count == 0, "Component removes Core-added value " + index);
            }
            dropdown("addMode").value = 0;
            await Click(button("addButton"), mouse);
            Require(!input.interactable && QGF.Config.GetInt("Runtime.Mode") == 7, "Typed mode still supplies independent representations");
            await Click(button("clearButton"), mouse);
        }
        else if (name == "SaveCounter")
        {
            var slider = (Slider)data.FindProperty("volumeSlider").objectReferenceValue;
            slider.value = 0.25f;
            await Click(button("saveButton"), mouse);
            Require(QGF.Setting.HasSetting("Volume") && QGF.Setting.GetFloat("Volume") == 0.25f, "Save chosen volume");
            QGF.Setting.SetFloat("Volume", 0.75f);
            slider.value = .75f;
            await Click(button("reloadButton"), mouse);
            Require(QGF.Setting.GetFloat("Volume") == 0.25f && slider.value == .25f, "Reload saved disk value through UI");
            await Click(button("resetVolumeButton"), mouse);
            Require(!QGF.Setting.HasSetting("Volume") && slider.value == 0.5f, "Remove volume restores default");
            await Click(button("resetButton"), mouse);
            Require(!QGF.Setting.HasSetting("LaunchCount"), "Reset launch count");
        }
        else if (name == "ReadTranslation")
        {
            ((InputField)data.FindProperty("nameInput").objectReferenceValue).text = "飞行员";
            await Click(button("rewardButton"), mouse);
            await Click(button("rewardButton"), mouse);
            Require(text("titleText").text == "我的飞行游戏" && text("rewardText").text == "飞行员 获得 20 金币"
                && text("missingText").text.Contains("<NoKey>Unknown.Key"), "Plain, formatted and missing translations");
        }
        else if (name == "SwitchLanguage")
        {
            var reload = (Toggle)data.FindProperty("reloadToggle").objectReferenceValue;
            await Click(button("action1Button"), mouse);
            string english = QGF.Localization.GetString("Game.Name");
            Require(QGF.Localization.Language == UnityQGameFramework.Core.Language.English, "English selected");
            await Click(reload, mouse);
            await Click(button("action2Button"), mouse);
            Require(QGF.Localization.GetString("Game.Name") == english, "Language property alone leaves loaded strings unchanged");
            await Click(reload, mouse);
            await Click(button("action2Button"), mouse);
            Require(QGF.Localization.GetString("Game.Name") == "我的飞行游戏" && text("outputText").text.Contains("我的飞行游戏"), "Reload refreshes live Chinese text");
        }
        else if (name == "LoadCubePrefab")
        {
            Func<string, GameObject> field = key => (GameObject)example.GetType().GetField(key, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(example);
            await Wait(() => field("asset") != null, "Resource load");
            Require(field("first") != null && field("second") != null && field("first") != field("second"), "One asset creates two instances");
            int assetId = field("asset").GetInstanceID();
            await Screenshot("LoadCubePrefab");
            await Click(button("removeButton"), mouse);
            Require(field("first") == null && field("second") != null && field("asset").GetInstanceID() == assetId, "Destroying one instance retains the other and asset");
            await Click(button("releaseButton"), mouse);
            Require(field("first") == null && field("second") == null && field("asset") == null && text("stateText").text.Contains("已归还"), "Instances end before returning asset");
        }
        else if (name == "LoadRoomExample")
        {
            string path = "Scene_LoadRoom_BlueRoom";
            for (int i = 0; i < 2; i++)
            {
                await Click(button("loadButton"), mouse);
                await Wait(() => QGF.Scene.SceneIsLoaded(path), "Room load");
                Require(text("stateText").text.Contains("已加载") && QGF.Scene.GetLoadedSceneAssetNames().Contains(path), "Loaded room and event " + i);
                await Click(button("unloadButton"), mouse);
                await Wait(() => !QGF.Scene.SceneIsUnloading(path), "Room unload");
                Require(!QGF.Scene.SceneIsLoaded(path) && text("stateText").text.Contains("已卸载"), "Unloaded room and event " + i);
            }
        }
        else if (name == "SubscribeEvent")
        {
            Require(text("coinsText").text.Contains("10"), "Queued event result");
            var toggle = (Toggle)data.FindProperty("listenToggle").objectReferenceValue;
            await Click(toggle, mouse);
            await Click(button("sendButton"), mouse);
            Require(text("coinsText").text == "金币：10" && text("listenersText").text.Contains("发送：2 次    接收：1 次"), "Sending after unsubscribe does not receive");
            await Click(toggle, mouse);
            await Click(button("sendButton"), mouse);
            Require(text("coinsText").text == "金币：20" && text("listenersText").text.Contains("接收：2 次"), "Resubscribe receives again");
        }
        else if (name == "MultipleEventHandlers")
        {
            Require(text("countsText").text.Contains("接收者 A：1 次") && text("countsText").text.Contains("接收者 B：1 次"), "Both handlers");
            var once = (Toggle)data.FindProperty("onceToggle").objectReferenceValue;
            once.isOn = true;
            await Click(button("sendButton"), mouse);
            Require(QGF.Event.EventHandlerCount == 1, "Unsubscribe during dispatch");
        }
    }

    private static async Task Click(Selectable button, Mouse mouse)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var scroll = button.GetComponentInParent<ScrollRect>();
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
        UnityEngine.Events.UnityAction<bool> toggled = value => received = true;
        if (button is Button action)
            action.onClick.AddListener(observed);
        if (button is Toggle toggle)
            toggle.onValueChanged.AddListener(toggled);
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(90);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
        await Task.Delay(90);
        var module = EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
        string down = $"mouse={mouse.leftButton.isPressed}, action={module?.leftClick.action.ReadValue<float>()}, pointer={module?.point.action.ReadValue<Vector2>()}, focused={Application.isFocused}, gameView={EditorWindow.focusedWindow?.GetType().Name}";
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(180);
        if (button is Button finished)
            finished.onClick.RemoveListener(observed);
        if (button is Toggle changed)
            changed.onValueChanged.RemoveListener(toggled);
        if (!received)
        {
            Directory.CreateDirectory("Artifacts~/ExamplesUgui");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ExamplesUgui/failed-click.png"));
            await Task.Delay(150);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            throw new Exception("Native mouse click: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path
                + " / " + button.name + " at " + position + " hits " + string.Join(", ", hits.Select(hit => hit.gameObject.name)) + " / " + down);
        }
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    private static void Require(bool condition, string step)
    {
        if (!condition)
            throw new Exception(step);
        assertionCount++;
    }
    private static async Task Screenshot(string name)
    {
        Directory.CreateDirectory("Artifacts~/ExamplesUgui");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ExamplesUgui/" + name + ".png"));
        await Task.Delay(200);
    }
    private static async Task Wait(Func<bool> done, string step)
    {
        double start = EditorApplication.timeSinceStartup;
        while (!done())
        {
            if (EditorApplication.timeSinceStartup - start > 15)
                throw new TimeoutException(step);
            await Task.Delay(100);
        }
        await Task.Delay(150);
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
