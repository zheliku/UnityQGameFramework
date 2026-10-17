using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    public static class LubanMenu
    {
        private const string GenerateMenu = "Unity Q Game Framework/Generate Data Tables %#g";
        private const string WorkspaceMenu = "Unity Q Game Framework/Luban/Create Workspace";
        public static bool CanGenerate => !LubanGenerator.IsBusy && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Unity Q Game Framework/Project Settings", false, 50)]
        public static void OpenSettings() => SettingsService.OpenProjectSettings(LubanSettings.SettingsPath);

        [MenuItem(GenerateMenu, false, 60)]
        public static async void Generate()
        {
            if (!CanGenerate) return;
            try
            {
                LubanProfile profile = LubanWorkspace.EnsureInitialized();
                Debug.Log("Luban generating: " + AssetDatabase.GetAssetPath(profile));
                if (string.IsNullOrWhiteSpace(LubanGenerator.LocalGenerator) && !File.Exists(LubanGenerator.DefaultGenerator))
                    await LubanGenerator.InstallAsync();
                await LubanGenerator.GenerateAsync(profile);
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        [MenuItem(GenerateMenu, true)]
        [MenuItem(WorkspaceMenu, true)]
        private static bool ValidateGeneration() => CanGenerate;

        [MenuItem(WorkspaceMenu, false, 61)]
        public static void CreateWorkspace()
        {
            if (!CanGenerate) return;
            try
            {
                var settings = LubanSettings.instance;
                var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(settings.WorkspaceRoot + "/Editor/LubanProfile.asset") ?? LubanWorkspace.Create(settings.WorkspaceRoot);
                settings.Profile = profile;
                Selection.activeObject = profile;
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        [MenuItem("Unity Q Game Framework/Luban/Select Local Compiler", false, 62)]
        public static void SelectLocalCompiler()
        {
            string path = EditorUtility.OpenFilePanel("选择 Luban " + LubanGenerator.Version + " 的 Luban.dll", string.Empty, "dll");
            if (!string.IsNullOrEmpty(path)) LubanGenerator.LocalGenerator = path;
        }

        [MenuItem("Unity Q Game Framework/Luban/Use Downloaded Compiler", false, 63)]
        public static void UseDownloadedCompiler() => LubanGenerator.LocalGenerator = string.Empty;
    }
}
