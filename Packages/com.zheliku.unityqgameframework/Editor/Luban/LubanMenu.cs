using System;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 提供配置表生成入口。
    /// </summary>
    public static class LubanMenu
    {
        private const string GenerateMenu = "QGF/Generate Tables %#g";

        /// <summary>
        /// 获取当前是否允许生成数据表。
        /// </summary>
        public static bool CanGenerate
        {
            get
            {
                return !LubanGenerator.IsBusy && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
            }
        }

        /// <summary>
        /// 打开 QGF 项目设置。
        /// </summary>
        [MenuItem("QGF/Settings", false, 50)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(LubanSettings.SettingsPath);
        }

        /// <summary>
        /// 使用全局 Profile 生成数据表。
        /// </summary>
        [MenuItem(GenerateMenu, false, 60)]
        public static async void Generate()
        {
            if (!CanGenerate)
            {
                return;
            }

            try
            {
                LubanProfile profile = LubanWorkspace.EnsureInitialized();
                Debug.Log("Luban generating: " + AssetDatabase.GetAssetPath(profile));
                await LubanGenerator.GenerateAsync(profile);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 检查当前配置是否可以启动 Luban 生成。
        /// </summary>
        /// <returns>是否允许执行生成。</returns>
        [MenuItem(GenerateMenu, true)]
        private static bool ValidateGeneration()
        {
            return CanGenerate;
        }

        /// <summary>
        /// 创建并选用新的配置目录。
        /// </summary>
        public static void CreateWorkspace()
        {
            if (!CanGenerate)
            {
                return;
            }

            try
            {
                var settings = LubanSettings.instance;
                var profile = LubanWorkspace.FindProfile(settings.WorkspaceRoot) ?? LubanWorkspace.Create(settings.WorkspaceRoot);
                settings.Profile = profile;
                Selection.activeObject = profile;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
