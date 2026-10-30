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
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

// 隔离工程验收：保留框架，只销毁控制脚本，检查公开属性与 Unity 状态是否一起恢复。
public static class ValidateBaseExamples
{
    private static readonly List<string> assertions = new List<string>();
    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        float originalTimeScale = Time.timeScale;
        int originalFrameRate = Application.targetFrameRate, originalSleep = Screen.sleepTimeout;
        bool originalBackground = Application.runInBackground;
        var errors = new List<string>();
        var completed = new List<object>();
        var savedScenes = new Dictionary<string, string>();
        assertions.Clear();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            foreach (string name in new[] { "GameSpeedExample", "PauseGameExample", "ApplicationSettingsExample" }.Where(name => names == null || names.Contains(name)))
                for (int mode = 0; mode < 4; mode++)
                {
                    int start = assertions.Count;
                    string source = (string)layouts[name]["script"];
                    string root = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath("e638c6e0da015f4d99bdc5c5491facc0")).Replace('\\', '/');
                    string scene = root + source.Substring("Assets/Examples/01_Modules".Length);
                    scene = scene.Substring(0, scene.Length - 3) + ".unity";
                    savedScenes[scene] = Hash(scene);
                    EditorSceneManager.OpenScene(scene);
                    Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + name)).First(candidate => candidate != null);
                    if (mode == 2)
                        ((MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type)).enabled = false;
                    // 不保存测试值：自定义初始速度可发现 Awake 早于框架初始化时的错误快照。
                    if (mode != 0)
                    {
                        var fixture = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<BaseComponent>());
                        fixture.FindProperty("m_GameSpeed").floatValue = .75f;
                        fixture.FindProperty("m_FrameRate").intValue = 60;
                        fixture.FindProperty("m_RunInBackground").boolValue = true;
                        fixture.FindProperty("m_NeverSleep").boolValue = false;
                        fixture.ApplyModifiedPropertiesWithoutUndo();
                    }
                    await Play(true);
                    Application.runInBackground = true;
                    EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                    var owner = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                    var framework = QGF.Base;
                    Require(owner != null && framework != null, name + " controller and framework");
                    if (mode != 2)
                        await Until(() => Field<bool>(owner, "ready"), "Controller initialized");
                    float speed = framework.GameSpeed;
                    int frameRate = framework.FrameRate;
                    bool background = framework.RunInBackground, sleep = framework.NeverSleep;
                    if (mode == 2)
                    {
                        Require(!Field<bool>(owner, "ready"), "Disabled controller never started");
                        framework.GameSpeed = speed = 1.25f;
                        framework.FrameRate = frameRate = 45;
                        framework.RunInBackground = background = true;
                        framework.NeverSleep = sleep = true;
                    }
                    else
                        await Interact(name, owner);
                    if (mode == 3)
                        UnityEngine.Object.DestroyImmediate(framework.gameObject);
                    UnityEngine.Object.DestroyImmediate(owner);
                    Require(Mathf.Approximately(Time.timeScale, speed), "Unity time scale restored or left untouched before Start");
                    Require(Application.targetFrameRate == frameRate && Application.runInBackground == background
                        && Screen.sleepTimeout == (sleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting), "Unity application parameters restored");
                    if (mode != 3)
                    {
                        Require(GameEntry.GetComponent<BaseComponent>() == framework, "Framework remains alive");
                        Require(Mathf.Approximately(framework.GameSpeed, speed) && framework.IsGamePaused == (speed <= 0)
                            && framework.IsNormalGameSpeed == (speed == 1), "Component speed and pause flags agree with Unity");
                        Require(framework.FrameRate == frameRate && framework.RunInBackground == background && framework.NeverSleep == sleep,
                            "Component application parameters agree with Unity");
                        // 恢复后继续使用同一个组件，检查暂停/恢复及正常速度重置没有沿用旧状态。
                        framework.PauseGame();
                        framework.ResumeGame();
                        Require(Mathf.Approximately(Time.timeScale, speed), "Pause and resume after controller destruction");
                        framework.ResetNormalGameSpeed();
                        Require(framework.IsNormalGameSpeed && Mathf.Approximately(Time.timeScale, 1), "Normal-speed reset after controller destruction");
                    }
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
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Time.timeScale = originalTimeScale;
            Application.targetFrameRate = originalFrameRate;
            Application.runInBackground = originalBackground;
            Screen.sleepTimeout = originalSleep;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Five host settings unchanged");
        Require(savedScenes.All(item => Hash(item.Key) == item.Value), "Saved scenes unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            runs = completed,
            assertionCount = assertions.Count,
            interaction = "Programmatic listeners for exact teardown timing; native buttons checked separately",
            settingsUnchanged = true,
            savedScenesUnchanged = true
        };
        Directory.CreateDirectory("Artifacts~/BaseOwnershipExamples");
        File.WriteAllText("Artifacts~/BaseOwnershipExamples/" + (names == null ? "base" : string.Join("-", names)) + "-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
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

    private static async Task Interact(string name, MonoBehaviour owner)
    {
        if (name == "GameSpeedExample")
        {
            Field<Button>(owner, "fastButton").onClick.Invoke();
            Require(QGF.Base.GameSpeed == 2 && Time.timeScale == 2, "Fast button");
            Field<Button>(owner, "slowButton").onClick.Invoke();
            Require(QGF.Base.GameSpeed == .5f && Time.timeScale == .5f, "Slow button");
            Field<Button>(owner, "normalButton").onClick.Invoke();
            Require(QGF.Base.IsNormalGameSpeed && Time.timeScale == 1, "Normal button");
            Field<Slider>(owner, "speedSlider").value = .25f;
            // 倍率在帧中修改，先越过该帧再比较两个时钟，避免把旧 deltaTime 算进样本。
            int frame = Time.frameCount;
            await Until(() => Time.frameCount >= frame + 3, "Speed change settled across frames");
            float game = Field<float>(owner, "gameSeconds"), real = Field<float>(owner, "realSeconds");
            frame = Time.frameCount;
            await Until(() => Time.frameCount >= frame + 8, "Clock sample spans eight frames");
            float gameDelta = Field<float>(owner, "gameSeconds") - game, realDelta = Field<float>(owner, "realSeconds") - real;
            Require(realDelta > .1f && Mathf.Abs(gameDelta / realDelta - .25f) < .06f,
                $"Game and real clocks show selected ratio: game={gameDelta:F4}, real={realDelta:F4}, scale={Time.timeScale:F2}");
            Require(Field<TMP_Text>(owner, "speedValueText").text.Contains("0.25"), "Live speed label");
        }
        else if (name == "PauseGameExample")
        {
            Field<Button>(owner, "speedButton").onClick.Invoke();
            if (QGF.Base.GameSpeed != .5f)
                Field<Button>(owner, "speedButton").onClick.Invoke();
            Field<Button>(owner, "pauseButton").onClick.Invoke();
            Require(QGF.Base.IsGamePaused && Time.timeScale == 0, "Pause uses component and Unity");
            int frame = Time.frameCount;
            await Until(() => Time.frameCount >= frame + 3, "Pause settled across frames");
            float game = Field<float>(owner, "gameSeconds"), real = Field<float>(owner, "realSeconds");
            frame = Time.frameCount;
            await Until(() => Time.frameCount >= frame + 8, "Paused clock sample spans eight frames");
            Require(Mathf.Approximately(game, Field<float>(owner, "gameSeconds")) && Field<float>(owner, "realSeconds") > real + .1f,
                "Pause freezes game clock while real clock advances");
            Require(Field<TMP_Text>(owner, "stateText").text == "已暂停", "Live paused label");
            Field<Button>(owner, "pauseButton").onClick.Invoke();
            Require(QGF.Base.GameSpeed == .5f && Time.timeScale == .5f, "Resume remembers half speed");
            Field<Button>(owner, "pauseButton").onClick.Invoke();
        }
        else
        {
            foreach (var pair in new[] { ("fps30Button", 30), ("fps60Button", 60), ("unlimitedButton", -1) })
            {
                Field<Button>(owner, pair.Item1).onClick.Invoke();
                Require(QGF.Base.FrameRate == pair.Item2 && Application.targetFrameRate == pair.Item2, "Frame-rate button " + pair.Item2);
            }
            await Task.Delay(100);
            Require(Field<TMP_Text>(owner, "fpsText").text.Contains("-1"), "Live frame-rate label");
            Field<Toggle>(owner, "backgroundToggle").isOn = !QGF.Base.RunInBackground;
            Field<Toggle>(owner, "neverSleepToggle").isOn = !QGF.Base.NeverSleep;
            Require(QGF.Base.RunInBackground == Application.runInBackground && Screen.sleepTimeout == (QGF.Base.NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting),
                "Application toggles update component and Unity together");
        }
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
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
        var end = DateTime.UtcNow.AddSeconds(6);
        while (!condition() && DateTime.UtcNow < end)
            await Task.Delay(20);
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
