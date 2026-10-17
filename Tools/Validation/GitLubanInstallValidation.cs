using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using Debug = UnityEngine.Debug;

/// <summary>Exercise first-install initialization and the same generation menu users click.</summary>
public static class GitLubanInstallValidation
{
    private static readonly Stopwatch Timer = Stopwatch.StartNew();

    public static void Run() => EditorApplication.update += BeginWhenReady;

    private static void BeginWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (LubanSettings.instance.Profile == null && Timer.Elapsed.TotalSeconds < 30) return;
        EditorApplication.update -= BeginWhenReady;
        Validate();
    }

    private static async void Validate()
    {
        var result = new Result { unityVersion = Application.unityVersion };
        try
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataTableComponent).Assembly);
            Require(package.source == UnityEditor.PackageManager.PackageSource.Git, "Package did not resolve through UPM Git.");
            result.resolvedPath = package.resolvedPath;
            var profile = LubanSettings.instance.Profile;
            Require(profile != null && AssetDatabase.GetAssetPath(profile) == LubanSettings.DefaultWorkspaceRoot + "/Editor/LubanProfile.asset", "Fresh install did not initialize its default global Profile.");
            string source = AssetDatabase.GetAssetPath(profile.SourceFolder);
            foreach (string file in new[] { "__tables__", "__beans__", "__enums__" })
                Require(File.Exists(source + "/Datas/" + file + ".xlsx"), "Excel schema template is missing: " + file);
            Require(!File.Exists(LubanGenerator.DefaultGenerator) && string.IsNullOrEmpty(LubanGenerator.LocalGenerator), "First-generation compiler cache was not clean.");
            result.automaticWorkspace = true;
            Require(EditorApplication.ExecuteMenuItem("Unity Q Game Framework/Generate Data Tables"), "Generate menu could not be invoked.");
            var generation = Stopwatch.StartNew();
            LubanDataTableCatalog catalog = null;
            do
            {
                await Task.Delay(100);
                catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(AssetDatabase.GetAssetPath(profile.DataFolder) + "/Catalog.asset");
            }
            while (catalog == null && LubanGenerator.IsBusy && generation.Elapsed.TotalSeconds < 240);
            Require(catalog != null && catalog.GetAssets().Count == 1 && !LubanGenerator.IsBusy, "First menu generation failed. " + LubanGenerator.LastLog);
            Require(File.Exists(LubanGenerator.DefaultGenerator), "Pinned compiler was not installed.");
            result.downloadedCompiler = true;
            result.generatedFromMenu = true;
            Require(File.ReadAllText(AssetDatabase.GetAssetPath(profile.CodeFolder) + "/Tables.cs").Contains(catalog.SchemaHash), "Catalog and generated code versions differ.");
            await LubanGenerator.GenerateAsync(profile, true);
            result.validation = true;
            result.success = true;
        }
        catch (Exception exception) { result.failure = exception.ToString(); Debug.LogException(exception); }
        Directory.CreateDirectory("Results");
        File.WriteAllText("Results/git-install-result.json", JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.success ? 0 : 1);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    [Serializable] private sealed class Result
    {
        public bool success;
        public string unityVersion;
        public string resolvedPath;
        public bool automaticWorkspace;
        public bool downloadedCompiler;
        public bool generatedFromMenu;
        public bool validation;
        public string failure;
    }
}
