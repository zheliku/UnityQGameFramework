using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// 只在 Artifacts~ 内有明确标记的消费工程中运行，绝不卸载开发工程的示例。
public static class ExampleConsumerValidation
{
    private const string StatePath = "Artifacts~/ExamplesMigration/current.json";
    private static readonly string[] Settings = { "ProjectSettings/GraphicsSettings.asset", "ProjectSettings/QualitySettings.asset",
        "ProjectSettings/TagManager.asset", "ProjectSettings/ProjectSettings.asset", "ProjectSettings/EditorBuildSettings.asset" };

    private sealed class State
    {
        public string folder, anchor, importedPath, movedPath;
        public Dictionary<string, string> settings;
        public bool imported, independent, moved, played, removed, settingsUnchanged;
        public bool profileSelected, profileReset;
        public int scenes;
    }

    public static object ImportOne(string folder)
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var package = PackageInfo.GetAllRegisteredPackages().Single(item => item.name == "com.zheliku.unityqgameframework");
        if (package.source != PackageSource.Git)
            throw new Exception("Expected real Git installation");
        var available = Sample.FindByPackage(package.name, package.version).ToArray();
        var manifest = JObject.Parse(File.ReadAllText(package.resolvedPath + "/package.json"));
        var registrations = (JArray)manifest["samples"];
        Require(available.Length == 3 && available.Length == registrations.Count, "Three Sample registrations");
        if (available.Any(sample => sample.isImported))
            throw new Exception("Another Sample is still imported");
        if (PackageInfo.GetAllRegisteredPackages().Any(item => item.name == "com.annulusgames.alchemy" || item.name == "com.borodar.rainbow-folders"))
            throw new Exception("Optional tools must be absent from the consumer");
        var registration = manifest["samples"].Single(item => (string)item["path"] == "Samples~/" + folder);
        var sampleToImport = available.Single(item => item.displayName == (string)registration["displayName"]);
        string anchor = folder == "00_PointPointPoint" ? "f99fe5fffa1724e4ab21a8936d44ab7e"
            : folder == "02_StarForce" ? "9d2d51bf0fc3dfc42908e3a7a64f4feb"
            : folder == "01_Modules" ? "26a4a9304f1b571a97885965a89aaed1"
            : throw new ArgumentException("Unknown Sample: " + folder, nameof(folder));
        var state = new State { folder = folder, anchor = anchor, settings = Settings.ToDictionary(path => path, Hash) };
        EditorApplication.LockReloadAssemblies();
        try
        {
            if (!sampleToImport.Import())
                throw new Exception("Sample.Import failed");
            state.importedPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), sampleToImport.importPath).Replace('\\', '/');
            state.imported = true;
            // 脚本导入会触发重载，先记录导入位置，之后再检查目录移动。
            Save(state);
        }
        finally { EditorApplication.UnlockReloadAssemblies(); }
        return new
        {
            imported = true,
            folder,
            package.source,
            package.packageId,
            sampleToImport.importPath,
            registrations = available.Length
        };
    }

    public static object MoveOne()
    {
        Guard();
        var state = Load();
        string root = Root(state);
        if (!state.imported || state.moved)
            throw new Exception("Unexpected move stage");
        // 先验证实际依赖，不能用“导入后未报编译错误”代替独立性检查。
        var dependencies = AssetDatabase.GetDependencies(Directory.GetFiles(root, "*.unity", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/')).ToArray(), true);
        var external = dependencies.Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)
            && !path.StartsWith(root + "/", StringComparison.Ordinal)).ToArray();
        if (external.Length != 0)
            throw new Exception("External Assets dependency: " + string.Join(", ", external));
        state.independent = true;
        state.movedPath = "Assets/Example_" + state.folder;
        if (Directory.Exists(state.movedPath))
            throw new Exception("Move target exists");
        string error = AssetDatabase.MoveAsset(root, state.movedPath);
        if (!string.IsNullOrEmpty(error))
            throw new Exception(error);
        if (Root(state) != state.movedPath)
            throw new Exception("Anchor did not follow move");
        state.moved = true;
        Save(state);
        return new
        {
            state.folder,
            state.independent,
            state.moved,
            state.movedPath
        };
    }

    public static Task<object> PlayOne() => PlaySelected();

    private static async Task<object> PlaySelected()
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var state = Load();
        if (!state.moved)
            throw new Exception("Move Sample before playing");
        bool moduleSample = IsModuleSample(state.folder);
        string root = Root(state);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool originalEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        bool originalBackground = Application.runInBackground;
        var originalOptions = EditorSettings.enterPlayModeOptions;
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        int completed = 0;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            string[] scenes = state.folder == "02_StarForce" ? new[] { root + "/StarForce Launcher.unity" }
                : Directory.GetFiles(root, "*.unity", SearchOption.AllDirectories).Select(path => path.Replace('\\', '/'))
                    .Where(path => !path.Contains("/Art/")).OrderBy(path => path).ToArray();
            foreach (string scene in scenes)
            {
                EditorSceneManager.OpenScene(scene);
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                await Task.Delay(state.folder == "02_StarForce" ? 4500 : 1200);
                // 严格引用池实验故意不启动框架，才能在没有模块持有引用时演示 ClearAll。
                bool coreOnly = Path.GetFileNameWithoutExtension(scene) == "ReferencePoolChecks";
                Require((GameEntry.GetComponent<BaseComponent>() == null) == coreOnly, "Expected bootstrap: " + scene);
                if (state.folder == "00_PointPointPoint")
                    Require(QGF.Procedure.CurrentProcedure.GetType().Name == "PointGameMenu", "Clicker menu");
                else if (state.folder == "02_StarForce")
                {
                    var menuType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("StarForce.MenuForm")).First(type => type != null);
                    Require(UnityEngine.Object.FindFirstObjectByType(menuType) != null, "StarForce menu after move");
                }
                else
                {
                    string exampleNamespace = "UnityQGameFramework.Examples.Modules.";
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(assembly => assembly.GetType(exampleNamespace + Path.GetFileNameWithoutExtension(scene)))
                        .First(candidate => candidate != null);
                    var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                    Require(example != null, "Example instance");
                    if (moduleSample)
                    {
                        if (!coreOnly)
                            Require(GameEntry.GetComponent<BaseComponent>().gameObject.name == "QGF", "Shared QGF root");
                        var serialized = new SerializedObject(example);
                        var field = serialized.GetIterator();
                        while (field.NextVisible(true))
                            if (field.propertyType == SerializedPropertyType.ObjectReference)
                                Require(field.objectReferenceValue != null, "Bound field: " + scene + ":" + field.propertyPath);
                        foreach (var image in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                            if (image.sprite != null && (image.sprite.name == "ButtonOutline" || image.sprite.name == "Checkmark"))
                                Require(AssetDatabase.GetAssetPath(image.sprite).StartsWith(root + "/Common/UI/UISprites/", StringComparison.Ordinal), "Shared UI sprite");
                        var ui = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"))[type.Name];
                        var definition = ModuleEntries().Single(item => ((string)ui["script"]).StartsWith(
                            "Assets/Examples/" + (string)item["folder"] + "/", StringComparison.Ordinal));
                        string sourceModule = (string)definition["folder"];
                        bool responsive = (bool?)definition["responsiveLayout"] == true && (bool?)ui["fullWidthPanel"] != true
                            && (!sourceModule.EndsWith("/15_Sound", StringComparison.Ordinal) || (bool?)ui["spatialSound"] == true);
                        if (responsive)
                        {
                            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                                .Single(item => item.gameObject.name == "Canvas_Example");
                            Require(canvas.GetComponents<MonoBehaviour>().All(item => item == null || item.GetType().Name != "ExampleSceneLayout"),
                                "Example layout is stored in the scene.");
                        }
                    }
                }
                await Play(false);
                Require(GameEntry.GetComponent<BaseComponent>() == null, "No stale Base after stop");
                completed++;
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = originalEnabled;
            Application.runInBackground = originalBackground;
            EditorSettings.enterPlayModeOptions = originalOptions;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        if (errors.Count != 0)
            throw new Exception(string.Join("\n", errors));
        state.played = true;
        state.scenes = completed;
        Save(state);
        return new
        {
            passed = true,
            state.folder,
            state.scenes,
            afterMove = true,
            scope = state.folder == "02_StarForce" ? "Launcher startup to Menu" : "all scenes in Sample"
        };
    }

    public static object RemoveOne() => Remove(true);

    public static object SelectProfile()
    {
        Guard();
        var state = Load();
        string root = Root(state);
        string[] profiles = AssetDatabase.FindAssets("t:LubanProfile", new[] { root });
        Require(profiles.Length == 1, "Expected one Sample profile");
        var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(AssetDatabase.GUIDToAssetPath(profiles[0]));
        LubanSettings.instance.Profile = profile;
        Require(LubanSettings.instance.Profile == profile, "Sample profile selected globally");
        return new
        {
            selected = AssetDatabase.GetAssetPath(profile)
        };
    }

    // 替换尚未运行的隔离导入副本，仍检查删除后宿主配置和工作区保持不变。
    public static object RemoveIntermediate() => Remove(false);

    private static object Remove(bool requirePlayed)
    {
        Guard();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var state = Load();
        if (requirePlayed && !state.played)
            throw new Exception("Play before uninstall");
        string root = Root(state);
        state.profileSelected = AssetDatabase.GetAssetPath(LubanSettings.instance.Profile)
            .StartsWith(root + "/", StringComparison.Ordinal);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // 与 Project 窗口删除目录使用同一 AssetDatabase 接口，无需框架追踪或卸载菜单。
        Require(AssetDatabase.DeleteAsset(root), "Sample folder removal failed");
        state.removed = !Directory.Exists(root);
        state.settingsUnchanged = state.settings.All(item => Hash(item.Key) == item.Value);
        if (state.profileSelected)
        {
            Require(LubanSettings.instance.Profile == null, "Deleted Sample profile must no longer resolve");
        }

        state.profileReset = !state.profileSelected || LubanSettings.instance.Profile == null;
        Require(state.removed && state.settingsUnchanged, "Sample deletion and host settings invariants");
        Require(state.profileReset, "Removing a selected Sample must not assign another global Profile");
        Save(state);
        File.WriteAllText("Artifacts~/ExamplesMigration/" + state.folder + ".json", JsonConvert.SerializeObject(state, Formatting.Indented));
        return new
        {
            passed = true,
            state.folder,
            state.removed,
            state.settingsUnchanged,
            state.profileSelected,
            state.profileReset
        };
    }

    private static string Root(State state)
    {
        string asset = AssetDatabase.GUIDToAssetPath(state.anchor);
        if (!asset.StartsWith("Assets/", StringComparison.Ordinal))
            throw new Exception("Sample anchor missing: " + state.folder);
        const string pointScene = "/GameMain/Scenes/PointPointPoint.unity";
        if (state.folder == "00_PointPointPoint" && asset.EndsWith(pointScene, StringComparison.Ordinal))
            return asset.Substring(0, asset.Length - pointScene.Length);
        return Path.GetDirectoryName(asset).Replace('\\', '/');
    }

    private static JArray ModuleEntries() => JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));

    private static bool IsModuleSample(string folder) => folder == "01_Modules";

    private static void Guard()
    {
        string root = Path.GetFullPath(".");
        if (!root.Contains(Path.DirectorySeparatorChar + "Artifacts~" + Path.DirectorySeparatorChar)
            || !File.Exists("QgfExampleConsumer~.json"))
            throw new Exception("Disposable consumer marker missing");
    }
    private static void Require(bool condition, string step)
    {
        if (!condition)
            throw new Exception(step);
    }
    private static void Save(State state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
        File.WriteAllText(StatePath, JsonConvert.SerializeObject(state, Formatting.Indented));
    }
    private static State Load() => JsonConvert.DeserializeObject<State>(File.ReadAllText(StatePath));
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
