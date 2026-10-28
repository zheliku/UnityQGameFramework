//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 打开应用目录和当前数据表配置目录。
    /// </summary>
    public static class OpenFolder
    {
        /// <summary>
        /// 打开持久化数据目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Save Data", false, 10)]
        public static void OpenFolderPersistentDataPath()
        {
            Execute(Application.persistentDataPath);
        }

        /// <summary>
        /// 打开 StreamingAssets 目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Streaming Assets", false, 11)]
        public static void OpenFolderStreamingAssetsPath()
        {
            Execute(Application.streamingAssetsPath);
        }

        /// <summary>
        /// 打开编辑器日志目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Logs", false, 12)]
        public static void OpenFolderConsoleLogPath()
        {
            Execute(Path.GetDirectoryName(Application.consoleLogPath));
        }

        /// <summary>
        /// 打开 Luban 表源文件目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Table Sources", false, 30)]
        private static void OpenLubanSources()
        {
            Execute(LubanGenerator.SourcePath(LubanSettings.instance.Profile));
        }

        /// <summary>
        /// 打开 Luban 生成代码目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Generated Code", false, 31)]
        private static void OpenLubanCode()
        {
            Execute(LubanGenerator.FolderPath(LubanSettings.instance.Profile.CodeFolder));
        }

        /// <summary>
        /// 打开 Luban 生成数据目录。
        /// </summary>
        [MenuItem("QGF/Open Folder/Table Data", false, 32)]
        private static void OpenLubanData()
        {
            Execute(LubanGenerator.FolderPath(LubanSettings.instance.Profile.DataFolder));
        }

        /// <summary>
        /// 检查 Luban 表源文件目录是否存在。
        /// </summary>
        /// <returns>目录是否存在。</returns>
        [MenuItem("QGF/Open Folder/Table Sources", true)]
        private static bool HasSourceFolder()
        {
            var profile = LubanSettings.instance.Profile;
            if (profile == null)
            {
                return false;
            }
            try
            {
                return Directory.Exists(LubanGenerator.SourcePath(profile));
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查 Luban 生成代码目录是否存在。
        /// </summary>
        /// <returns>目录是否存在。</returns>
        [MenuItem("QGF/Open Folder/Generated Code", true)]
        private static bool HasCodeFolder()
        {
            return HasFolder(LubanSettings.instance.Profile?.CodeFolder);
        }

        /// <summary>
        /// 检查 Luban 生成数据目录是否存在。
        /// </summary>
        /// <returns>目录是否存在。</returns>
        [MenuItem("QGF/Open Folder/Table Data", true)]
        private static bool HasDataFolder()
        {
            return HasFolder(LubanSettings.instance.Profile?.DataFolder);
        }

        /// <summary>
        /// 检查目录资产是否有效。
        /// </summary>
        /// <param name="folder">要检查的目录资产。</param>
        /// <returns>目录是否存在。</returns>
        private static bool HasFolder(DefaultAsset folder)
        {
            return folder != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
        }

        /// <summary>
        /// 在文件管理器中打开目录。
        /// </summary>
        /// <param name="folder">要打开的目录。</param>
        public static void Execute(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Debug.LogWarning("Folder does not exist: " + folder);
                return;
            }
            // A trailing separator asks the OS to display this directory's contents.
            EditorUtility.RevealInFinder(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        }
    }
}
