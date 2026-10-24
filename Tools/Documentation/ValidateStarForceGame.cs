using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarForce;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;
using Entry = StarForce.GameEntry;
using FrameworkEntry = UnityQGameFramework.Runtime.GameEntry;

// 只在隔离工程运行。菜单和飞行通过 Input System 鼠标输入；伤害由测试注入。
public static class ValidateStarForceGame
{
    private static Mouse mouse;
    private static int bullets;

    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(scene => scene.isLoaded && !string.IsNullOrEmpty(scene.path)
            && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty)) throw new Exception("Unsaved scene.");
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        var input = InputSystem.settings;
        var background = input.backgroundBehavior;
        var editorInput = input.editorInputBehaviorInPlayMode;
        bool runInBackground = Application.runInBackground;
        var disabledPointers = new HashSet<InputDevice>();
        string savePath = Path.Combine(Application.persistentDataPath, "GameFrameworkSetting.dat");
        byte[] saved = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
        var errors = new List<string>();
        var warnings = new List<string>();
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Assert || kind == LogType.Exception) errors.Add(message + "\n" + stack);
            else if (kind == LogType.Warning) warnings.Add(message);
        };
        Application.logMessageReceived += capture;
        bullets = 0;
        bool subscribed = false;
        bool secondAircraftIdle = false;
        int jsonChecks = 0;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath("9d2d51bf0fc3dfc42908e3a7a64f4feb"));
            await Play(true);
            // 自动化在后台驱动 Game 视图，不能沿用示例的失焦停帧设置。
            Application.runInBackground = true;
            foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
            {
                disabledPointers.Add(pointer);
                InputSystem.DisableDevice(pointer);
            }
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            await Wait(() => Find<MenuForm>() != null && Entry.Procedure.CurrentProcedure is ProcedureMenu, "launch to menu");
            jsonChecks = StarForceJsonChecks.Run();
            Require(Entry.DataTable.GetDataTable<TbScene>().GetDataRow(1) != null, "Luban scene data");
            Require(Entry.Config.GetInt("Scene.Menu") == 1, "TOML config");
            await Screenshot("starforce-menu");

            await Click(Action(Find<MenuForm>(), "OnSettingButtonClick"));
            await Wait(() => Find<SettingForm>() != null, "settings open");
            var settings = Find<SettingForm>();
            var music = (Toggle)Field(settings, "m_MusicMuteToggle");
            bool wasOn = music.isOn;
            await Click(music);
            Require(music.isOn != wasOn && Entry.Sound.IsMuted("Music") == wasOn, "native mute toggle: " + wasOn + " -> " + music.isOn + "; muted=" + Entry.Sound.IsMuted("Music"));
            await Click(music);
            var slider = (Slider)Field(settings, "m_MusicVolumeSlider");
            await Click(slider, 0.62f);
            Require(Entry.Sound.GetVolume("Music") > 0.4f, "native volume slider");
            await Screenshot("starforce-settings");
            await Click(Action(settings, "Close"));
            await Wait(() => Find<SettingForm>() == null, "settings close");

            await Click(Action(Find<MenuForm>(), "OnAboutButtonClick"));
            await Wait(() => Find<AboutForm>() != null, "about open");
            var about = Find<AboutForm>();
            var credits = (RectTransform)Field(about, "m_Transform");
            float creditY = credits.localPosition.y;
            await Task.Delay(250);
            Require(credits.localPosition.y > creditY, "credits scrolling");
            await Click(Action(about, "Close"));
            await Wait(() => Find<AboutForm>() == null, "about close");
            await Click(Action(Find<MenuForm>(), "OnQuitButtonClick"));
            await Wait(() => Find<DialogForm>() != null, "quit dialog");
            await Click(Action(Find<DialogForm>(), "OnCancelButtonClick"));
            await Wait(() => Find<DialogForm>() == null, "quit cancel");

            Entry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnEntity);
            subscribed = true;
            await Click(Action(Find<MenuForm>(), "OnStartButtonClick"));
            await Wait(() => Find<MyAircraft>() != null && ((System.Collections.IList)Field(Find<MyAircraft>(), "m_Weapons")).Count > 0, "aircraft and attached weapon");
            var aircraft = Find<MyAircraft>();
            Vector3 first = aircraft.transform.position;
            Vector3 target = first + new Vector3(2, 0, 2);
            Vector2 point = Camera.main.WorldToScreenPoint(target);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
            await Wait(() => (aircraft.transform.position - first).sqrMagnitude > 0.25f && bullets > 0, "native movement and fire");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 0 });
            await Screenshot("starforce-battle");
            aircraft.ApplyDamage(null, 1);
            Require(((System.Collections.IList)Field(Entry.HPBar, "m_ActiveHPBarItems")).Count > 0, "damage shows pooled HP bar");
            aircraft.ApplyDamage(null, 1000000);
            await Wait(() => Find<MenuForm>() != null && Entry.Procedure.CurrentProcedure is ProcedureMenu, "death returns to menu");
            Require(Find<MyAircraft>() == null && Entry.Scene.GetLoadedSceneAssetNames().Length == 1, "battle cleanup");
            // 第二局直接触发菜单动作，避免按住开始按钮的那几帧被飞机当成移动输入。
            Find<MenuForm>().OnStartButtonClick();
            await Wait(() => Find<MyAircraft>() != null && ((System.Collections.IList)Field(Find<MyAircraft>(), "m_Weapons")).Count > 0, "second battle");
            aircraft = Find<MyAircraft>();
            first = aircraft.transform.position;
            await Task.Delay(200);
            secondAircraftIdle = (aircraft.transform.position - first).sqrMagnitude < 0.01f;
            Require(secondAircraftIdle, "reused aircraft does not inherit old movement target");
            aircraft.ApplyDamage(null, 1000000);
            await Wait(() => Find<MenuForm>() != null && Entry.Procedure.CurrentProcedure is ProcedureMenu, "second return");
            Entry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnEntity);
            subscribed = false;

            await Click(Action(Find<MenuForm>(), "OnSettingButtonClick"));
            await Wait(() => Find<SettingForm>() != null, "language settings");
            settings = Find<SettingForm>();
            Language next = Entry.Localization.Language == Language.Korean ? Language.English : Language.Korean;
            await Click((Toggle)Field(settings, next == Language.Korean ? "m_KoreanToggle" : "m_EnglishToggle"));
            await Click(Action(settings, "OnSubmitButtonClick"));
            await Wait(() => Find<MenuForm>() != null && Entry.Localization.Language == next, "language restart to menu");
            Require(Entry.Setting.GetString(Constant.Setting.Language) == next.ToString(), "language saved");
        }
        catch (Exception exception)
        {
            throw new Exception(exception.Message + "\n" + string.Join("\n", errors), exception);
        }
        finally
        {
            if (subscribed && Entry.Event != null) Entry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnEntity);
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var pointer in disabledPointers.Where(device => device.added && !device.enabled)) InputSystem.EnableDevice(pointer);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            input.backgroundBehavior = background;
            input.editorInputBehaviorInPlayMode = editorInput;
            Application.runInBackground = runInBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (saved != null) File.WriteAllBytes(savePath, saved);
            else if (File.Exists(savePath)) File.Delete(savePath);
        }
        if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
        return new { passed = true, nativeMouseInput = true, settings = true, about = true, quitCancel = true,
            battleRounds = 2, bullets, damageInjection = true, pooledHpBar = true, secondAircraftIdle, languageRestart = true, jsonChecks,
            screenshots = 3, warnings = warnings.Distinct().ToArray() };
    }

    private static void OnEntity(object sender, GameEventArgs args)
    {
        if (((ShowEntitySuccessEventArgs)args).EntityLogicType == typeof(Bullet)) bullets++;
    }
    private static T Find<T>() where T : UnityEngine.Object
    {
        if (typeof(UIFormLogic).IsAssignableFrom(typeof(T)))
            return Entry.UI == null ? null : Entry.UI.GetAllLoadedUIForms().Select(form => form.Logic).OfType<T>().FirstOrDefault();
        if (typeof(EntityLogic).IsAssignableFrom(typeof(T)))
            return Entry.Entity == null ? null : Entry.Entity.GetAllLoadedEntities().Select(entity => entity.Logic).OfType<T>().FirstOrDefault();
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }
    private static object Field(object instance, string name)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(instance);
        }
        throw new Exception("Missing field " + name);
    }
    private static CommonButton Action(Component form, string method) => form.GetComponentsInChildren<CommonButton>()
        .Single(button => Enumerable.Range(0, ((UnityEngine.Events.UnityEvent)Field(button, "m_OnClick")).GetPersistentEventCount())
            .Any(index => ((UnityEngine.Events.UnityEvent)Field(button, "m_OnClick")).GetPersistentMethodName(index) == method));
    private static async Task Click(Component element, float fraction = 0.5f)
    {
        await Task.Delay(400);
        Canvas.ForceUpdateCanvases();
        var rect = element is Toggle toggle ? (RectTransform)toggle.targetGraphic.transform
            : element is Slider slider ? (RectTransform)slider.handleRect.parent : (RectTransform)element.transform;
        var canvas = element.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, fraction), rect.rect.center.y)));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(90);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
        await Task.Delay(100);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
        await Task.Delay(160);
    }
    private static void Require(bool condition, string step) { if (!condition) throw new Exception(step); }
    private static async Task Wait(Func<bool> condition, string step)
    {
        double started = EditorApplication.timeSinceStartup;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup - started > 20) throw new TimeoutException(step + "; procedure=" + Entry.Procedure?.CurrentProcedure?.GetType().Name
                + "; timeScale=" + Time.timeScale + "; frame=" + Time.frameCount + "; playing=" + EditorApplication.isPlaying);
            await Task.Delay(30);
        }
    }
    private static async Task Screenshot(string name)
    {
        await Task.Delay(400);
        Directory.CreateDirectory("Artifacts~/StarForce");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/StarForce/" + name + ".png"));
        await Task.Delay(250);
    }
    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true);
        };
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
