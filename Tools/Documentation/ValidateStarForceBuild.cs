using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using StarForce.Editor;

public static class ValidateStarForceBuild
{
    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use an isolated validation project.");
        var watched = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .Select(name => "ProjectSettings/" + name + ".asset")
            .Concat(new[] { AssetDatabase.GUIDToAssetPath(BuildStarForce.SceneGuid) }).ToDictionary(path => path, Hash);
        var streaming = Tree("Assets/StreamingAssets");
        var memory = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.ToDictionary(name => name,
            name => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(name)));
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
        };
        string folder = Path.GetFullPath("Artifacts~/StarForcePlayer");
        Directory.CreateDirectory(folder + "/Before");
        foreach (string path in watched.Keys) File.Copy(path, folder + "/Before/" + Path.GetFileName(path), true);
        string player;
        Application.logMessageReceived += capture;
        try { player = BuildStarForce.BuildPlayer(folder + "/StarForce.exe"); }
        finally { Application.logMessageReceived -= capture; }
        var changed = watched.Where(pair => Hash(pair.Key) != pair.Value).Select(pair => pair.Key).ToArray();
        if (changed.Length != 0 || !Same(streaming, Tree("Assets/StreamingAssets")))
            throw new Exception("Build changed host files: " + string.Join(", ", changed));
        if (memory.Any(pair => EditorJsonUtility.ToJson(Unsupported.GetSerializedAssetInterfaceSingleton(pair.Key)) != pair.Value))
            throw new Exception("Build changed in-memory settings.");
        if (errors.Count != 0) throw new Exception("Build errors: " + string.Join("\n", errors));
        string resources = folder + "/StarForce_Data/StreamingAssets/" + StarForce.GameEntry.ResourceFolder;
        if (!File.Exists(resources + "/GameFrameworkVersion.dat")) throw new Exception("Missing Package resource list.");
        return new { passed = true, player, unity = Application.unityVersion, settingsUnchanged = true,
            settingsMemoryUnchanged = true, sceneUnchanged = true, projectStreamingAssetsUnchanged = true,
            packageFiles = Directory.GetFiles(resources, "*", SearchOption.AllDirectories).Length, editorErrors = 0 };
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
