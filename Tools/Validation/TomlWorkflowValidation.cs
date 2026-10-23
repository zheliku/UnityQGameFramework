using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Tomlyn;
using Tomlyn.Model;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Editor.ResourceTools;

[InitializeOnLoad]
public static class TomlWorkflowValidation
{
    private static double s_Start = EditorApplication.timeSinceStartup;
    private const string State = "Results/toml-stage.txt";
    static TomlWorkflowValidation() => EditorApplication.update += Tick;

    public static void Run()
    {
        Directory.CreateDirectory("Results");
        File.WriteAllText(State, "import");
    }

    private static void Tick()
    {
        if (!File.Exists(State) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string stage = File.ReadAllText(State);
        if (stage == "done") return;
        try
        {
            Require(EditorApplication.timeSinceStartup - s_Start < 240, "Sample import timed out");
            if (stage == "import")
            {
                File.WriteAllText(State, "verify");
                string packageVersion = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == "com.zheliku.unityqgameframework").version;
                Require(Sample.FindByPackage("com.zheliku.unityqgameframework", packageVersion).Single(s => s.displayName == "22 · StarForce").Import(Sample.ImportOptions.OverridePreviousImports), "Could not import sample");
                return;
            }
            if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.GetType("StarForce.MenuForm") == null)) return;
            Validate();
            File.WriteAllText(State, "done");
            File.WriteAllText("Results/toml-result.json", "{\"passed\":true,\"translations\":120,\"scenePaths\":3,\"resourceRoundTrip\":true,\"invalidInputPreserved\":true,\"assetBundleBuild\":true}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText(State, "done");
            File.WriteAllText("Results/toml-result.json", JsonUtility.ToJson(new Failure { passed = false, failure = exception.ToString() }, true));
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Validate()
    {
        string launcher = AssetDatabase.GUIDToAssetPath("9d2d51bf0fc3dfc42908e3a7a64f4feb");
        string sample = Path.GetDirectoryName(launcher).Replace('\\', '/');
        string configs = sample + "/GameMain/Configs";
        string temporary = Path.GetFullPath("Results/ConfigCopies");
        Directory.CreateDirectory(temporary);
        Require(!Directory.GetFiles(sample, "*.xml", SearchOption.AllDirectories).Any(p => Path.GetFileName(p) != "link.xml"), "XML config remains");

        // Every original resource, asset GUID, group and variant must round-trip.
        string path = temporary + "/ResourceCollection.toml";
        File.Copy(configs + "/ResourceCollection.toml", path, true);
        var collection = new ResourceCollection(path);
        Require(collection.Load(), "Collection load failed");
        string before = Snapshot(collection);
        Require(collection.Save() && collection.Load() && Snapshot(collection) == before, "Collection round-trip changed data");
        File.WriteAllText(path, "[[Resources]]\nName = 42\nAssets = []\n");
        string invalid = File.ReadAllText(path);
        Require(!collection.Load() && File.ReadAllText(path) == invalid && Snapshot(collection) == before, "Invalid collection deleted the file or changed live data");
        File.WriteAllText(path, "Resources = []\nAssets = []\n");
        Require(collection.Load() && collection.Save() && collection.Load() && collection.ResourceCount == 0, "Empty collection round-trip failed");

        string builderPath = temporary + "/ResourceBuilder.toml";
        File.Copy(configs + "/ResourceBuilder.toml", builderPath, true);
        var builder = new ResourceBuilderController(builderPath);
        Require(builder.Load() && builder.InternalResourceVersion == 1 && builder.Platforms == Platform.Windows && builder.AssetBundleCompression == AssetBundleCompressionType.LZ4, "Builder settings changed during migration");
        builder.Platforms = Platform.Windows64 | Platform.Android;
        builder.OutputDirectory = Path.GetFullPath("Results/资源输出");
        Require(builder.Save(), "Builder save failed");
        var roundTrip = new ResourceBuilderController(builderPath);
        Require(roundTrip.Load() && roundTrip.InternalResourceVersion == 2 && roundTrip.Platforms == builder.Platforms && roundTrip.OutputDirectory == builder.OutputDirectory, "Builder round-trip failed");
        var pack = new ResourcePackBuilderController(builderPath);
        Require(pack.Load() && pack.WorkingDirectory == builder.OutputDirectory && pack.CompressionHelperTypeName == builder.CompressionHelperTypeName, "Pack builder did not read the shared TOML");
        File.WriteAllText(builderPath, "[ResourceBuilder]\nPlatforms = 'NotAPlatform'\n");
        Require(!roundTrip.Load() && File.Exists(builderPath) && roundTrip.InternalResourceVersion == 2, "Invalid builder changed settings or removed input");

        string editorPath = temporary + "/ResourceEditor.toml";
        var editorModel = Toml.ToModel(File.ReadAllText(configs + "/ResourceEditor.toml"));
        ((TomlTable)editorModel["ResourceEditor"])["SourceAssetRootPath"] = sample + "/GameMain";
        File.WriteAllText(editorPath, Toml.FromModel(editorModel));
        File.Copy(configs + "/ResourceCollection.toml", path, true);
        var editor = new ResourceEditorController(editorPath, path);
        Require(editor.Load() && editor.SourceAssetRootPath == sample + "/GameMain" && editor.Save(), "Resource editor TOML load/save failed");
        var editorRoundTrip = new ResourceEditorController(editorPath, path);
        var savedCollection = new ResourceCollection(path);
        Require(editorRoundTrip.Load() && editorRoundTrip.SourceAssetRootPath == editor.SourceAssetRootPath &&
            editorRoundTrip.SourceAssetUnionTypeFilter == editor.SourceAssetUnionTypeFilter &&
            editorRoundTrip.AssetSorter == editor.AssetSorter && savedCollection.Load() && Snapshot(savedCollection) == before, "Resource editor reload changed settings or collection");
        var sceneType = typeof(ResourceCollection).Assembly.GetType("UnityQGameFramework.Editor.BuildSettings");
        var scenePaths = (string[])sceneType.GetMethod("ReadScenePaths", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { true });
        Require(scenePaths.Length == 3 && scenePaths[0] == launcher, "Scene paths no longer resolve relative to sample");

        var manager = FrameworkEntry.GetModule<ILocalizationManager>();
        int translations = 0;
        foreach (string dictionary in Directory.GetFiles(sample + "/GameMain", "*.toml", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) == "DefaultDictionary.toml" || Path.GetFileName(p) == "Default.toml"))
        {
            string text = File.ReadAllText(dictionary);
            var document = Toml.ToModel(text);
            foreach (var language in document)
            {
                manager.Language = (Language)Enum.Parse(typeof(Language), language.Key);
                manager.RemoveAllRawStrings();
                TomlLocalizationParser.ParseAndAdd(manager, text);
                var expected = Flatten((TomlTable)language.Value);
                foreach (var entry in expected) Require(manager.GetRawString(entry.Key) == entry.Value, "Lost translation: " + entry.Key);
                translations += expected.Count;
            }
        }
        Require(translations == 120, "Translation count changed");
        manager.RemoveAllRawStrings();
        manager.Language = Language.Korean;
        TomlLocalizationParser.ParseAndAdd(manager, "[Korean.Game]\nName = \"게임 프레임워크\"\n");
        Require(manager.GetString("Game.Name") == "게임 프레임워크", "Nested table did not produce dotted query key");
        foreach (string invalidDictionary in new[] { "[Korean]\nWrong = 42", "[English]\nName = 'Missing language'", "[Korean]\n'Game.Name' = 'duplicate'\n[Korean.Game]\nName = 'duplicate too'" })
        {
            bool rejected = false;
            try { TomlLocalizationParser.ParseAndAdd(manager, invalidDictionary); }
            catch { rejected = true; }
            Require(rejected && manager.DictionaryCount == 1 && manager.GetString("Game.Name") == "게임 프레임워크", "Invalid dictionary changed published translations");
        }
        FrameworkEntry.Shutdown();

        // A full Windows64 resource build covers the new collection reader and report writer.
        builder = new ResourceBuilderController(configs + "/ResourceBuilder.toml");
        Require(builder.Load(), "Resource builder could not load sample configuration");
        builder.Platforms = Platform.Windows64;
        builder.OutputDirectory = Path.GetFullPath("Results/Bundles");
        Directory.CreateDirectory(builder.OutputDirectory);
        builder.BuildEventHandlerTypeName = string.Empty;
        Require(builder.RefreshCompressionHelper() && builder.RefreshBuildEventHandler(), "Builder helpers could not initialize");
        Require(builder.BuildResources(), "Resource build failed; see BuildLog.txt");
        var report = Toml.ToModel(File.ReadAllText(builder.BuildReportPath + "BuildReport.toml"));
        Require(((TomlTable)report["Summary"])["Platforms"].ToString() == "Windows64", "Build report lost platform");
        Require(((TomlTableArray)report["Resources"]).Count > 0, "Build report lost resources");
        Require(Directory.GetFiles(builder.OutputFullPath, "*.dat", SearchOption.AllDirectories).Length > 0 &&
            !File.ReadAllText(builder.BuildReportPath + "BuildLog.txt").Contains("[ERROR]"), "Resource build produced no files or logged errors");
        Require(!File.Exists(builder.BuildReportPath + "BuildReport.xml"), "Legacy XML build report remains");
        FrameworkEntry.Shutdown();
    }

    private static System.Collections.Generic.Dictionary<string, string> Flatten(TomlTable table, string prefix = "")
    {
        var result = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var entry in table)
        {
            string key = string.IsNullOrEmpty(prefix) ? entry.Key : prefix + "." + entry.Key;
            if (entry.Value is TomlTable child) foreach (var leaf in Flatten(child, key)) result.Add(leaf.Key, leaf.Value);
            else result.Add(key, (string)entry.Value);
        }
        return result;
    }

    private static string Snapshot(ResourceCollection collection) => string.Join("\n", collection.GetResources().Select(r => r.FullName + "|" + r.FileSystem + "|" + r.LoadType + "|" + r.Packed + "|" + string.Join(",", r.GetResourceGroups().OrderBy(g => g)))) + "\n" + string.Join("\n", collection.GetAssets().Select(a => a.Guid + "|" + a.Resource.FullName));
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    [Serializable] private sealed class Failure { public bool passed; public string failure; }
}
