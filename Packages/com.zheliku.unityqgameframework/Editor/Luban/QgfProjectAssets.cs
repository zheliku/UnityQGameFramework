using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityQGameFramework.Editor
{
    [FilePath("UserSettings/QgfProjectAssets.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class QgfProjectAssets : ScriptableSingleton<QgfProjectAssets>
    {
        private const string LauncherGuid = "9d2d51bf0fc3dfc42908e3a7a64f4feb";
        private const string PointGameGuid = "f99fe5fffa1724e4ab21a8936d44ab7e";
        [SerializeField] private List<string> m_Workspaces = new List<string>();
        [SerializeField] private string m_Sample;
        [FormerlySerializedAs("m_TutorialSample")]
        [SerializeField] private string m_ModuleExamplesSample;
        [SerializeField] private string m_PointSample;
        [SerializeField] private List<string> m_ModuleSamples = new List<string>();
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
            string guid = RootGuid(LauncherGuid);
            string examplesGuid = RootGuid(ExampleSampleCatalog.ModulesReadmeGuid);
            if (string.IsNullOrEmpty(examplesGuid)) examplesGuid = RootGuid(ExampleSampleCatalog.LegacyAssemblyGuid);
            string pointGuid = PointRootGuid();
            var modules = ExampleSampleCatalog.Modules.SelectMany(entry => new[]
                {
                    RootGuid(entry[1], entry[2]),
                    RootGuid(entry[2])
                })
                .Concat(ExampleSampleCatalog.GroupAssemblyGuids.Select(anchor => RootGuid(anchor)))
                .Where(value => !string.IsNullOrEmpty(value)).Distinct().ToList();
            if (instance.m_Sample == guid && instance.m_ModuleExamplesSample == examplesGuid
                && instance.m_PointSample == pointGuid && instance.m_ModuleSamples.SequenceEqual(modules)) return;
            instance.m_Sample = guid;
            instance.m_ModuleExamplesSample = examplesGuid;
            instance.m_PointSample = pointGuid;
            instance.m_ModuleSamples = modules;
            instance.Save(true);
        }

        private static string RootGuid(string anchorGuid, string companionGuid = null)
        {
            string asset = AssetDatabase.GUIDToAssetPath(anchorGuid);
            if (!asset.StartsWith("Assets/", StringComparison.Ordinal)) return string.Empty;
            string root = Path.GetDirectoryName(asset).Replace('\\', '/');
            if (new[] { "Assets", "Assets/Examples", "Assets/Samples", "Assets/Plugins", "Assets/Editor", "Assets/Resources" }.Contains(root))
                return string.Empty;
            if (companionGuid != null && Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(companionGuid))?.Replace('\\', '/') != root)
                return string.Empty;
            return AssetDatabase.IsValidFolder(root) ? AssetDatabase.AssetPathToGUID(root) : string.Empty;
        }

        private static string PointRootGuid()
        {
            string scene = AssetDatabase.GUIDToAssetPath(PointGameGuid);
            string assembly = AssetDatabase.GUIDToAssetPath(ExampleSampleCatalog.PointAssemblyGuid);
            const string suffix = "/GameMain/Scenes/PointPointPoint.unity";
            if (!scene.EndsWith(suffix, StringComparison.Ordinal))
                return RootGuid(PointGameGuid, ExampleSampleCatalog.PointAssemblyGuid);

            string root = scene.Substring(0, scene.Length - suffix.Length);
            if (!root.StartsWith("Assets/", StringComparison.Ordinal)
                || new[] { "Assets/Examples", "Assets/Samples", "Assets/Plugins", "Assets/Editor", "Assets/Resources" }.Contains(root)
                || assembly != root + "/GameMain/Scripts/UnityQGameFramework.PointPointPoint.asmdef") return string.Empty;
            return AssetDatabase.IsValidFolder(root) ? AssetDatabase.AssetPathToGUID(root) : string.Empty;
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

        [MenuItem("QGF/Uninstall/03 StarForce", false, 80)]
        public static void RemoveSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RemoveSampleRoots(new[] { instance.m_Sample });
            instance.m_Sample = string.Empty;
            instance.Save(true);
        }

        [MenuItem("QGF/Uninstall/03 StarForce", true)]
        private static bool HasSample() => !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(instance.m_Sample);

        [MenuItem("QGF/Uninstall/All Module Samples", false, 80)]
        public static void RemoveAllModuleSamples()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RemoveSampleRoots(instance.m_ModuleSamples.Concat(new[] { instance.m_ModuleExamplesSample }));
            instance.m_ModuleSamples.Clear();
            instance.m_ModuleExamplesSample = string.Empty;
            instance.Save(true);
        }

        [MenuItem("QGF/Uninstall/All Module Samples", true)]
        private static bool HasModuleSamples() => !EditorApplication.isPlayingOrWillChangePlaymode
            && (instance.m_ModuleSamples.Count != 0 || !string.IsNullOrEmpty(instance.m_ModuleExamplesSample));

        [MenuItem("QGF/Uninstall/PointPointPoint", false, 80)]
        public static void RemovePointSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RemoveSampleRoots(new[] { instance.m_PointSample });
            instance.m_PointSample = string.Empty;
            instance.Save(true);
        }

        [MenuItem("QGF/Uninstall/PointPointPoint", true)]
        private static bool HasPointSample() => !EditorApplication.isPlayingOrWillChangePlaymode
            && !string.IsNullOrEmpty(instance.m_PointSample);

        [MenuItem("QGF/Uninstall/Selected Example", false, 79)]
        public static void RemoveSelectedExample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string guid = SelectedExampleGuid();
            if (string.IsNullOrEmpty(guid)) return;
            RemoveSampleRoots(new[] { guid });
            instance.m_ModuleSamples.Remove(guid);
            if (instance.m_Sample == guid) instance.m_Sample = string.Empty;
            if (instance.m_PointSample == guid) instance.m_PointSample = string.Empty;
            if (instance.m_ModuleExamplesSample == guid) instance.m_ModuleExamplesSample = string.Empty;
            instance.Save(true);
        }

        [MenuItem("QGF/Uninstall/Selected Example", true)]
        private static bool HasSelectedExample() => !EditorApplication.isPlayingOrWillChangePlaymode
            && !string.IsNullOrEmpty(SelectedExampleGuid());

        private static string SelectedExampleGuid()
        {
            string selected = AssetDatabase.GetAssetPath(Selection.activeObject);
            return instance.m_ModuleSamples.Concat(new[] { instance.m_Sample, instance.m_PointSample, instance.m_ModuleExamplesSample })
                .FirstOrDefault(guid =>
                {
                    string root = AssetDatabase.GUIDToAssetPath(guid);
                    return !string.IsNullOrEmpty(root) && (selected == root || selected.StartsWith(root + "/", StringComparison.Ordinal));
                });
        }

        private static void RemoveSampleRoots(IEnumerable<string> guids)
        {
            string backup = NewBackup();
            string profile = AssetDatabase.GetAssetPath(LubanSettings.instance.Profile);
            var candidates = guids.Select(AssetDatabase.GUIDToAssetPath).Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
            var roots = candidates.Where(path => !candidates.Any(parent => parent != path && path.StartsWith(parent + "/", StringComparison.Ordinal))).ToArray();
            bool selectedProfileRemoved = roots.Any(root => profile.StartsWith(root + "/", StringComparison.Ordinal));
            foreach (string root in roots) BackupAndDelete(root, backup);
            if (selectedProfileRemoved)
                LubanSettings.instance.Profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(LubanSettings.DefaultWorkspaceRoot + "/Editor/LubanProfile.asset");
        }

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
                foreach (string guid in state.m_Workspaces.Concat(state.m_ModuleSamples)
                    .Concat(new[] { state.m_Sample, state.m_ModuleExamplesSample, state.m_PointSample }).Distinct().ToArray())
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
