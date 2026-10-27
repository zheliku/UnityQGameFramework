using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using Debug = UnityEngine.Debug;

// Run in a fresh UPM consumer, without invoking EnsureInitialized ourselves.
public static class LubanSetupValidation
{
    private static readonly Stopwatch Timer = new Stopwatch();

    public static void Run()
    {
        Timer.Start();
        EditorApplication.update += WaitForAutomaticSetup;
    }

    private static void WaitForAutomaticSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (LubanSettings.instance.Profile == null && Timer.Elapsed.TotalSeconds < 30) return;
        EditorApplication.update -= WaitForAutomaticSetup;
        var result = new Result { unityVersion = Application.unityVersion };
        try
        {
            var settings = LubanSettings.instance;
            var initial = settings.Profile;
            Require(initial != null, "First installation did not initialize a global Profile.");
            Require(AssetDatabase.GetAssetPath(initial) == LubanSettings.DefaultWorkspaceRoot + "/LubanProfile.asset", "Unexpected initial Profile path.");
            string source = LubanGenerator.SourcePath(initial);
            byte[] workbook = File.ReadAllBytes(source + "/Item.xlsx");
            Require(File.Exists(LubanSettings.DefaultWorkspaceRoot + "/luban.conf"), "Starter schema is missing.");
            Require(Directory.GetFiles(AssetDatabase.GetAssetPath(initial.CodeFolder), "*.cs").Length == 0, "Installation unexpectedly generated business code.");
            Require(!File.Exists(LubanGenerator.DefaultGenerator), "Installation unexpectedly downloaded the compiler.");
            result.automaticWorkspace = true;

            const string custom = "Assets/DeveloperTables";
            const string moved = "Assets/MovedDeveloperTables";
            Require(!Directory.Exists(custom) && !Directory.Exists(moved), "Validation paths are occupied.");
            settings.WorkspaceRoot = custom;
            LubanMenu.CreateWorkspace();
            var created = settings.Profile;
            Require(created != initial && AssetDatabase.GetAssetPath(created) == custom + "/LubanProfile.asset", "Custom workspace was not applied globally.");
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(created));
            Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(custom, moved)), "Cannot move the workspace.");
            Require(settings.Profile == created && LubanGenerator.SourcePath(settings.Profile) == moved + "/Source", "Folder references did not follow the move.");
            Require(File.ReadAllText("ProjectSettings/UnityQGameFrameworkSettings.asset").Contains(guid), "Global Profile GUID was not persisted.");
            result.customDirectoryAndMoves = true;

            settings.Profile = initial;
            settings.WorkspaceRoot = LubanSettings.DefaultWorkspaceRoot;
            Require(AssetDatabase.DeleteAsset(moved), "Cannot remove temporary workspace.");
            Require(LubanWorkspace.EnsureInitialized() == initial, "Repeated initialization changed the active Profile.");
            Require(Convert.ToBase64String(workbook) == Convert.ToBase64String(File.ReadAllBytes(source + "/Item.xlsx")), "Existing source data changed.");
            bool rejected = false;
            try { settings.WorkspaceRoot = "Assets/../../OutsideAssets"; }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "An invalid workspace root was accepted.");
            result.existingWorkspacePreserved = true;
            result.success = true;
        }
        catch (Exception exception) { result.failure = exception.ToString(); Debug.LogException(exception); }
        Directory.CreateDirectory("Results");
        File.WriteAllText("Results/setup-result.json", JsonUtility.ToJson(result, true));
        EditorApplication.Exit(result.success ? 0 : 1);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [Serializable]
    private sealed class Result
    {
        public bool success;
        public string unityVersion;
        public bool automaticWorkspace;
        public bool customDirectoryAndMoves;
        public bool existingWorkspacePreserved;
        public string failure;
    }
}
