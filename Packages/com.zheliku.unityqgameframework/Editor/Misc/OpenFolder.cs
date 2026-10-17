//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>Open Unity application directories and the active QGF table workspace.</summary>
    public static class OpenFolder
    {
        [MenuItem("QGF/Open Folder/Save Data", false, 10)]
        public static void OpenFolderPersistentDataPath() => Execute(Application.persistentDataPath);

        [MenuItem("QGF/Open Folder/Streaming Assets", false, 11)]
        public static void OpenFolderStreamingAssetsPath() => Execute(Application.streamingAssetsPath);

        [MenuItem("QGF/Open Folder/Logs", false, 12)]
        public static void OpenFolderConsoleLogPath() => Execute(Path.GetDirectoryName(Application.consoleLogPath));

        [MenuItem("QGF/Open Folder/Table Sources", false, 30)]
        private static void OpenLubanSources() => Execute(LubanGenerator.FolderPath(LubanSettings.instance.Profile.SourceFolder));

        [MenuItem("QGF/Open Folder/Generated Code", false, 31)]
        private static void OpenLubanCode() => Execute(LubanGenerator.FolderPath(LubanSettings.instance.Profile.CodeFolder));

        [MenuItem("QGF/Open Folder/Table Data", false, 32)]
        private static void OpenLubanData() => Execute(LubanGenerator.FolderPath(LubanSettings.instance.Profile.DataFolder));

        [MenuItem("QGF/Open Folder/Table Sources", true)]
        private static bool HasSourceFolder() => HasFolder(LubanSettings.instance.Profile?.SourceFolder);

        [MenuItem("QGF/Open Folder/Generated Code", true)]
        private static bool HasCodeFolder() => HasFolder(LubanSettings.instance.Profile?.CodeFolder);

        [MenuItem("QGF/Open Folder/Table Data", true)]
        private static bool HasDataFolder() => HasFolder(LubanSettings.instance.Profile?.DataFolder);

        private static bool HasFolder(DefaultAsset folder) => folder != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));

        public static void Execute(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Debug.LogWarning("目录不存在：" + folder);
                return;
            }
            // A trailing separator asks the OS to display this directory's contents.
            EditorUtility.RevealInFinder(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        }
    }
}
