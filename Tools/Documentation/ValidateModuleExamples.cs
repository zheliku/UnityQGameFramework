using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 检查隔离工程中的模块示例。
/// </summary>
public static class ValidateModuleExamples
{
    /// <summary>
    /// 运行示例检查并保存报告。
    /// </summary>
    /// <param name="root">示例根目录。</param>
    /// <param name="names">示例名称，为空时检查全部示例。</param>
    public static async void Begin(string root = "Assets/Examples", string[] names = null)
    {
        try
        {
            var result = await Run(root, names);
            File.WriteAllText("Artifacts~/SampleSimplification/behavior-final.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory("Artifacts~/SampleSimplification");
            File.WriteAllText("Artifacts~/SampleSimplification/behavior-final.json", JsonConvert.SerializeObject(new
            {
                error = exception.ToString()
            }, Formatting.Indented));
        }
    }

    private static MonoBehaviour example;
    private static T Field<T>(string name)
    {
        return (T)example.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(example);
    }
    private static void Click(string field)
    {
        var button = Field<Button>(field);
        Require(button.interactable, "Button disabled: " + field);
        button.onClick.Invoke();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
    private static async Task Wait(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Example operation did not complete");
            await Task.Delay(50);
        }
        await Task.Delay(80);
    }

    /// <summary>
    /// 运行指定示例并检查场景绑定和操作结果。
    /// </summary>
    /// <param name="root">示例根目录。</param>
    /// <param name="names">示例名称，为空时检查全部示例。</param>
    /// <param name="exercise">检查操作，为空时使用默认操作。</param>
    /// <returns>场景检查结果。</returns>
    public static async Task<object> Run(string root = "Assets/Examples", string[] names = null,
        Func<string, MonoBehaviour, Task> exercise = null)
    {
        if (!Path.GetFullPath(".").Contains(Path.DirectorySeparatorChar + "Artifacts~" + Path.DirectorySeparatorChar))
            throw new Exception("Use an isolated project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        names = names ?? JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"))
            .Properties().Select(item => item.Name).ToArray();
        var scenes = AssetDatabase.FindAssets("t:Scene", new[] { root }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var results = new List<object>();
        var errors = new List<string>();
        bool passed = true;
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        bool originalEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var originalOptions = EditorSettings.enterPlayModeOptions;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool originalBackground = Application.runInBackground;
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            foreach (string name in names)
            {
                int previousErrors = errors.Count;
                string failure = null;
                try
                {
                    EditorSceneManager.OpenScene(scenes.Single(path => Path.GetFileNameWithoutExtension(path) == name));
                    await Play(true);
                    Application.runInBackground = true;
                    await Task.Delay(350);
                    example = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                        .Single(item => item != null && item.GetType().Name == name);
                    var serialized = new SerializedObject(example);
                    var property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Require(property.objectReferenceValue != null, "Missing scene binding: " + property.propertyPath);
                    if (exercise != null)
                        await exercise(name, example);
                    else
                        await Exercise(name);
                }
                catch (Exception exception) { failure = exception.ToString(); }
                finally { if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false); }
                bool succeeded = failure == null && errors.Count == previousErrors;
                passed &= succeeded;
                results.Add(new
                {
                    name,
                    passed = succeeded,
                    failure,
                    errors = errors.Skip(previousErrors).ToArray()
                });
                Directory.CreateDirectory("Artifacts~/SampleSimplification");
                File.WriteAllText("Artifacts~/SampleSimplification/behavior-progress.json", JsonConvert.SerializeObject(results, Formatting.Indented));
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = originalEnabled;
            EditorSettings.enterPlayModeOptions = originalOptions;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        return new
        {
            passed,
            root,
            results,
            errors
        };
    }

    private static async Task Exercise(string name)
    {
        switch (name)
        {
            case "GameSpeedExample":
                Field<Slider>("speedSlider").value = 0.5f;
                Require(Mathf.Approximately(QGF.Base.GameSpeed, 0.5f), "Game speed");
                break;
            case "JsonUtilityExample":
                Require(!Field<InputField>("jsonInput").readOnly, "JSON input is editable");
                Field<InputField>("nameInput").text = "QGF";
                Click("serializeButton");
                Field<InputField>("nameInput").text = "Changed";
                Click("deserializeButton");
                Require(Field<InputField>("nameInput").text == "QGF", "JSON snapshot restore");
                Field<InputField>("jsonInput").text = "{\"Name\":\"手动修改\",\"Level\":9}";
                Click("deserializeButton");
                await Task.Delay(80);
                Require(Field<InputField>("nameInput").text == "手动修改" && Field<Text>("levelText").text == "Level: 9", "Edited JSON restores object");
                break;
            case "LoggingExample":
                break; // 不主动制造 Error 日志；按钮按日志编译宏显示。
            case "ReadConfigValues":
                Click("parseButton");
                Require(QGF.Config.GetInt("Player.MaxHp") > 0, "TOML read");
                break;
            case "NodePaths":
                Click("relativeButton");
                Require(QGF.DataNode.GetData<VarInt32>("Example/Battle/Score").Value == 10, "Node write");
                break;
            case "SubscribeEvent":
                Click("sendButton");
                await Wait(() => Field<Text>("coinsText").text.Contains("10"));
                break;
            case "TwoStates":
                Click("workButton");
                await Wait(() => Field<int>("completed") == 1);
                break;
            case "ChangeProcedureExample":
                await Wait(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExampleMenu02");
                Click("enterButton");
                await Wait(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExamplePlay02");
                Click("backButton");
                await Wait(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExampleMenu02");
                break;
            case "ReuseReference":
                Click("acquireButton");
                var cargo = Field<object>("held");
                await Task.Delay(80);
                Click("releaseButton");
                await Task.Delay(80);
                Click("acquireButton");
                Require(ReferenceEquals(cargo, Field<object>("held")), "Reference reuse");
                break;
            case "SpawnObject":
                Click("spawnButton");
                var cube = Field<ObjectBase>("borrowed").Target;
                await Task.Delay(80);
                Click("returnButton");
                await Task.Delay(80);
                Click("spawnButton");
                Require(ReferenceEquals(cube, Field<ObjectBase>("borrowed").Target), "Cube reuse");
                break;
            case "SpawnOrCreateCube":
                Click("spawnButton");
                Click("spawnButton");
                await Task.Delay(80);
                Click("returnButton");
                await Task.Delay(80);
                Click("spawnButton");
                Require(Field<int>("created") == 2 && Field<int>("reused") == 1, "Business factory and reuse");
                break;
            case "SaveCounter":
                int before = QGF.Setting.GetInt("Examples.Counter", 0);
                Click("saveButton");
                Require(QGF.Setting.GetInt("Examples.Counter") == before + 1, "Setting save");
                break;
            case "ReadTranslation":
                Click("rewardButton");
                await Task.Delay(80);
                Require(Field<Text>("rewardText").text.Contains("10"), "Localized format");
                break;
            case "SwitchLanguage":
                Click("action1Button");
                string english = Field<Text>("outputText").text;
                Click("action2Button");
                Require(Field<Text>("outputText").text != english, "Dictionary reload");
                break;
            case "LoadLubanTablesExample":
                Require(QGF.DataTable.IsLoaded && QGF.DataTable.Count > 0, "Luban tables published");
                Require(!string.IsNullOrWhiteSpace(Field<Text>("rowsText").text), "Generated records displayed");
                break;
            case "QueryRowsExample":
                Require(QGF.DataTable.IsLoaded, "Query table loaded");
                Field<InputField>("idInput").text = "999999";
                Require(Field<Text>("selectedText").text == "ID not found.", "Missing Id");
                break;
            case "LoadCubePrefab":
                await Wait(() => Field<GameObject>("first") != null && Field<GameObject>("second") != null);
                Click("removeButton");
                await Task.Delay(80);
                Require(Field<GameObject>("second") != null, "Other instance survives");
                Click("releaseButton");
                await Wait(() => Field<GameObject>("asset") == null);
                break;
            case "ShowCubeEntity":
                Click("showButton");
                await Wait(() => QGF.Entity.HasEntity(1));
                Click("hideButton");
                Require(!QGF.Entity.HasEntity(1), "Entity returned to group");
                break;
            case "OpenWindowExample":
                Click("openButton");
                await Wait(() => QGF.UI.HasUIForm(Field<int>("serialId")));
                Click("closeButton");
                Require(!QGF.UI.HasUIForm(Field<int>("serialId")), "UI closed");
                break;
            case "OneShotSoundExample":
                Click("playButton");
                Require(Field<int>("serialId") > 0, "Sound submitted");
                await Wait(() => !QGF.Sound.IsLoadingSound(Field<int>("serialId")));
                break;
            case "LoadRoomExample":
                Click("loadButton");
                await Wait(() => QGF.Scene.SceneIsLoaded("Scene_LoadRoom_BlueRoom"));
                Click("unloadButton");
                await Wait(() => !QGF.Scene.SceneIsUnloading("Scene_LoadRoom_BlueRoom"));
                Require(!QGF.Scene.SceneIsLoaded("Scene_LoadRoom_BlueRoom"), "Room unloaded");
                break;
            case "WriteReadFileExample":
                Field<InputField>("contentInput").text = "QGF 读写";
                Click("writeButton");
                await Task.Delay(80);
                Click("readButton");
                Require(Field<Text>("resultText").text == "QGF 读写", "Container UTF8 roundtrip");
                break;
            case "GetWebRequestExample":
                Click("requestButton");
                await Wait(() => Field<int>("serial") == 0);
                Require(Field<Text>("responseText").text.Contains("{"), "HTTP GET result");
                break;
            case "PostWebRequest":
                Field<InputField>("messageInput").text = "QGF + &= 你好";
                Click("formButton");
                await Wait(() => Field<int>("serial") == 0);
                Require(Field<Text>("responseText").text.Contains("QGF + &= 你好"), "POST field encoding");
                break;
            case "DownloadFileExample":
                Click("downloadButton");
                await Wait(() => Field<int>("serial") == 0);
                Require(File.Exists(Field<string>("path")), "Downloaded file exists");
                break;
            case "ConnectNetworkExample":
                Click("connectButton");
                await Wait(() => Field<Text>("stateText").text == "Connected");
                Click("closeButton");
                await Wait(() => Field<Text>("stateText").text.Contains("Disconnected"));
                break;
            case "InspectDebuggerExample":
                Field<Toggle>("activeToggle").isOn = false;
                Require(!QGF.Debugger.ActiveWindow, "Debugger visibility");
                break;
            default:
                await ModuleExampleActions.Exercise(name, example);
                break;
        }
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
