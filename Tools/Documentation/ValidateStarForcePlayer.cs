using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StarForce.Editor;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;

public static class ValidateStarForcePlayer
{
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use an isolated validation project.");
        string resourceSettingsPath = Path.GetDirectoryName(BuildStarForce.GetScenePath()).Replace('\\', '/')
            + "/UnityQGameFramework/Resource/ResourceSettings.asset";
        byte[] resourceSettingsBytes = File.ReadAllBytes(resourceSettingsPath);
        var watched = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .Select(name => "ProjectSettings/" + name + ".asset")
            .Concat(new[] { BuildStarForce.GetScenePath() }).ToDictionary(path => path, Hash);
        var streaming = Tree("Assets/StreamingAssets");
        var memory = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.ToDictionary(name => name,
            name => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(name)));
        var editorErrors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) editorErrors.Add(message + "\n" + stack);
        };
        string folder = Path.GetFullPath("Artifacts~/StarForcePlayer");
        Directory.CreateDirectory(folder + "/Before");
        foreach (string path in watched.Keys) File.Copy(path, folder + "/Before/" + Path.GetFileName(path), true);
        string player = folder + "/StarForce.exe";
        string savePath = Path.Combine(Application.persistentDataPath, "GameFrameworkSetting.dat");
        byte[] save = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
        var runs = new List<JObject>();
        // Unity 的命令行解析会丢弃空参数，首轮也传一个明确的占位值。
        string language = "Unspecified";
        try
        {
            // Player 使用同一项 Play Mode；这里显式选择离线模式，不由框架按环境替换。
            var resourceSettings = new SerializedObject(AssetDatabase.LoadAssetAtPath<ResourceSettings>(resourceSettingsPath));
            resourceSettings.FindProperty("m_Packages").GetArrayElementAtIndex(0).FindPropertyRelative("m_Mode").intValue = (int)EPlayMode.OfflinePlayMode;
            resourceSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Application.logMessageReceived += capture;
            try { BuildStarForce.BuildPlayer(player); }
            finally { Application.logMessageReceived -= capture; }
            if (editorErrors.Count != 0) throw new Exception("Build errors: " + string.Join("\n", editorErrors));
            foreach (string run in new[] { "first", "second" })
            {
                string report = folder + "/" + run + ".json";
                if (File.Exists(report)) File.Delete(report);
                using (var process = Process.Start(new ProcessStartInfo(player)
                {
                    Arguments = "-batchmode -nographics -logFile \"" + folder + "/" + run + ".log\" -qgf-starforce-run " + run
                        + " -qgf-starforce-language \"" + language + "\" -qgf-starforce-result \"" + report + "\"",
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (process == null) throw new Exception("Player did not start.");
                    if (!await Task.Run(() => process.WaitForExit(120000)))
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                        throw new TimeoutException("StarForce Player did not exit: " + run);
                    }
                    if (!File.Exists(report)) throw new Exception("Player did not write a report. See " + run + ".log");
                    var result = JObject.Parse(File.ReadAllText(report));
                    runs.Add(result);
                    if (process.ExitCode != 0 || (bool?)result["passed"] != true) throw new Exception("StarForce Player failed: " + result);
                    string log = File.ReadAllText(folder + "/" + run + ".log");
                    if (log.Contains("NullReferenceException") || log.Contains("OnClose with exception"))
                        throw new Exception("Player shutdown error. See " + run + ".log");
                    language = (string)result["language"];
                }
            }
        }
        finally
        {
            File.WriteAllBytes(resourceSettingsPath, resourceSettingsBytes);
            AssetDatabase.ImportAsset(resourceSettingsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (save != null) File.WriteAllBytes(savePath, save);
            else if (File.Exists(savePath)) File.Delete(savePath);
        }
        var changed = watched.Where(pair => Hash(pair.Key) != pair.Value).Select(pair => pair.Key).ToArray();
        if (changed.Length != 0 || !Same(streaming, Tree("Assets/StreamingAssets")))
            throw new Exception("Build changed host files: " + string.Join(", ", changed));
        if (memory.Any(pair => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(pair.Key)) != pair.Value))
            throw new Exception("Build changed in-memory settings.");
        var summary = new { passed = true, unity = Application.unityVersion, player, runs,
            settingsUnchanged = true, settingsMemoryUnchanged = true, sceneUnchanged = true, projectStreamingAssetsUnchanged = true,
            editorErrors = 0,
            input = "Form methods; per-process inputMode identifies virtual mouse or target injection. Native mouse verified separately in Unity 6.3 Editor",
            damageInjection = true };
        File.WriteAllText(folder + "/audit.json", Newtonsoft.Json.JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented));
        return summary;
    }

    private static Dictionary<string, string> Tree(string folder) => Directory.Exists(folder)
        ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToDictionary(path => path, Hash)
        : new Dictionary<string, string>();
    private static bool Same(Dictionary<string, string> a, Dictionary<string, string> b) => a.Count == b.Count
        && a.All(pair => b.TryGetValue(pair.Key, out string value) && value == pair.Value);
    private static string Hash(string path)
    {
        using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));
    }
}
