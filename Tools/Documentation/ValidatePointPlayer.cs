using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Examples.PointPointPoint;
using UnityQGameFramework.Examples.PointPointPoint.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;

// 验收真实 Player，构建后分别启动两个进程；不修改项目资源与设置。
public static class ValidatePointPlayer
{
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use an isolated validation project.");
        string output = Path.GetFullPath("Artifacts~/PointPlayer");
        Directory.CreateDirectory(output);
        string player = Path.Combine(output, "PointPointPoint.exe");
        string scene = BuildPointGame.GetScenePath();
        string resourceSettingsPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scene))).Replace('\\', '/')
            + "/UnityQGameFramework/Resource/ResourceSettings.asset";
        byte[] resourceSettingsBytes = File.ReadAllBytes(resourceSettingsPath);
        var watched = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .Select(name => "ProjectSettings/" + name + ".asset").Concat(new[] { scene }).ToDictionary(path => path, Hash);
        var streaming = Tree("Assets/StreamingAssets");
        var memory = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.ToDictionary(name => name,
            name => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(name)));
        var editorErrors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                editorErrors.Add(message);
        };
        string baseline = Path.Combine(output, "Before");
        Directory.CreateDirectory(baseline);
        foreach (string path in watched.Keys)
            File.Copy(path, Path.Combine(baseline, Path.GetFileName(path)), true);
        byte[] save = File.Exists(PointGameSettingHelper.SavePath) ? File.ReadAllBytes(PointGameSettingHelper.SavePath) : null;
        var runs = new List<JObject>();
        Application.logMessageReceived += capture;
        try
        {
            // 和开发者在 Inspector 中的操作相同：构建前明确选择离线模式，验收后还原源资产。
            var resourceSettings = new SerializedObject(AssetDatabase.LoadAssetAtPath<ResourceSettings>(resourceSettingsPath));
            resourceSettings.FindProperty("m_Packages").GetArrayElementAtIndex(0).FindPropertyRelative("m_Mode").intValue = (int)EPlayMode.OfflinePlayMode;
            resourceSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            BuildPointGame.BuildPlayer(player);
            foreach (string run in new[] { "first", "second" })
            {
                string report = Path.Combine(output, run + ".json");
                if (File.Exists(report))
                    File.Delete(report);
                using (var process = Process.Start(new ProcessStartInfo(player)
                {
                    Arguments = "-batchmode -nographics -logFile \"" + Path.Combine(output, run + ".log")
                        + "\" -qgf-point-run " + run + " -qgf-point-result \"" + report + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (process == null)
                        throw new Exception("Player did not start.");
                    var wait = Task.Run(() => process.WaitForExit(60000));
                    if (!await wait)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                        throw new TimeoutException("Point Player did not exit: " + run);
                    }
                    if (!File.Exists(report))
                        throw new Exception("Player did not write a report. See " + run + ".log");
                    var result = JObject.Parse(File.ReadAllText(report));
                    runs.Add(result);
                    if (process.ExitCode != 0 || (bool?)result["passed"] != true)
                        throw new Exception("Point Player failed: " + result);
                }
            }
        }
        finally
        {
            Application.logMessageReceived -= capture;
            File.WriteAllBytes(resourceSettingsPath, resourceSettingsBytes);
            AssetDatabase.ImportAsset(resourceSettingsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            // 测试会覆盖示例纪录；完成后还原验收前的文件。
            if (save != null)
                File.WriteAllBytes(PointGameSettingHelper.SavePath, save);
            else if (File.Exists(PointGameSettingHelper.SavePath))
                File.Delete(PointGameSettingHelper.SavePath);
        }
        var changed = watched.Where(pair => Hash(pair.Key) != pair.Value).Select(pair => pair.Key).ToArray();
        if (changed.Length != 0 || !Same(streaming, Tree("Assets/StreamingAssets")))
            throw new Exception("Player workflow changed host files: " + string.Join(", ", changed)
                + "; StreamingAssets unchanged: " + Same(streaming, Tree("Assets/StreamingAssets")));
        if (memory.Any(pair => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(pair.Key)) != pair.Value))
            throw new Exception("Player build changed in-memory project settings.");
        if (editorErrors.Count != 0)
            throw new Exception("Editor build errors: " + string.Join("\n", editorErrors));
        var summary = new
        {
            passed = true,
            unity = Application.unityVersion,
            player,
            runs,
            settingsUnchanged = true,
            sceneUnchanged = true,
            projectStreamingAssetsUnchanged = true,
            settingsMemoryUnchanged = true,
            editorErrors = 0,
            input = "Player Button.onClick; native mouse interaction checked separately in Editor"
        };
        File.WriteAllText(Path.Combine(output, "audit.json"), JsonConvert.SerializeObject(summary, Formatting.Indented));
        return summary;
    }

    private static Dictionary<string, string> Tree(string folder) => Directory.Exists(folder)
        ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(path => path, Hash)
        : new Dictionary<string, string>();
    private static bool Same(Dictionary<string, string> a, Dictionary<string, string> b) => a.Count == b.Count
        && a.All(pair => b.TryGetValue(pair.Key, out string value) && value == pair.Value);
    private static string Hash(string path)
    {
        using (var hash = SHA256.Create())
            return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));
    }
}
