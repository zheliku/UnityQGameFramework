using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Sound;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples;
using PlaySoundSuccessEventArgs = UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs;

// 仅用于隔离工程验收。直接调用按钮监听器，以便在异步请求尚未完成时销毁控制器。
public static class ValidateExampleOwnership
{
    private static readonly List<string> assertions = new List<string>();
    private static readonly string[] controllers = { "OpenWindowExample", "ReopenWindowExample", "CoverWindowExample", "InspectWindows",
        "ReplaceSoundAgents", "FollowEntitySound", "LoadRoomExample", "SwitchRoomsExample", "SceneOrderExample" };

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)) throw new Exception("Unsaved scene.");
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        var sceneHashes = new Dictionary<string, string>();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var completed = new List<object>();
        var errors = new List<string>();
        assertions.Clear();
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool background = Application.runInBackground;
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            foreach (string name in controllers.Where(name => names == null || names.Contains(name)))
            {
                int modes = name == "ReopenWindowExample" || name == "FollowEntitySound" || name == "SwitchRoomsExample" ? 3 : 2;
                for (int mode = 0; mode < modes; mode++)
                {
                    int start = assertions.Count;
                    string scene = ScenePath(layouts, name);
                    sceneHashes[scene] = Hash(scene);
                    EditorSceneManager.OpenScene(scene);
                    Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples." + name)).First(t => t != null);
                    // 不保存：让框架先启动，再建一个不属于示例控制器的对象。
                    ((MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type)).enabled = false;
                    await Play(true);
                    Application.runInBackground = true;
                    var owner = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                    await Task.Delay(250);
                    if (name == "ReplaceSoundAgents" || name == "FollowEntitySound") await CheckSound(name, mode, owner);
                    else if (name.EndsWith("RoomExample") || name == "SwitchRoomsExample" || name == "SceneOrderExample") await CheckScene(name, mode, owner, layouts);
                    else await CheckUI(name, mode, owner);
                    await Play(false);
                    Require(errors.Count == 0, name + " runtime errors: " + string.Join("\n", errors));
                    completed.Add(new { name, mode, scene, assertions = assertions.Skip(start).ToArray() });
                }
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Five host settings unchanged");
        Require(sceneHashes.All(item => Hash(item.Key) == item.Value), "Saved source scenes unchanged");
        var result = new { passed = true, unity = Application.unityVersion, interaction = "Programmatic listeners for destruction timing; functional UI uses separate native-input validators",
            runs = completed, assertionCount = assertions.Count, settingsUnchanged = true, savedScenesUnchanged = true };
        Directory.CreateDirectory("Artifacts~/OwnerCleanupExamples");
        File.WriteAllText("Artifacts~/OwnerCleanupExamples/" + (names == null ? "ownership" : string.Join("-", names)) + "-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, runs = completed.Count, result.assertionCount, result.settingsUnchanged, result.savedScenesUnchanged };
    }

    private static async Task CheckUI(string name, int mode, MonoBehaviour owner)
    {
        string asset = AssetDatabase.GUIDToAssetPath(Field<string>(owner, "windowGuid"));
        string group = name == "InspectWindows" ? "Hud" : "Example";
        QGF.UI.AddUIGroup(group);
        int outside = QGF.UI.OpenUIForm(asset, group, "外部窗口");
        await Until(() => QGF.UI.HasUIForm(outside), "Unrelated window loaded");
        SetLoadDelay(mode == 1 ? 2 : 0);
        owner.enabled = true;
        await Until(() => Field<bool>(owner, "ready"), "Controller initialized");
        string[] keys = name == "CoverWindowExample" ? new[] { "bottom", "top" } : name == "InspectWindows" ? new[] { "hud", "popup" } : new[] { "serialId" };
        if (name == "CoverWindowExample" && mode == 0)
        {
            await Until(() => QGF.UI.HasUIForm(Field<int>(owner, "bottom")), "Bottom window loaded");
            Field<Button>(owner, "openButton").onClick.Invoke();
        }
        int[] ids = keys.Select(key => Field<int>(owner, key)).Where(id => id != 0).ToArray();
        Require(ids.Length == (name == "CoverWindowExample" && mode == 1 ? 1 : keys.Length), "Owned request identities recorded");
        if (mode == 1) Require(ids.All(id => QGF.UI.IsLoadingUIForm(id)), "Owned windows still loading");
        else await Until(() => ids.All(id => QGF.UI.HasUIForm(id)), "Owned windows loaded");
        if (mode == 2)
        {
            Field<Slider>(owner, "idleSlider").value = 2.5f;
            Field<Button>(owner, "reopenButton").onClick.Invoke();
            Require(Field<bool>(owner, "changing") && !QGF.UI.HasUIForm(ids[0]), "Reopen idle wait started");
        }
        UnityEngine.Object.Destroy(owner.gameObject);
        await Task.Delay(mode == 2 ? 2800 : mode == 1 ? 2300 : 150);
        Require(ids.All(id => !QGF.UI.HasUIForm(id) && !QGF.UI.IsLoadingUIForm(id)), "Owned loaded and pending windows removed");
        Require(QGF.UI.GetAllLoadedUIForms().Length == 1 && QGF.UI.HasUIForm(outside), "Unrelated window preserved with no late reopen");
    }

    private static async Task CheckSound(string name, int mode, MonoBehaviour owner)
    {
        string asset = AssetDatabase.GUIDToAssetPath(Field<string>(owner, "soundGuid"));
        bool follow = name == "FollowEntitySound";
        string group = follow ? "Outside" : "Effects";
        QGF.Sound.AddSoundGroup(group, false, false, .2f, follow ? 1 : 2);
        var parameters = PlaySoundParams.Create();
        parameters.Loop = true;
        parameters.Priority = 100;
        var outsideData = new object();
        ISoundAgent outsideAgent = null;
        var heard = new Dictionary<int, ISoundAgent>();
        EventHandler<GameEventArgs> played = (sender, args) =>
        {
            var e = (PlaySoundSuccessEventArgs)args;
            heard[e.SerialId] = e.SoundAgent;
            if (ReferenceEquals(e.UserData, outsideData)) outsideAgent = e.SoundAgent;
        };
        QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, played);
        int outside = QGF.Sound.PlaySound(asset, group, 0, parameters, outsideData);
        await Until(() => outsideAgent != null && outsideAgent.Length > 0, "Unrelated looping sound loaded");
        if (follow)
        {
            QGF.Entity.AddEntityGroup("Emitters", 30, 4, 60, 0);
            QGF.Entity.ShowEntity<SoundEmitterLogic>(99, AssetDatabase.GUIDToAssetPath(Field<string>(owner, "prefabGuid")), "Emitters");
            await Until(() => QGF.Entity.HasEntity(99), "Unrelated emitter loaded");
        }
        SetLoadDelay(mode == 1 ? 2 : 0);
        owner.enabled = true;
        await Until(() => Field<bool>(owner, follow ? "ready" : "initialized"), "Controller initialized");
        int[] ids;
        if (follow && mode == 1)
        {
            Require(QGF.Entity.IsLoadingEntity(1), "Owned emitter still loading");
            ids = new int[0];
        }
        else
        {
            if (follow) await Until(() => QGF.Entity.HasEntity(1), "Owned emitter loaded");
            if (mode == 2) SetLoadDelay(2);
            Field<Button>(owner, "playButton").onClick.Invoke();
            if (follow)
            {
                int id = Field<int>(owner, "serialId");
                ids = new[] { id };
                if (mode == 2) Require(QGF.Sound.IsLoadingSound(id), "Owned bound sound still loading");
                else await Until(() => Field<ISoundAgent>(owner, "agent")?.Length > 0, "Owned bound sound loaded");
            }
            else
            {
                var requests = Field<List<int>>(owner, "requests");
                if (mode == 0)
                {
                    await Until(() => Field<List<ISoundAgent>>(owner, "agents").Count > 0, "Owned priority sound loaded");
                    Field<Button>(owner, "playButton").onClick.Invoke();
                    await Until(() => !QGF.Sound.IsLoadingSound(requests.Last()), "Replacement request finished");
                }
                else Require(QGF.Sound.IsLoadingSound(requests.Last()), "Owned priority sound still loading");
                ids = requests.ToArray();
            }
        }
        var ownedAgents = follow ? new[] { Field<ISoundAgent>(owner, "agent") } : Field<List<ISoundAgent>>(owner, "agents").ToArray();
        UnityEngine.Object.Destroy(owner.gameObject);
        await Task.Delay(mode == 0 ? 200 : 2300);
        Require(ids.All(id => !QGF.Sound.IsLoadingSound(id)), "Owned sound requests completed or cancelled");
        Require(ownedAgents.All(agent => agent == null || !ids.Contains(agent.SerialId) || agent.Length == 0), "Owned voices stopped");
        Require(ids.All(id => !heard.ContainsKey(id) || heard[id].SerialId != id || heard[id].Length == 0), "No owned playback survives cancellation");
        if ((!follow && mode == 1) || (follow && mode == 2)) Require(ids.All(id => !heard.ContainsKey(id)), "Cancelled request never reported playback success");
        Require(outsideAgent.SerialId == outside && outsideAgent.Length > 0 && outsideAgent.IsPlaying, "Unrelated sound preserved");
        if (follow) Require(!QGF.Entity.HasEntity(1) && !QGF.Entity.IsLoadingEntity(1) && QGF.Entity.HasEntity(99), "Only owned emitter hidden, including pending display");
        QGF.Event.Unsubscribe(PlaySoundSuccessEventArgs.EventId, played);
    }

    private static async Task CheckScene(string name, int mode, MonoBehaviour owner, JObject layouts)
    {
        string outsideController = name == "LoadRoomExample" ? "SwitchRoomsExample" : "LoadRoomExample";
        string outsideScene = ScenePath(layouts, outsideController);
        // 从保存的场景读取另一个房间的 GUID；不在运行中打开另一个示例入口。
        string field = outsideController == "LoadRoomExample" ? "roomGuid" : "orangeRoomGuid";
        string guid = Regex.Match(File.ReadAllText(outsideScene), @"\b" + field + @": (\w+)").Groups[1].Value;
        string outside = AssetDatabase.GUIDToAssetPath(guid);
        QGF.Scene.LoadScene(outside);
        await Until(() => QGF.Scene.SceneIsLoaded(outside), "Unrelated room loaded");
        string[] owned = name == "LoadRoomExample" ? new[] { AssetDatabase.GUIDToAssetPath(Field<string>(owner, "roomGuid")) }
            : new[] { AssetDatabase.GUIDToAssetPath(Field<string>(owner, "blueRoomGuid")), AssetDatabase.GUIDToAssetPath(Field<string>(owner, "orangeRoomGuid")) };
        Camera camera = name == "SceneOrderExample" ? Field<Camera>(owner, "sceneCamera") : null;
        string tag = camera != null ? camera.tag : null;
        bool caughtPending = false;
        EditorApplication.CallbackFunction destroyPending = () =>
        {
            if (mode == 1 && owner != null && owned.Any(path => QGF.Scene.SceneIsLoading(path)))
            {
                caughtPending = true;
                UnityEngine.Object.DestroyImmediate(owner.gameObject);
            }
        };
        EditorApplication.update += destroyPending;
        try
        {
            owner.enabled = true;
            if (name == "LoadRoomExample")
            {
                await Until(() => Field<bool>(owner, "ready"), "Controller initialized");
                Field<Button>(owner, "loadButton").onClick.Invoke();
                if (mode == 1) destroyPending();
            }
            if (mode == 1) await Until(() => caughtPending, "Controller destroyed during actual scene loading");
            else
            {
                await Until(() => name == "SceneOrderExample" ? owned.All(path => QGF.Scene.SceneIsLoaded(path))
                    : QGF.Scene.SceneIsLoaded(owned[0]) && (name != "SwitchRoomsExample" || !Field<bool>(owner, "changing")), "Owned rooms loaded");
                if (camera != null)
                {
                    Field<Toggle>(owner, "cameraTagToggle").isOn = tag != "MainCamera";
                    Require(camera.tag != tag, "Camera tag changed by example");
                }
                if (mode == 2)
                {
                    Field<Button>(owner, "switchButton").onClick.Invoke();
                    Require(QGF.Scene.SceneIsUnloading(owned[0]), "Destroy during old-room unload");
                }
                UnityEngine.Object.Destroy(owner.gameObject);
            }
            await Until(() => owned.All(path => !QGF.Scene.SceneIsLoaded(path) && !QGF.Scene.SceneIsLoading(path) && !QGF.Scene.SceneIsUnloading(path)), "Owned loaded and pending rooms removed");
            await Task.Delay(250);
            Require(QGF.Scene.SceneIsLoaded(outside) && QGF.Scene.GetLoadedSceneAssetNames().Length == 1, "Unrelated room preserved");
            if (camera != null) Require(camera.tag == tag && QGF.Scene.MainCamera == Camera.main, "Camera tag and cache restored");
        }
        finally { EditorApplication.update -= destroyPending; }
    }

    private static string ScenePath(JObject layouts, string name)
    {
        string source = (string)layouts[name]["script"];
        string module = source.Split('/')[2];
        string guid = module == "14_UI" ? "cad20e62ad474103ab805e69424a0958" : module == "15_Sound" ? "bead710ed85a4e19b16e4e6815fc7e14" : "bdd615d3777d46c487aeeb6777122b02";
        string root = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guid)).Replace('\\', '/');
        string script = root + source.Substring(("Assets/Examples/" + module).Length);
        return script.Substring(0, script.Length - 3) + ".unity";
    }

    private static void SetLoadDelay(float seconds)
    {
        var resource = UnityEngine.Object.FindFirstObjectByType<EditorResourceComponent>();
        var data = new SerializedObject(resource);
        data.FindProperty("m_MinLoadAssetRandomDelaySeconds").floatValue = seconds;
        data.FindProperty("m_MaxLoadAssetRandomDelaySeconds").floatValue = seconds;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(instance);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); assertions.Add(message); }
    private static string Hash(string path) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static async Task Until(Func<bool> condition, string message)
    {
        var end = DateTime.UtcNow.AddSeconds(8);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(10);
        Require(condition(), message);
    }
    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state => { if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true); };
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
