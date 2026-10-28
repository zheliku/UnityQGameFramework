using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityQGameFramework.Examples.PointPointPoint;
using UnityQGameFramework.Runtime;

public static class ValidatePointGame
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Mouse testMouse;
    private static readonly HashSet<int> FormInstances = new HashSet<int>();
    private static readonly HashSet<int> FormSerials = new HashSet<int>();
    private static readonly Dictionary<int, int> FirstFormSerial = new Dictionary<int, int>();
    private static bool reusedForm;

    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new Exception("Unsaved scene");
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        var inputSettings = InputSystem.settings;
        var oldBackgroundBehavior = inputSettings.backgroundBehavior;
        var oldEditorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
        FormInstances.Clear();
        FormSerials.Clear();
        FirstFormSerial.Clear();
        reusedForm = false;
        var disabledPointers = new HashSet<InputDevice>();
        byte[] savedRecord = File.Exists(PointGameSettingHelper.SavePath) ? File.ReadAllBytes(PointGameSettingHelper.SavePath) : null;
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Assert || kind == LogType.Exception)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        int record;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testMouse = InputSystem.AddDevice<Mouse>();
            EditorSceneManager.OpenScene("Assets/Examples/00_PointPointPoint/GameMain/Scenes/PointPointPoint.unity");
            await Play(true);
            foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != testMouse && device.enabled).ToArray())
            {
                disabledPointers.Add(pointer);
                InputSystem.DisableDevice(pointer);
            }
            PointGameView view = null;
            await Wait(() => (view = UnityEngine.Object.FindFirstObjectByType<PointGameView>()) != null
                && (string)Get(Get(view, "Game"), "Phase") == "Menu", "launch to menu");
            var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            gameView.Focus();
            object game = Get(view, "Game");
            Require(GameEntry.GetComponent<BaseComponent>().gameObject.name == "QGF"
                && (string)Get(game, "FormAsset") == "Assets/Examples/00_PointPointPoint/GameMain/UI/UIForms/PointGameView.prefab", "QGF root and sample resource paths");
            Require(QGF.Procedure.CurrentProcedure is PointGameMenu, "Actual menu procedure");
            Require((int)Get(game, "TargetCount") == 10 && (float)Get(game, "Duration") == 20, "Rules read from TOML");
            QGF.Setting.RemoveAllSettings();
            Set(game, "BestScore", 0);
            await Screenshot("point-menu");
            await Click(Button("startButton"));
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGamePlay, "menu to play");
            var board = (RectTransform)Get(view, "board");
            var target = (RectTransform)Get(view, "target");
            Require(Math.Abs(target.rect.width - 64) < 1 && board.rect.Contains(board.InverseTransformPoint(target.position)),
                "Styled target remains inside board after import and move");
            float before = (float)Get(game, "Remaining");
            await Task.Delay(300);
            Require((float)Get(game, "Remaining") < before, "Countdown updates");
            await Screenshot("point-play");
            for (int i = 0; i < 10; i++)
            {
                await Click(Button("targetButton"));
                Require((int)Get(game, "Hits") == i + 1, "Native target hit " + (i + 1));
            }
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGameResult, "win to result");
            Require((int)Get(game, "Hits") == 10 && (int)Get(game, "Score") == 100, "Hits and score");
            Require(QGF.Setting.GetInt("BestScore") == 100 && File.Exists(PointGameSettingHelper.SavePath), "Record saved to disk");
            Require(QGF.Event.Count(PointGameClick.EventId) == 0, "Click handler removed");
            await Screenshot("point-result");
            await Play(false);
            await Play(true);
            foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != testMouse && device.enabled).ToArray())
            {
                disabledPointers.Add(pointer);
                InputSystem.DisableDevice(pointer);
            }
            await Wait(() => (view = UnityEngine.Object.FindFirstObjectByType<PointGameView>()) != null
                && QGF.Procedure.CurrentProcedure is PointGameMenu, "restart to menu");
            game = Get(view, "Game");
            record = (int)Get(game, "BestScore");
            Require(record == 100, "Record loaded on a second run");
            await Click(Button("startButton"));
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGamePlay, "second round");
            QGF.Event.Fire(new object(), PointGameClick.Create(true));
            await Task.Delay(100);
            Require((int)Get(game, "Hits") == 0, "Ignore another sender");
            for (int i = 0; i < 3; i++)
            {
                await Click((RectTransform)Get(UnityEngine.Object.FindFirstObjectByType<PointGameView>(), "board"), true);
            }
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGameResult, "misses to result");
            Require((int)Get(game, "Lives") == 0 && (int)Get(game, "Score") == -15, "Miss penalty");
            await Click(Button("menuButton"));
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGameMenu, "return to menu");
            await Click(Button("startButton"));
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGamePlay, "third round");
            Set(game, "Remaining", 0.01f);
            await Wait(() => QGF.Procedure.CurrentProcedure is PointGameResult, "timeout to result");
            Require((string)Get(game, "Result") == "时间到", "Timeout result");
            Require(QGF.Event.EventHandlerCount == 0, "Unsubscribed click handlers");
            Require(reusedForm, "A single UIForm instance is reused under a new open serial ID");
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            if (testMouse != null && testMouse.added)
                InputSystem.RemoveDevice(testMouse);
            foreach (var pointer in disabledPointers.Where(device => device.added && !device.enabled))
                InputSystem.EnableDevice(pointer);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            inputSettings.backgroundBehavior = oldBackgroundBehavior;
            inputSettings.editorInputBehaviorInPlayMode = oldEditorInputBehavior;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (savedRecord != null)
                File.WriteAllBytes(PointGameSettingHelper.SavePath, savedRecord);
            else if (File.Exists(PointGameSettingHelper.SavePath))
                File.Delete(PointGameSettingHelper.SavePath);
        }
        if (errors.Count != 0)
            throw new Exception(string.Join("\n", errors));
        var result = new
        {
            passed = true,
            record,
            rounds = 3,
            win = true,
            miss = true,
            timeout = true,
            persistedAcrossPlaySessions = true,
            handlersRemoved = true,
            screenshots = 3,
            nativeMouseInput = true,
            uiModule = true,
            formInstances = FormInstances.Count,
            formOpenSerials = FormSerials.Count,
            reusedForms = true
        };
        Directory.CreateDirectory("Artifacts~/Examples");
        File.WriteAllText("Artifacts~/Examples/point-game-check.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    private static object Get(object target, string name) => target.GetType().GetField(name, Fields)?.GetValue(target)
        ?? target.GetType().GetProperty(name, Fields)?.GetValue(target);
    private static Button Button(string name)
    {
        var view = UnityEngine.Object.FindFirstObjectByType<PointGameView>();
        FormInstances.Add(view.GetInstanceID());
        FormSerials.Add(view.UIForm.SerialId);
        if (FirstFormSerial.TryGetValue(view.GetInstanceID(), out int first))
            reusedForm |= first != view.UIForm.SerialId;
        else
            FirstFormSerial.Add(view.GetInstanceID(), view.UIForm.SerialId);
        return (Button)Get(view, name);
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private static void Require(bool condition, string step)
    {
        if (!condition)
            throw new Exception(step);
    }
    private static async Task Click(Component element, bool emptyCorner = false)
    {
        var rect = (RectTransform)element.transform;
        var local = emptyCorner ? new Vector2(rect.rect.xMin + 4, rect.rect.yMin + 4) : rect.rect.center;
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
        bool received = false;
        var button = element.GetComponent<Button>();
        UnityEngine.Events.UnityAction observed = () => received = true;
        button.onClick.AddListener(observed);
        InputSystem.QueueStateEvent(testMouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(90);
        InputSystem.QueueStateEvent(testMouse, new MouseState { position = position, buttons = 1 });
        await Task.Delay(100);
        InputSystem.QueueStateEvent(testMouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(160);
        button.onClick.RemoveListener(observed);
        Require(received, "Native button click " + button.name);
    }
    private static async Task Wait(Func<bool> condition, string step)
    {
        double started = EditorApplication.timeSinceStartup;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup - started > 10)
                throw new TimeoutException(step);
            await Task.Delay(30);
        }
    }
    private static async Task Screenshot(string name)
    {
        Directory.CreateDirectory("Artifacts~/Examples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/Examples/" + name + ".png"));
        await Task.Delay(250);
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
