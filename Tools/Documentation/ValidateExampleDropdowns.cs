using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// 在隔离工程检查展开后的真实文字网格、鼠标选择和样式，不随示例发布。
public static class ValidateExampleDropdowns
{
    public static object AuditSavedAssets()
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use the isolated authoring project.");
        var modules = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
        var samples = modules.Select(module => new
        {
            name = (string)module["folder"],
            roots = new[] { "Assets/Examples/" + (string)module["folder"], "Assets/Examples/Common" }
        }).ToList();
        samples.Insert(0, new { name = "00_PointPointPoint", roots = new[] { "Assets/Examples/00_PointPointPoint" } });
        samples.Add(new { name = "22_StarForce", roots = new[] { "Assets/Examples/22_StarForce" } });
        var completed = new List<object>();
        foreach (var sample in samples)
        {
            var assets = AssetDatabase.FindAssets("t:Scene t:Prefab", sample.roots).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var external = AssetDatabase.GetDependencies(assets, true)
                .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)
                    && !sample.roots.Any(root => p.StartsWith(root + "/", StringComparison.Ordinal))).ToArray();
            Require(external.Length == 0, "Cross-sample references: " + sample.name + " -> " + string.Join(", ", external));
            completed.Add(new { sample = sample.name, assets = assets.Length, independent = true });
        }
        var result = new { passed = true, samples = completed };
        Directory.CreateDirectory("Artifacts~/SciFiStyle");
        File.WriteAllText("Artifacts~/SciFiStyle/dependencies-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use the isolated authoring project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene.");
        var settings = InputSystem.settings;
        var oldBackground = settings.backgroundBehavior;
        var oldEditorInput = settings.editorInputBehaviorInPlayMode;
        bool oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var oldOptions = EditorSettings.enterPlayModeOptions;
        var disabled = new HashSet<InputDevice>();
        var completed = new List<object>();
        var errors = new List<string>();
        Mouse mouse = null;
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            mouse = InputSystem.AddDevice<Mouse>();
            var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
            foreach (var pair in layouts.Properties().Where(p => ((JArray)p.Value["controls"]).Any(c => (string)c["type"] == "Dropdown")))
            {
                string script = (string)pair.Value["script"];
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<Pointer>().Where(d => d != mouse && d.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(400);
                // 这一个示例故意延迟两秒加载，成功提示从一行变成三行后再定位控件。
                if (pair.Name == "ShowCubeEntity") await Task.Delay(2200);
                foreach (var dropdown in UnityEngine.Object.FindObjectsByType<Dropdown>(FindObjectsSortMode.None))
                {
                    var deadline = DateTime.UtcNow.AddSeconds(8);
                    while (!dropdown.interactable && DateTime.UtcNow < deadline) await Task.Delay(50);
                    Require(dropdown.interactable, pair.Name + ": dropdown ready");
                    Require(dropdown.itemText.rectTransform.rect.height >= 30, pair.Name + ": item height");
                    await Reveal(dropdown);
                    await Click(dropdown.transform as RectTransform, mouse);
                    var list = dropdown.transform.Find("Dropdown List");
                    if (list == null)
                    {
                        Directory.CreateDirectory("Artifacts~/SciFiStyle");
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/SciFiStyle/failed-dropdown.png"));
                        await Task.Delay(150);
                        throw new Exception(pair.Name + ": native mouse opens " + dropdown.name);
                    }
                    Canvas.ForceUpdateCanvases();
                    var labels = list.GetComponentsInChildren<Toggle>().Select(t => t.GetComponentInChildren<Text>()).ToArray();
                    Require(labels.Length == dropdown.options.Count, pair.Name + ": option count");
                    foreach (var label in labels.Where(t => !t.canvasRenderer.cull))
                    {
                        Require(!string.IsNullOrWhiteSpace(label.text), pair.Name + ": option text");
                        Require(label.color.a > .95f && label.color.grayscale > .8f, pair.Name + ": text contrast");
                        Require(label.cachedTextGenerator.vertexCount > 0, pair.Name + ": visible text mesh " + label.text);
                    }
                    Require(labels.Any(t => !t.canvasRenderer.cull), pair.Name + ": visible options");
                    var chosen = labels.FirstOrDefault(t => !t.canvasRenderer.cull && t.text != dropdown.captionText.text) ?? labels[0];
                    string expected = chosen.text;
                    if (pair.Name == "InspectProcedures" || pair.Name == "AttachEntities")
                    {
                        Directory.CreateDirectory("Artifacts~/SciFiStyle");
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/SciFiStyle/" + pair.Name + "-dropdown.png"));
                        await Task.Delay(150);
                    }
                    await Click(chosen.GetComponentInParent<Toggle>().transform as RectTransform, mouse);
                    Require(dropdown.captionText.text == expected, pair.Name + ": native selection updates caption");
                    Require(dropdown.transform.Find("Dropdown List") == null, pair.Name + ": selection closes list");
                    completed.Add(new { example = pair.Name, control = dropdown.name, options = labels.Length, selected = expected });
                }
                await Play(false);
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var pointer in disabled.Where(d => d.added && !d.enabled)) InputSystem.EnableDevice(pointer);
            settings.backgroundBehavior = oldBackground;
            settings.editorInputBehaviorInPlayMode = oldEditorInput;
            EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
            EditorSettings.enterPlayModeOptions = oldOptions;
            Application.logMessageReceived -= capture;
            if (setup.Any(s => s.isActive && s.isLoaded && !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(errors.Count == 0, string.Join("\n", errors));
        var result = new { passed = true, unity = Application.unityVersion, nativeMouseSelection = true, completed, errors };
        Directory.CreateDirectory("Artifacts~/SciFiStyle");
        File.WriteAllText("Artifacts~/SciFiStyle/dropdowns-check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static async Task Reveal(Dropdown dropdown)
    {
        Canvas.ForceUpdateCanvases();
        await Task.Delay(100);
        var scroll = dropdown.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;
        scroll.StopMovement();
        var rect = (RectTransform)dropdown.transform;
        var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
        if (overflow > 0) scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y) / overflow);
        Canvas.ForceUpdateCanvases();
        await Task.Delay(100);
    }

    private static async Task Click(RectTransform rect, Mouse mouse)
    {
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        await Task.Delay(70);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
        await Task.Delay(70);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        await Task.Delay(220);
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = s =>
        {
            if (s == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true);
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

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
