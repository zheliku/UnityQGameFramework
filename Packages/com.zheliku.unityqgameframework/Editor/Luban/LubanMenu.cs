using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    public static class LubanMenu
    {
        private const string GenerateMenu = "QGF/Generate Tables %#g";
        public static bool CanGenerate => !LubanGenerator.IsBusy && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("QGF/Settings", false, 50)]
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
        private static bool ValidateGeneration() => CanGenerate;

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

        public static void SelectLocalCompiler()
        {
            string path = EditorUtility.OpenFilePanel("选择 Luban " + LubanGenerator.Version + " 的 Luban.dll", string.Empty, "dll");
            if (!string.IsNullOrEmpty(path)) LubanGenerator.LocalGenerator = path;
        }

        public static void UseDownloadedCompiler() => LubanGenerator.LocalGenerator = string.Empty;
    }
}
