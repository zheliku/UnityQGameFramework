using TMPro;
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
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

/// <summary>
/// 检查示例销毁时的资源归属。
/// </summary>
public static class ValidateExampleOwnership
{
    private static readonly List<string> assertions = new List<string>();
    private static readonly string[] controllers = { "OpenWindowExample", "ReopenWindowExample", "CoverWindowExample",
        "LoadRoomExample", "SwitchRoomsExample" };

    /// <summary>
    /// 检查所有保留示例的资源清理。
    /// </summary>
    /// <returns>资源归属检查结果。</returns>
    public static Task<object> Run()
    {
        return RunOnly(null);
    }

    /// <summary>
    /// 检查指定示例的资源清理。
    /// </summary>
    /// <param name="names">示例名称，为空时检查全部示例。</param>
    /// <returns>资源归属检查结果。</returns>
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
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            foreach (string name in controllers.Where(name => names == null || names.Contains(name)))
            {
                int modes = name == "ReopenWindowExample" || name == "SwitchRoomsExample" ? 3 : 2;
                for (int mode = 0; mode < modes; mode++)
                {
                    int start = assertions.Count;
                    string scene = ScenePath(layouts, name);
                    sceneHashes[scene] = Hash(scene);
                    EditorSceneManager.OpenScene(scene);
                    Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + name)).First(t => t != null);
                    // 不保存：让框架先启动，再建一个不属于示例控制器的对象。
                    ((MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type)).enabled = false;
                    await Play(true);
                    Application.runInBackground = true;
                    var owner = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                    await Task.Delay(250);
                    if (name == "LoadRoomExample" || name == "SwitchRoomsExample")
                        await CheckScene(name, mode, owner, layouts);
                    else
                        await CheckUI(name, mode, owner);
                    await Play(false);
                    Require(errors.Count == 0, name + " runtime errors: " + string.Join("\n", errors));
                    completed.Add(new
                    {
                        name,
                        mode,
                        scene,
                        assertions = assertions.Skip(start).ToArray()
                    });
                }
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Five host settings unchanged");
        Require(sceneHashes.All(item => Hash(item.Key) == item.Value), "Saved source scenes unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            interaction = "Programmatic listeners for destruction timing; functional UI uses separate native-input validators",
            runs = completed,
            assertionCount = assertions.Count,
            settingsUnchanged = true,
            savedScenesUnchanged = true
        };
        Directory.CreateDirectory("Artifacts~/OwnerCleanupExamples");
        File.WriteAllText("Artifacts~/OwnerCleanupExamples/" + (names == null ? "ownership" : string.Join("-", names)) + "-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            result.unity,
            runs = completed.Count,
            result.assertionCount,
            result.settingsUnchanged,
            result.savedScenesUnchanged
        };
    }

    private static async Task CheckUI(string name, int mode, MonoBehaviour owner)
    {
        const string group = "Example";
        QGF.UI.AddUIGroup(group);
        // 使用另一份预制体，避免外部窗口把被测资产预热成同步加载。
        string outsideAsset = name == "OpenWindowExample" ? "UI_Cover_Window" : "UI_OpenWindow_Window";
        int outside = QGF.UI.OpenUIForm(outsideAsset, group, "外部窗口");
        await Until(() => QGF.UI.HasUIForm(outside), "Unrelated window loaded");
        if (mode == 1)
        {
            // 同一调用栈内发出请求并销毁控制器，不靠加载耗时捕捉中间状态。
            // 保持组件禁用，防止 Unity 再自动调用一次 Start。
            owner.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        }
        else
        {
            owner.enabled = true;
            if (name == "OpenWindowExample")
            {
                await Until(() => Field<TMP_Text>(owner, "stateText").text == "窗口已关闭", "Controller initialized");
            }
            else
            {
                await Until(() => Field<bool>(owner, "ready"), "Controller initialized");
            }
        }
        if (name == "OpenWindowExample")
        {
            Field<Button>(owner, "openButton").onClick.Invoke();
        }
        string[] keys = name == "CoverWindowExample" ? new[] { "bottom", "top" } : new[] { "serialId" };
        if (name == "CoverWindowExample" && mode == 0)
        {
            await Until(() => QGF.UI.HasUIForm(Field<int>(owner, "bottom")), "Bottom window loaded");
            Field<Button>(owner, "openButton").onClick.Invoke();
        }
        int[] ids = keys.Select(key => Field<int>(owner, key)).Where(id => id != 0).ToArray();
        Require(ids.Length == (name == "CoverWindowExample" && mode == 1 ? 1 : keys.Length), "Owned request identities recorded");
        if (mode == 1)
            Require(ids.All(id => QGF.UI.IsLoadingUIForm(id)), "Owned windows still loading");
        else
            await Until(() => ids.All(id => QGF.UI.HasUIForm(id)), "Owned windows loaded");
        if (mode == 2)
        {
            Field<Slider>(owner, "idleSlider").value = 2.5f;
            Field<Button>(owner, "reopenButton").onClick.Invoke();
            Require(Field<bool>(owner, "changing") && !QGF.UI.HasUIForm(ids[0]), "Reopen idle wait started");
        }
        if (mode == 1)
        {
            UnityEngine.Object.DestroyImmediate(owner.gameObject);
        }
        else
        {
            UnityEngine.Object.Destroy(owner.gameObject);
        }
        await Task.Delay(mode == 2 ? 2800 : mode == 1 ? 2300 : 150);
        Require(ids.All(id => !QGF.UI.HasUIForm(id) && !QGF.UI.IsLoadingUIForm(id)), "Owned loaded and pending windows removed");
        Require(QGF.UI.GetAllLoadedUIForms().Length == 1 && QGF.UI.HasUIForm(outside), "Unrelated window preserved with no late reopen");
    }

    private static async Task CheckScene(string name, int mode, MonoBehaviour owner, JObject layouts)
    {
        string outsideController = name == "LoadRoomExample" ? "SwitchRoomsExample" : "LoadRoomExample";
        // 使用另一个场景示例的 YooAsset 地址，不在运行中打开另一个示例入口。
        string outside = outsideController == "LoadRoomExample" ? "Scene_LoadRoom_BlueRoom" : "Scene_SwitchRooms_OrangeRoom";
        QGF.Scene.LoadScene(outside);
        await Until(() => QGF.Scene.SceneIsLoaded(outside), "Unrelated room loaded");
        string[] owned = name == "LoadRoomExample" ? new[] { "Scene_LoadRoom_BlueRoom" }
            : new[] { "Scene_SwitchRooms_BlueRoom", "Scene_SwitchRooms_OrangeRoom" };
        // 在禁用的控制器上直接启动请求，以固定销毁时机；不依赖示例私有的就绪标记。
        owner.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        if (mode == 1)
        {
            if (name == "LoadRoomExample")
            {
                Field<Button>(owner, "loadButton").onClick.Invoke();
            }
            Require(owned.Any(path => QGF.Scene.SceneIsLoading(path)), "Owned room request is pending before destruction");
            UnityEngine.Object.DestroyImmediate(owner.gameObject);
        }
        else
        {
            if (name == "LoadRoomExample")
            {
                Field<Button>(owner, "loadButton").onClick.Invoke();
            }
            await Until(() => QGF.Scene.SceneIsLoaded(owned[0]) && (name != "SwitchRoomsExample" || !Field<bool>(owner, "changing")), "Owned rooms loaded");
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
    }

    private static string ScenePath(JObject layouts, string name)
    {
        string source = (string)layouts[name]["script"];
        const string prefix = "Assets/Examples/01_Modules";
        string assembly = AssetDatabase.GUIDToAssetPath("26a4a9304f1b571a97885965a89aaed1");
        if (string.IsNullOrEmpty(assembly) || !source.StartsWith(prefix + "/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot locate module sample: " + source);
        }
        string root = Path.GetDirectoryName(assembly).Replace('\\', '/');
        string script = root + source.Substring(prefix.Length);
        return script.Substring(0, script.Length - 3) + ".unity";
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(instance);
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
    private static async Task Until(Func<bool> condition, string message)
    {
        var end = DateTime.UtcNow.AddSeconds(8);
        while (!condition() && DateTime.UtcNow < end)
            await Task.Delay(10);
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
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
