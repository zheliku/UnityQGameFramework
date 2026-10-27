using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Examples.Modules;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 检查统一数据表入口、资源模式及示例启动警告。
/// </summary>
public static class QgfSampleWorkflowValidation
{
    private static readonly List<string> s_Warnings = new List<string>();
    private static readonly List<string> s_Errors = new List<string>();
    private static string s_BuiltinRoot;

    /// <summary>
    /// 在隔离工程检查示例并退出编辑器。
    /// </summary>
    public static async void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use an isolated validation project.");
        }

        Directory.CreateDirectory("Artifacts~/SampleWorkflow");
        int exitCode = 1;
        string failure = null;
        object tables = null, resources = null, smoke = null, logger = null, editorStartup = null;
        Application.logMessageReceived += OnLog;
        try
        {
            // executeMethod 早于编辑器的延迟初始化，先让 Search 等内置服务完成启动。
            await Task.Delay(2000);
            AuditExampleApi.Run();
            // 空场景不创建框架组件，用于区分编辑器首次进入 Play 的故障与示例运行结果。
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            await SetPlayMode(true);
            await Task.Delay(500);
            Require(UnityEngine.Object.FindFirstObjectByType<BaseComponent>() == null, "Baseline scene contains QGF components.");
            await SetPlayMode(false);
            editorStartup = new { emptyScene = true, errors = s_Errors.ToArray(), warnings = s_Warnings.ToArray() };
            File.WriteAllText("Artifacts~/SampleWorkflow/editor-baseline.json", JsonConvert.SerializeObject(editorStartup, Formatting.Indented));
            Require(s_Errors.All(error => error.Contains("UnityEditor.Search.SearchDatabase")), "The editor baseline has an unrelated failure.");
            // 上述原生 Search 故障保存在独立记录中；后续示例的任何错误都仍然使检查失败。
            s_Errors.Clear();
            s_Warnings.Clear();
            string pipelinePath = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
                .Select(AssetDatabase.GUIDToAssetPath).First();
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            logger = QgfLogValidation.Run();
            Require(typeof(DataTableComponent).GetProperty("LubanTables") == null
                && typeof(DataTableComponent).GetMethod("GetLubanTables") == null, "A second table access API remains.");
            ExampleCompatibilityChecks.Run();
            object structure = ValidateSampleStructure.Run();
            Require((bool)structure.GetType().GetProperty("passed").GetValue(structure), "Sample references failed.");

            ResourceSettings settings = AssetDatabase.FindAssets("t:ResourceSettings")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ResourceSettings>)
                .Single(item => item.DefaultPackageName == "QgfExamples.Modules");
            var collector = ResourceBuildService.GetCollector(settings);
            // 验证需要完整的离线首包；临时采用 YooAsset 原生选项，结束后恢复本机设置。
            string buildPipelineName = BundleBuilderSetting.GetPackageBuildPipeline(ResourceExamplePaths.RemotePackageName);
            EBundledCopyOption copyOption = BundleBuilderSetting.GetPackageBundledCopyOption(ResourceExamplePaths.RemotePackageName, buildPipelineName);
            try
            {
                BundleBuilderSetting.SetPackageBundledCopyOption(ResourceExamplePaths.RemotePackageName, buildPipelineName, EBundledCopyOption.ClearAndCopyAll);
                // 只构建隔离工程中的资源，不写源工程的 StreamingAssets。
                UnityQGameFramework.Examples.Modules.Editor.UpdateRemoteResourcesInspector.BuildResources();
            }
            finally
            {
                BundleBuilderSetting.SetPackageBundledCopyOption(ResourceExamplePaths.RemotePackageName, buildPipelineName, copyOption);
            }
            s_BuiltinRoot = ResourceBuildService.GetBundledOutputDirectory(collector, ResourceExamplePaths.RemotePackageName);

            tables = await ValidateDataTableExamples.Run();
            resources = await ValidateModuleExamples.Run("Assets/Examples", new[]
            {
                "LoadCubePrefab", "LoadResourcePackage", "ReadBinaryResource", "UpdateRemoteResources"
            }, ExerciseResource);

            // 全部模块场景都启动，检查自动加载与销毁；交互断言由上述专项检查负责。
            smoke = await ValidateModuleExamples.Run("Assets/Examples", null,
                async (name, example) => await Task.Delay(500));
            Require((bool)smoke.GetType().GetProperty("passed").GetValue(smoke), "Module startup failed.");

            EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
            await SetPlayMode(true);
            await Until(() => UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() != null, "StarForce menu", 60000);
            Require(StarForce.GameEntry.DataTable.GetDataTable<StarForce.TbScene>().GetDataRow(1) != null, "StarForce table lookup failed.");
            await SetPlayMode(false);
            Require((bool)tables.GetType().GetProperty("passed").GetValue(tables), "DataTable examples failed.");
            Require((bool)resources.GetType().GetProperty("passed").GetValue(resources), "Resource examples failed.");
            Require(s_Warnings.Count == 0 && s_Errors.Count == 0, "Unexpected runtime warnings or errors were captured.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                await SetPlayMode(false);
            }
            Application.logMessageReceived -= OnLog;
            File.WriteAllText("Artifacts~/SampleWorkflow/result.json", JsonConvert.SerializeObject(new
            {
                passed = exitCode == 0, unity = Application.unityVersion, editorStartup, logger, tables, resources, smoke,
                warnings = s_Warnings, errors = s_Errors, failure
            }, Formatting.Indented));
            EditorApplication.Exit(exitCode);
        }
    }

    private static async Task ExerciseResource(string name, MonoBehaviour example)
    {
        if (name == "LoadCubePrefab")
        {
            await Until(() => Field<GameObject>(example, "first") != null && Field<GameObject>(example, "second") != null, "Path load");
            Field<Button>(example, "releaseButton").onClick.Invoke();
            await Until(() => Field<GameObject>(example, "asset") == null, "Path asset returned");
        }
        else if (name == "LoadResourcePackage")
        {
            Field<Button>(example, "initializeButton").onClick.Invoke();
            await Until(() => Field<bool>(example, "initialized"), "Package initialized");
            Field<Button>(example, "loadButton").onClick.Invoke();
            await Until(() => Field<GameObject>(example, "instance") != null, "GUID load");
            Field<Button>(example, "releaseButton").onClick.Invoke();
            await Until(() => Field<AssetHandle>(example, "handle") == null, "Handle released");
        }
        else if (name == "ReadBinaryResource")
        {
            Field<Button>(example, "initializeButton").onClick.Invoke();
            await Until(() => Field<bool>(example, "initialized"), "Raw package initialized");
            Field<Button>(example, "readButton").onClick.Invoke();
            await Until(() => Field<byte[]>(example, "result") != null, "Binary segment loaded");
            byte[] source = File.ReadAllBytes("Assets/Examples/01_Modules/12_Resource/Common/Art/Archive.bytes");
            Require(Field<byte[]>(example, "result").SequenceEqual(source.Skip(8).Take(16)), "Binary segment differs.");
        }
        else if (name == "UpdateRemoteResources")
        {
            // 同一份实际构建产物先验证 Offline，再切回示例自己的 Host 初始化。
            var options = new OfflinePlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(s_BuiltinRoot)
            };
            await QGF.Resource.InitializePackageAsync(ResourceExamplePaths.RemotePackageName, options);
            GameObject asset = await QGF.Resource.LoadAssetAsync<GameObject>(ResourceExamplePaths.RemoteCubeAddress, ResourceExamplePaths.RemotePackageName);
            Require(asset != null, "Offline bundle load failed.");
            QGF.Resource.UnloadAsset(asset, ResourceExamplePaths.RemotePackageName);
            await QGF.Resource.DestroyPackageAsync(ResourceExamplePaths.RemotePackageName);

            Field<Button>(example, "versionButton").onClick.Invoke();
            await Until(() => Field<ResourcePackage>(example, "package")?.PackageValid == true, "Host package initialized");
            Field<Button>(example, "updateButton").onClick.Invoke();
            await Until(() => Field<ResourceDownloaderOperation>(example, "downloader")?.IsDone == true, "Host bundles downloaded");
            Require(Field<ResourceDownloaderOperation>(example, "downloader").Status == EOperationStatus.Succeeded, "Host download failed.");
            Field<Button>(example, "loadButton").onClick.Invoke();
            await Until(() => Field<GameObject>(example, "asset") != null, "Address load");
            var package = Field<ResourcePackage>(example, "package");
            AssetInfo address = package.GetAssetInfo(ResourceExamplePaths.RemoteCubeAddress);
            Require(address.IsValid && address.AssetPath == package.GetAssetInfo(address.AssetPath).AssetPath, "Address and path resolve differently.");
        }
    }

    private static T Field<T>(MonoBehaviour example, string name)
    {
        return (T)example.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(example);
    }

    private static async Task Until(Func<bool> condition, string message, int timeout = 30000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(message);
            }
            await Task.Delay(30);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static async Task SetPlayMode(bool playing)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (playing ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
            {
                completion.TrySetResult(true);
            }
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            if (await Task.WhenAny(completion.Task, Task.Delay(20000)) != completion.Task)
            {
                throw new TimeoutException("Play Mode transition");
            }
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Warning)
        {
            s_Warnings.Add(message + "\n" + stack);
        }
        else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            s_Errors.Add(message + "\n" + stack);
        }
    }
}
