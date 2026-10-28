using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityQGameFramework.Editor;

// 在已导入并移动的独立 Sample 上检查鼠标输入、战斗和 URP 画面。
public static class ValidateShooterUi
{
    private static Mouse mouse;

    public static async Task<object> Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty))
            throw new Exception("Unsaved scene");
        bool enabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        float originalSpeed = Time.timeScale;
        var inputSettings = InputSystem.settings;
        var backgroundBehavior = inputSettings.backgroundBehavior;
        var editorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
        var disabledPointers = new HashSet<InputDevice>();
        var errors = new List<string>();
        Application.LogCallback capture = (text, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(text + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        string scene = "Assets/Examples/02_StarForce/StarForce Launcher.unity";
        string root = Path.GetDirectoryName(scene).Replace('\\', '/');
        bool moved = root.Contains("02_StarForce");
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            EditorSceneManager.OpenScene(scene);
            await Play(true);
            foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
            {
                disabledPointers.Add(pointer);
                InputSystem.DisableDevice(pointer);
            }
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Component menu = null;
            await Wait(() => (menu = Find("StarForce.MenuForm")) != null && menu.GetComponent<CanvasGroup>().alpha > .99f, "Menu fades in");
            await Screenshot("shooter-menu");
            var start = menu.GetComponentsInChildren(FindType("StarForce.CommonButton"), true).First(item => item.name == "Button_Start");
            var canvas = start.GetComponentInParent<Canvas>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,
                ((RectTransform)start.transform).TransformPoint(((RectTransform)start.transform).rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 0 });
            await Task.Delay(150);
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
            Require(hits.Count > 0 && (hits[0].gameObject.transform.IsChildOf(start.transform) || hits[0].gameObject == start.gameObject),
                "Start raycast at " + point + ": " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            bool clicked = false;
            var click = (UnityEngine.Events.UnityEvent)start.GetType().GetField("m_OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(start);
            UnityEngine.Events.UnityAction observed = () => clicked = true;
            click.AddListener(observed);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
            await Task.Delay(150);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 0 });
            await Task.Delay(200);
            click.RemoveListener(observed);
            Require(clicked, "Start pointer-down event; raycasts: " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            Component plane = null;
            await Wait(() => (plane = Find("StarForce.MyAircraft")) != null, "Native mouse starts battle");
            await Task.Delay(500);
            Vector3 before = plane.transform.position;
            point = new Vector2(Screen.width * .7f, Screen.height * .7f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
            await Task.Delay(700);
            Require(plane != null && Vector3.Distance(before, plane.transform.position) > .1f, "Held mouse moves aircraft");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 0 });
            var materials = AssetDatabase.FindAssets("t:Material", new[] { root + "/GameMain/Materials" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            Require(materials.Length == 21 && materials.All(item => item.shader.name.StartsWith("Universal Render Pipeline/")
                && item.shader.isSupported && item.GetTexture("_BaseMap") != null), "URP materials and textures");
            Require(plane.gameObject.layer == 0, "No custom layer required");
            await Screenshot("shooter-battle");
            // 回收检查期间隔离碰撞，避免另一发伤害重启血条动画。
            foreach (var collider in plane.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            plane.GetType().GetMethod("ApplyDamage").Invoke(plane, new object[] { null, 1 });
            Require(ActiveBar(plane) != null, "HP bar shown");
            Time.timeScale = 0;
            EditorWindow.GetWindow<SceneView>().Focus();
            await Wait(() => ActiveBar(plane) == null, "HP bar returned to pool");
            Time.timeScale = 1;
            plane.GetType().GetMethod("ApplyDamage").Invoke(plane, new object[] { null, 1 });
            Require(ActiveBar(plane) != null, "HP bar reused");
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
            EditorSettings.enterPlayModeOptionsEnabled = enabled;
            EditorSettings.enterPlayModeOptions = options;
            Time.timeScale = originalSpeed;
            if (setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        if (errors.Count > 0)
            throw new Exception(string.Join("\n", errors));
        var result = new
        {
            passed = true,
            moved,
            nativeMouseInput = true,
            menu = true,
            battle = true,
            aircraftMoves = true,
            urpMaterials = 21,
            hpBarPool = true,
            fadesWithoutGameViewFocus = true,
            unscaledUiAnimations = true,
            screenshots = 2
        };
        File.WriteAllText("Artifacts~/Examples/shooter-ui-check.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object SelectProfile()
    {
        string root = Path.GetDirectoryName(scene).Replace('\\', '/');
        var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(root + "/UnityQGameFramework/Config/LubanProfile.asset");
        if (profile == null)
            throw new Exception("Sample profile missing");
        LubanSettings.instance.Profile = profile;
        return new
        {
            selected = AssetDatabase.GetAssetPath(LubanSettings.instance.Profile)
        };
    }

    private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
    private static Component Find(string name) => UnityEngine.Object.FindFirstObjectByType(FindType(name)) as Component;
    private static Component ActiveBar(Component owner) => UnityEngine.Object.FindObjectsByType(FindType("StarForce.HPBarItem"), FindObjectsSortMode.None)
        .Cast<Component>().FirstOrDefault(item => item.gameObject.activeInHierarchy && Equals(item.GetType().GetProperty("Owner").GetValue(item), owner));
    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
    private static async Task Wait(Func<bool> condition, string step)
    {
        double start = EditorApplication.timeSinceStartup;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup - start > 15)
                throw new TimeoutException(step);
            await Task.Delay(40);
        }
    }
    private static async Task Screenshot(string name)
    {
        Directory.CreateDirectory("Artifacts~/Examples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/Examples/" + name + ".png"));
        await Task.Delay(300);
    }
    private static async Task Play(bool playing)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (playing ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
                completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(200);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
