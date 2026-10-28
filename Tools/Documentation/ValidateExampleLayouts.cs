using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules.Presentation;

/// <summary>
/// 检查模块示例在不同分辨率下的布局。
/// </summary>
public static class ValidateExampleLayouts
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int assertions;

    /// <summary>
    /// 检查控件、窗口和演示物体的可见范围。
    /// </summary>
    /// <returns>布局检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use the isolated validation project in Edit Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty))
            throw new InvalidOperationException("Save scenes first.");
        var paths = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .Select(name => "ProjectSettings/" + name + ".asset").ToDictionary(path => path, Hash);
        var editorAssembly = typeof(Editor).Assembly;
        var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selection = gameViewType.GetProperty("selectedSizeIndex", Fields);
        int oldSelection = (int)selection.GetValue(gameView);
        var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        object sizes = sizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
        object group = sizesType.GetProperty("currentGroup", Fields).GetValue(sizes);
        Type groupType = group.GetType();
        var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        var fixedResolution = Enum.Parse(editorAssembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
        var temporarySizes = new List<int>();
        var resolutions = new[] { new Vector2Int(1280, 720), new Vector2Int(480, 800), new Vector2Int(800, 600) };
        foreach (var resolution in resolutions)
        {
            int index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            object size = Activator.CreateInstance(sizeType, fixedResolution, resolution.x, resolution.y, "QGF layout check");
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
            temporarySizes.Add(index);
        }
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        var input = InputSystem.settings;
        var background = input.backgroundBehavior;
        var editorInput = input.editorInputBehaviorInPlayMode;
        bool originalBackground = Application.runInBackground;
        var disabled = new List<InputDevice>();
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        var results = new List<object>();
        Mouse mouse = null;
        assertions = 0;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (string path in new[] {
                "08_ObjectPool/02_Factory/SpawnOrCreateCube", "13_Entity/02_Reuse/ReuseCubeEntity",
                "15_Sound/05_Spatial/SpatialSoundExample", "14_UI/03_Cover/CoverWindowExample" })
            {
                EditorSceneManager.OpenScene("Assets/Examples/01_Modules/" + path + ".unity");
                await Play(true);
                Application.runInBackground = true;
                gameView.Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled))
                {
                    InputSystem.DisableDevice(pointer);
                    disabled.Add(pointer);
                }
                await Task.Delay(600);
                var layout = UnityEngine.Object.FindObjectsByType<ExampleSceneLayout>(FindObjectsSortMode.None)
                    .Single(item => Get<RectTransform>(item, "controls") != null);
                var example = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Single(item => item.GetType().Name == Path.GetFileName(path));
                var fields = new SerializedObject(example);
                Func<string, Button> button = name => (Button)fields.FindProperty(name).objectReferenceValue;
                if (example.name == "SpawnOrCreateCube")
                    for (int i = 0; i < 6; i++)
                        await Click(button("spawnButton"), mouse);
                if (example.name == "CoverWindowExample")
                {
                    ((Toggle)fields.FindProperty("pauseToggle").objectReferenceValue).isOn = false;
                    await Click(button("openButton"), mouse);
                }
                for (int mode = 0; mode < resolutions.Length; mode++)
                {
                    selection.SetValue(gameView, temporarySizes[mode]);
                    gameView.Repaint();
                    await Task.Delay(500);
                    Require(Screen.width == resolutions[mode].x && Screen.height == resolutions[mode].y, "Requested GameView size");
                    Canvas.ForceUpdateCanvases();
                    bool portrait = Screen.width < Screen.height;
                    var panel = Get<RectTransform>(layout, "controls");
                    var panelBounds = ScreenRect(panel);
                    Require(panelBounds.xMin >= 0 && panelBounds.yMin >= 0 && panelBounds.xMax <= 1 && panelBounds.yMax <= 1, "Controls remain in screen");
                    Require(portrait ? panelBounds.yMin >= .51f && panelBounds.width > .9f : panelBounds.xMax < .5f, "Control arrangement matches aspect");
                    var scroll = panel.GetComponent<ScrollRect>();
                    Require(scroll.verticalScrollbar != null && scroll.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHide,
                        "Scrollable controls have a native scrollbar");
                    if (example.name == "CoverWindowExample")
                    {
                        var windows = UnityEngine.Object.FindObjectsByType<ExampleSceneLayout>(FindObjectsSortMode.None)
                            .Select(item => Get<RectTransform>(item, "window")).Where(item => item != null).ToArray();
                        Require(windows.Length == 2, "Both overlapping windows are present");
                        foreach (var window in windows)
                        {
                            Rect rect = ScreenRect(window);
                            Require(rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= 1 && rect.yMax <= 1, "Window stays in screen");
                            Require(!rect.Overlaps(panelBounds), "Window avoids controls");
                        }
                        Require(Vector2.Distance(windows[0].anchorMin, windows[1].anchorMin) > .05f, "Window offsets survive reflow");
                    }
                    else
                    {
                        Rect area = portrait ? new Rect(.04f, .04f, .92f, Get<RectTransform>(layout, "legend") == null ? .44f : .29f)
                            : new Rect(.53f, .06f, .44f, Get<RectTransform>(layout, "legend") == null ? .88f : .62f);
                        var camera = Get<Camera>(layout, "sceneCamera");
                        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                        Require(renderers.Length >= 1, "Visible demonstration objects");
                        foreach (var renderer in renderers)
                        {
                            Bounds bounds = renderer.bounds;
                            foreach (float x in new[] { bounds.min.x, bounds.max.x })
                                foreach (float y in new[] { bounds.min.y, bounds.max.y })
                                {
                                    Vector3 point = camera.WorldToViewportPoint(new Vector3(x, y, bounds.center.z));
                                    Require(point.z > 0 && area.Contains(point), "Full renderer bounds fit in the observation area: " + renderer.name);
                                }
                        }
                    }
                    string name = Path.GetFileName(path) + "-" + Screen.width + "x" + Screen.height;
                    Directory.CreateDirectory("Artifacts~/ResponsiveExamples");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ResponsiveExamples/" + name + ".png"));
                    await Task.Delay(200);
                    results.Add(new
                    {
                        example = example.name,
                        width = Screen.width,
                        height = Screen.height,
                        portrait
                    });
                }
                await Play(false);
                Require(errors.Count == 0, string.Join("\n", errors));
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
            Application.logMessageReceived -= capture;
            input.backgroundBehavior = background;
            input.editorInputBehaviorInPlayMode = editorInput;
            Application.runInBackground = originalBackground;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            selection.SetValue(gameView, oldSelection);
            foreach (int index in temporarySizes.OrderByDescending(index => index))
                groupType.GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
            gameView.Repaint();
            if (setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(paths.All(item => Hash(item.Key) == item.Value), "Project settings unchanged");
        var report = new
        {
            passed = true,
            assertions,
            nativeButtons = true,
            settingsUnchanged = true,
            scenes = results
        };
        File.WriteAllText("Artifacts~/ResponsiveExamples/check.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    private static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x / Screen.width, corners[0].y / Screen.height, corners[2].x / Screen.width, corners[2].y / Screen.height);
    }
    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        assertions++;
    }
    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    private static async Task Play(bool value)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (value ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
                completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = value;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(200);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
    private static async Task Click(Button button, Mouse mouse)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var scroll = button.GetComponentInParent<ScrollRect>();
        Vector3 local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        if (!scroll.viewport.rect.Contains(local))
        {
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            float offset = scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y;
            if (overflow > 0)
                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(100);
        }
        bool clicked = false;
        UnityEngine.Events.UnityAction observer = () => clicked = true;
        button.onClick.AddListener(observer);
        try
        {
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(180);
            Require(clicked, "Native button: " + button.name);
        }
        finally { button.onClick.RemoveListener(observer); }
    }
}
