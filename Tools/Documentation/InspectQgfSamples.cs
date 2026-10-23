using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// 只刷新资产并读取状态，不切换或保存用户当前场景。
public static class InspectQgfSamples
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before refreshing assets.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return Read();
    }

    public static object Read()
    {
        var package = PackageInfo.GetAllRegisteredPackages().Single(p => p.name == "com.zheliku.unityqgameframework");
        var manifest = JObject.Parse(File.ReadAllText(package.resolvedPath + "/package.json"));
        var registrations = Sample.FindByPackage(package.name, package.version).Select(s => new { s.displayName, s.isImported }).ToArray();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var result = new { project = Application.dataPath, package = package.resolvedPath,
            samples = registrations, registeredPaths = manifest["samples"].Select(s => (string)s["path"]).ToArray(),
            developmentExamples = Directory.Exists("Assets/Examples"),
            baseAssembly = AssetDatabase.GUIDToAssetPath("451fb7eed1a048a39d433c5f2e2a84f9"),
            currentScene = scene.path, currentSceneIsDirty = scene.isDirty, isCompiling = EditorApplication.isCompiling,
            isPaused = EditorApplication.isPaused, isPlaying = EditorApplication.isPlaying };
        Directory.CreateDirectory("Artifacts~/SampleLayout");
        File.WriteAllText("Artifacts~/SampleLayout/editor-samples.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
