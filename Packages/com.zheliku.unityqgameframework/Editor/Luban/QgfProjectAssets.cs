using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    [FilePath("UserSettings/QgfProjectAssets.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class QgfProjectAssets : ScriptableSingleton<QgfProjectAssets>
    {
        private const string LauncherGuid = "9d2d51bf0fc3dfc42908e3a7a64f4feb";
        [SerializeField] private List<string> m_Workspaces = new List<string>();
        [SerializeField] private string m_Sample;
        private static bool s_Removing;

        [InitializeOnLoadMethod]
        private static void Subscribe()
        {
            Events.registeringPackages += args =>
            {
                if (args.removed.Any(package => package.name == "com.zheliku.unityqgameframework")) RemoveProjectAssets();
            };
            Schedule();
        }

        internal static void Schedule()
        {
            EditorApplication.update -= TrackImportedSample;
            EditorApplication.update += TrackImportedSample;
        }

        private static void TrackImportedSample()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= TrackImportedSample;
            if (s_Removing) return;
            string launcher = AssetDatabase.GUIDToAssetPath(LauncherGuid);
            string guid = launcher.StartsWith("Assets/", StringComparison.Ordinal)
                ? AssetDatabase.AssetPathToGUID(Path.GetDirectoryName(launcher).Replace('\\', '/')) : string.Empty;
            if (instance.m_Sample == guid) return;
            instance.m_Sample = guid;
            instance.Save(true);
        }

        public static void TrackWorkspace(string root)
        {
            string guid = AssetDatabase.AssetPathToGUID(root);
            if (!string.IsNullOrEmpty(guid) && !instance.m_Workspaces.Contains(guid))
            {
                instance.m_Workspaces.Add(guid);
                instance.Save(true);
            }
        }

        [MenuItem("QGF/Uninstall/StarForce", false, 80)]
        public static void RemoveSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string root = AssetDatabase.GUIDToAssetPath(instance.m_Sample);
            if (!string.IsNullOrEmpty(root))
            {
                bool selectedProfile = AssetDatabase.GetAssetPath(LubanSettings.instance.Profile).StartsWith(root + "/", StringComparison.Ordinal);
                BackupAndDelete(root, NewBackup());
                if (selectedProfile) LubanSettings.instance.Profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(LubanSettings.DefaultWorkspaceRoot + "/Editor/LubanProfile.asset");
            }
            instance.m_Sample = string.Empty;
            instance.Save(true);
        }

        [MenuItem("QGF/Uninstall/StarForce", true)]
        private static bool HasSample() => !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(instance.m_Sample);

        [MenuItem("QGF/Uninstall/Framework", false, 81)]
        public static void RemoveFramework()
        {
            if (!EditorUtility.DisplayDialog("卸载 QGF", "配置和示例将备份到 QgfBackup~ 后移除。", "卸载", "取消")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (RemoveProjectAssets()) Client.Remove("com.zheliku.unityqgameframework");
        }

        [MenuItem("QGF/Uninstall/Framework", true)]
        private static bool CanRemoveFramework() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(QgfProjectAssets).Assembly).source != PackageSource.Embedded;

        private static bool RemoveProjectAssets()
        {
            if (s_Removing) return true;
            s_Removing = true;
            var state = instance;
            string backup = NewBackup();
            try
            {
                foreach (string guid in state.m_Workspaces.Concat(new[] { state.m_Sample }).Distinct().ToArray())
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path)) BackupAndDelete(path, backup);
                }
                foreach (string file in new[] { "ProjectSettings/UnityQGameFrameworkSettings.asset", "UserSettings/QgfProjectAssets.asset" })
                    if (File.Exists(file)) File.Delete(file);
                foreach (string key in new[] { "Generator", "Timeout", "Profile" }) EditorPrefs.DeleteKey("QGF.Luban." + key + "." + Application.dataPath);
                const string parent = "Assets/UnityQGameFramework";
                if (Directory.Exists(parent) && Directory.GetFileSystemEntries(parent).Length == 0) AssetDatabase.DeleteAsset(parent);
                return true;
            }
            catch (Exception exception) { Debug.LogException(exception); s_Removing = false; return false; }
        }

        private static string NewBackup() => Path.GetFullPath("QgfBackup~/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));

        private static void BackupAndDelete(string root, string backup)
        {
            string absolute = Path.GetFullPath(root);
            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assets, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(absolute)) return;
            string destination = Path.Combine(backup, root.Replace('/', Path.DirectorySeparatorChar));
            foreach (string file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, file.Substring(absolute.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(root + ".meta")) File.Copy(root + ".meta", destination + ".meta", true);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.StartsWith(root + "/", StringComparison.Ordinal))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            if (!AssetDatabase.DeleteAsset(root)) throw new IOException("无法移除目录：" + root);
            Debug.Log("QGF 备份：" + destination);
        }
    }

    internal sealed class QgfSampleImportProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous) => QgfProjectAssets.Schedule();
    }
}
