using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Runtime;

/// <summary>
/// 检查当前三个示例的兼容性。
/// </summary>
public static class CurrentSampleCompatibility
{
    /// <summary>
    /// 检查场景引用、数据表和游戏启动，然后退出隔离工程。
    /// </summary>
    public static async void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use an isolated compatibility project.");
        }

        Directory.CreateDirectory("Artifacts~/Compatibility");
        File.Delete("Artifacts~/Compatibility/failure.json");
        try
        {
            // executeMethod 早于 Editor 启动的 delayCall；先让编辑器完成索引等初始化，再运行场景检查。
            await Task.Delay(1000);
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ResourceComponent).Assembly);
            var samples = UnityEditor.PackageManager.UI.Sample.FindByPackage(package.name, package.version).ToArray();
            if (samples.Length != 3 || (File.Exists("QgfGitInstallValidation~.json")
                && package.source != UnityEditor.PackageManager.PackageSource.Git))
            {
                throw new InvalidOperationException("Unexpected QGF installation or sample registrations.");
            }
            File.WriteAllText("Artifacts~/Compatibility/installation.json", JsonConvert.SerializeObject(new
            {
                package.name,
                package.version,
                package.packageId,
                source = package.source.ToString(),
                samples = samples.Select(sample => sample.displayName).ToArray()
            }, Formatting.Indented));

            ExampleCompatibilityChecks.Run();
            ValidateSampleStructure.Run();
            object modules = await ValidateModuleExamples.Run("Assets/Examples", new[]
            {
                "LoadLubanTablesExample", "QueryRowsExample", "LoadCatalogExample", "FilterRowsExample", "TableRegistryExample", "TableModesExample",
                "SpawnOrCreateCube", "LoadCubePrefab", "LoadResourcePackage", "ReadBinaryResource", "LoadAddressRules",
                "ShowCubeEntity", "ReuseCubeEntity", "OpenWindowExample", "SoundGroupVolumeExample", "SpatialSoundExample",
                "LoadRoomExample", "GetWebRequestExample", "DownloadFileExample", "ConnectNetworkExample"
            });
            string report = JsonConvert.SerializeObject(modules, Formatting.Indented);
            File.WriteAllText("Artifacts~/Compatibility/module-behavior.json", report);
            if (!(bool)modules.GetType().GetProperty("passed").GetValue(modules))
            {
                throw new InvalidOperationException("Module behavior checks failed. See module-behavior.json.");
            }

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
            await SetPlayMode(true);
            DateTime deadline = DateTime.UtcNow.AddSeconds(45);
            while (!EditorApplication.isPlaying || UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() == null)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException("StarForce menu did not open.");
                }
                await Task.Delay(50);
            }
            if (StarForce.GameEntry.DataTable.GetDataTable<StarForce.TbScene>().GetDataRow(1) == null
                || StarForce.GameEntry.Config.GetInt("Scene.Menu") != 1)
            {
                throw new InvalidOperationException("StarForce Luban or TOML data is unavailable.");
            }
            File.WriteAllText("Artifacts~/Compatibility/starforce-startup.json", JsonConvert.SerializeObject(new
            {
                passed = true,
                unity = Application.unityVersion,
                menu = true,
                luban = true,
                toml = true
            }, Formatting.Indented));
            await SetPlayMode(false);

            // 最后检查点点点及 UniTask；该检查保存报告并退出 Editor。
            CompatibilitySmoke.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText("Artifacts~/Compatibility/failure.json", JsonConvert.SerializeObject(new
            {
                passed = false,
                unity = Application.unityVersion,
                error = exception.ToString()
            }, Formatting.Indented));
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 等待编辑器完成运行模式切换。
    /// </summary>
    /// <param name="playing">是否进入运行模式。</param>
    /// <returns>切换任务。</returns>
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
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
            {
                throw new TimeoutException("Play Mode transition did not complete.");
            }
            await Task.Delay(120);
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }
}
