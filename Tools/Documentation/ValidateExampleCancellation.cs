using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>
/// 检查示例销毁后是否取消跨帧等待。
/// </summary>
public static class ValidateExampleCancellation
{
    /// <summary>
    /// 在实体和窗口重新打开期间销毁示例。
    /// </summary>
    /// <returns>取消检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new Exception("Unsaved scene");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool enabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        var errors = new List<string>();
        var results = new List<string>();
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
            var cases = new[]
            {
                new[] { "Entity/02_Reuse/ReuseCubeEntity", "HideAndShow", "recycling" },
                new[] { "UI/02_Reopen/ReopenWindowExample", "CloseAndReopen", "changing" }
            };
            foreach (var entry in cases)
            {
                var parts = entry[0].Split('/');
                var module = JArray.Parse(System.IO.File.ReadAllText("Tools/Documentation/examples-layout.json"))
                    .Single(item => (string)item["module"] == parts[0]);
                EditorSceneManager.OpenScene("Assets/Examples/" + (string)module["folder"] + "/" + string.Join("/", parts.Skip(1)) + ".unity");
                await Play(true);
                await Task.Delay(1300);
                string typeName = entry[0].Split('/').Last();
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + typeName))
                    .First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                if (example == null)
                    throw new Exception("Missing example " + typeName);
                var token = example.destroyCancellationToken;
                type.GetMethod(entry[1], BindingFlags.Instance | BindingFlags.NonPublic).Invoke(example, null);
                if (!(bool)type.GetField(entry[2], BindingFlags.Instance | BindingFlags.NonPublic).GetValue(example))
                    throw new Exception("Expected an in-flight operation: " + typeName);
                UnityEngine.Object.DestroyImmediate(example.gameObject);
                if (!token.IsCancellationRequested)
                    throw new Exception("Destroy did not cancel " + typeName);
                await Task.Delay(300);
                if ((bool)type.GetField(entry[2], BindingFlags.Instance | BindingFlags.NonPublic).GetValue(example))
                    throw new Exception("Operation did not finish after destruction: " + typeName);
                if (Time.timeScale != 1)
                    throw new Exception("Time scale was not restored: " + typeName);
                await Play(false);
                if (errors.Count != 0)
                    throw new Exception(string.Join("\n", errors));
                results.Add(typeName + ": destroyed during await without late Unity access");
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = enabled;
            EditorSettings.enterPlayModeOptions = options;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        var result = new
        {
            passed = true,
            cases = results
        };
        System.IO.Directory.CreateDirectory("Artifacts~/ExampleValidation");
        System.IO.File.WriteAllText("Artifacts~/ExampleValidation/cancellation-check.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    private static async Task Play(bool playing)
    {
        var finished = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (playing ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
                finished.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            if (await Task.WhenAny(finished.Task, Task.Delay(15000)) != finished.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(120);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
