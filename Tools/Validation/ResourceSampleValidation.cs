using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarForce;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 验证资源收口后 UI、实体、声音、场景及 StarForce 的调用链。
/// </summary>
public static class ResourceSampleValidation
{
    /// <summary>
    /// 在隔离工程运行模块操作与 StarForce 菜单、战斗、返回流程。
    /// </summary>
    public static async void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use an isolated validation project.");
        }
        Directory.CreateDirectory("Artifacts~");
        int exitCode = 1;
        object modules = null;
        object pointGame = null;
        string failure = null;
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors.Add(message + "\n" + stack);
            }
        };
        try
        {
            await Task.Delay(1000);
            Application.logMessageReceived += capture;
            modules = await ValidateModuleExamples.Run("Assets/Examples", new[]
            {
                "LoadCubePrefab", "LoadResourcePackage", "ReadBinaryResource", "LoadAddressRules",
                "ShowCubeEntity", "OpenWindowExample", "OneShotSoundExample", "LoadRoomExample"
            });
            if (!(bool)modules.GetType().GetProperty("passed").GetValue(modules))
            {
                throw new InvalidOperationException("A resource-dependent module failed its interaction checks.");
            }
            pointGame = await ValidatePointGame.Run();
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
            EditorApplication.isPlaying = true;
            await Until(() => EditorApplication.isPlaying && FindMenu() != null, "StarForce menu");
            FindMenu().OnSettingButtonClick();
            await Until(() => UnityEngine.Object.FindFirstObjectByType<SettingForm>() != null, "StarForce settings");
            UnityEngine.Object.FindFirstObjectByType<SettingForm>().Close(true);
            FindMenu().OnStartButtonClick();
            await Until(() => GameEntry.Procedure.CurrentProcedure is ProcedureMain
                && UnityEngine.Object.FindFirstObjectByType<MyAircraft>() != null, "StarForce gameplay entities");
            await Task.Delay(1000);
            // StarForce 在玩家飞机销毁后结束本局，再由切场景流程返回菜单。
            UnityEngine.Object.FindFirstObjectByType<MyAircraft>().ApplyDamage(null, 1000000);
            await Until(() => GameEntry.Procedure.CurrentProcedure is ProcedureMenu && FindMenu() != null, "StarForce return to menu");
            if (errors.Count != 0)
            {
                throw new InvalidOperationException("StarForce emitted runtime errors.");
            }
            exitCode = 0;
        }
        catch (Exception exception)
        {
            failure = exception.ToString();
        }
        finally
        {
            EditorApplication.isPlaying = false;
            await Until(() => !EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode");
            await Task.Delay(250);
            Application.logMessageReceived -= capture;
            if (errors.Count != 0)
            {
                exitCode = 1;
            }
            File.WriteAllText("Artifacts~/resource-samples.json", JsonConvert.SerializeObject(new
            {
                passed = exitCode == 0,
                unity = Application.unityVersion,
                modules,
                pointGame,
                starForce = exitCode == 0,
                errors,
                error = failure
            }, Formatting.Indented));
            EditorApplication.Exit(exitCode);
        }
    }

    /// <summary>
    /// 获取当前可操作的菜单。
    /// </summary>
    /// <returns>已打开的菜单；尚未打开时返回空。</returns>
    private static MenuForm FindMenu()
    {
        MenuForm menu = UnityEngine.Object.FindFirstObjectByType<MenuForm>();
        return menu != null && menu.Available ? menu : null;
    }

    /// <summary>
    /// 等待异步场景操作达到预期状态。
    /// </summary>
    /// <param name="condition">完成条件。</param>
    /// <param name="operation">超时时报告的操作名称。</param>
    /// <returns>等待条件成立的任务。</returns>
    private static async Task Until(Func<bool> condition, string operation)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(45);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(operation);
            }
            await Task.Delay(50);
        }
    }
}
