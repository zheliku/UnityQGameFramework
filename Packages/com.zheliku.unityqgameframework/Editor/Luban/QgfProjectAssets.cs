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
    /// <summary>
    /// 追踪 QGF 创建的配置与导入的示例，供卸载时备份移除。
    /// </summary>
    [FilePath("UserSettings/QgfProjectAssets.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class QgfProjectAssets : ScriptableSingleton<QgfProjectAssets>
    {
        private const string LauncherGuid = "9d2d51bf0fc3dfc42908e3a7a64f4feb";
        private const string PointGameGuid = "f99fe5fffa1724e4ab21a8936d44ab7e";
        [SerializeField]
        private List<string> m_Workspaces = new List<string>();
        [SerializeField]
        private string m_Sample;
        [FormerlySerializedAs("m_TutorialSample")]
        [SerializeField]
        private string m_ModuleExamplesSample;
        [SerializeField]
        private string m_PointSample;
        [SerializeField]
        private List<string> m_ModuleSamples = new List<string>();
        private static bool s_Removing;

        /// <summary>
        /// 订阅包注册事件并安排示例追踪。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Subscribe()
        {
            Events.registeringPackages += args =>
            {
                if (args.removed.Any(package => package.name == "com.zheliku.unityqgameframework"))
                {
                    RemoveProjectAssets();
                }
            };
            Schedule();
        }

        /// <summary>
        /// 安排一次导入示例追踪。
        /// </summary>
        internal static void Schedule()
        {
            EditorApplication.update -= TrackImportedSample;
            EditorApplication.update += TrackImportedSample;
        }

        /// <summary>
        /// 扫描并记录已导入的 QGF 示例。
        /// </summary>
        private static void TrackImportedSample()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= TrackImportedSample;
            if (s_Removing)
            {
                return;
            }

            string guid = RootGuid(LauncherGuid);
            string examplesGuid = RootGuid(ExampleSampleCatalog.ModulesReadmeGuid);
            if (string.IsNullOrEmpty(examplesGuid))
            {
                examplesGuid = RootGuid(ExampleSampleCatalog.LegacyAssemblyGuid);
            }

            string pointGuid = PointRootGuid();
            var modules = ExampleSampleCatalog.Modules.SelectMany(entry => new[] { RootGuid(entry[1], entry[2]), RootGuid(entry[2]) }).Concat(ExampleSampleCatalog.GroupAssemblyGuids.Select(anchor => RootGuid(anchor))).Where(value => !string.IsNullOrEmpty(value)).Distinct().ToList();
            if (instance.m_Sample == guid && instance.m_ModuleExamplesSample == examplesGuid && instance.m_PointSample == pointGuid && instance.m_ModuleSamples.SequenceEqual(modules))
            {
                return;
            }

            instance.m_Sample = guid;
            instance.m_ModuleExamplesSample = examplesGuid;
            instance.m_PointSample = pointGuid;
            instance.m_ModuleSamples = modules;
            instance.Save(true);
        }

        /// <summary>
        /// 根据锚点 GUID 获取示例根目录 GUID。
        /// </summary>
        /// <param name="anchorGuid">示例锚点 GUID。</param>
        /// <param name="companionGuid">用于确认根目录的伴随资产 GUID。</param>
        /// <returns>示例根目录 GUID；找不到时返回空。</returns>
        private static string RootGuid(string anchorGuid, string companionGuid = null)
        {
            string asset = AssetDatabase.GUIDToAssetPath(anchorGuid);
            if (!asset.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            string root = Path.GetDirectoryName(asset).Replace('\\', '/');
            if (new[]
            {
                "Assets",
                "Assets/Examples",
                "Assets/Samples",
                "Assets/Plugins",
                "Assets/Editor",
                "Assets/Resources"
            }.Contains(root))
            {
                return string.Empty;
            }

            if (companionGuid != null && Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(companionGuid))?.Replace('\\', '/') != root)
            {
                return string.Empty;
            }

            return AssetDatabase.IsValidFolder(root) ? AssetDatabase.AssetPathToGUID(root) : string.Empty;
        }

        /// <summary>
        /// 获取点点点示例根目录 GUID。
        /// </summary>
        /// <returns>示例根目录 GUID；找不到时返回空。</returns>
        private static string PointRootGuid()
        {
            string scene = AssetDatabase.GUIDToAssetPath(PointGameGuid);
            string assembly = AssetDatabase.GUIDToAssetPath(ExampleSampleCatalog.PointAssemblyGuid);
            const string suffix = "/GameMain/Scenes/PointPointPoint.unity";
            if (!scene.EndsWith(suffix, StringComparison.Ordinal))
            {
                return RootGuid(PointGameGuid, ExampleSampleCatalog.PointAssemblyGuid);
            }

            string root = scene.Substring(0, scene.Length - suffix.Length);
            if (!root.StartsWith("Assets/", StringComparison.Ordinal) || new[]
            {
                "Assets/Examples",
                "Assets/Samples",
                "Assets/Plugins",
                "Assets/Editor",
                "Assets/Resources"
            }.Contains(root) || assembly != root + "/GameMain/Scripts/UnityQGameFramework.PointPointPoint.asmdef")
            {
                return string.Empty;
            }

            return AssetDatabase.IsValidFolder(root) ? AssetDatabase.AssetPathToGUID(root) : string.Empty;
        }

        /// <summary>
        /// 记录由 QGF 管理的配置根目录。
        /// </summary>
        /// <param name="root">项目内的配置根目录。</param>
        public static void TrackWorkspace(string root)
        {
            string guid = AssetDatabase.AssetPathToGUID(root);
            if (!string.IsNullOrEmpty(guid) && !instance.m_Workspaces.Contains(guid))
            {
                instance.m_Workspaces.Add(guid);
                instance.Save(true);
            }
        }

        /// <summary>
        /// 备份并移除 StarForce 示例。
        /// </summary>
        [MenuItem("QGF/Uninstall/03 StarForce", false, 80)]
        public static void RemoveSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RemoveSampleRoots(new[] { instance.m_Sample });
            instance.m_Sample = string.Empty;
            instance.Save(true);
        }

        /// <summary>
        /// 检查 StarForce 示例是否可卸载。
        /// </summary>
        /// <returns>是否允许执行对应的卸载操作。</returns>
        [MenuItem("QGF/Uninstall/03 StarForce", true)]
        private static bool HasSample()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(instance.m_Sample);
        }

        /// <summary>
        /// 备份并移除模块示例。
        /// </summary>
        [MenuItem("QGF/Uninstall/All Module Samples", false, 80)]
        public static void RemoveAllModuleSamples()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RemoveSampleRoots(instance.m_ModuleSamples.Concat(new[] { instance.m_ModuleExamplesSample }));
            instance.m_ModuleSamples.Clear();
            instance.m_ModuleExamplesSample = string.Empty;
            instance.Save(true);
        }

        /// <summary>
        /// 检查模块示例是否可卸载。
        /// </summary>
        /// <returns>是否允许执行对应的卸载操作。</returns>
        [MenuItem("QGF/Uninstall/All Module Samples", true)]
        private static bool HasModuleSamples()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && (instance.m_ModuleSamples.Count != 0 || !string.IsNullOrEmpty(instance.m_ModuleExamplesSample));
        }

        /// <summary>
        /// 备份并移除点点点示例。
        /// </summary>
        [MenuItem("QGF/Uninstall/PointPointPoint", false, 80)]
        public static void RemovePointSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            RemoveSampleRoots(new[] { instance.m_PointSample });
            instance.m_PointSample = string.Empty;
            instance.Save(true);
        }

        /// <summary>
        /// 检查点点点示例是否可卸载。
        /// </summary>
        /// <returns>是否允许执行对应的卸载操作。</returns>
        [MenuItem("QGF/Uninstall/PointPointPoint", true)]
        private static bool HasPointSample()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(instance.m_PointSample);
        }

        /// <summary>
        /// 备份并移除当前选中的示例。
        /// </summary>
        [MenuItem("QGF/Uninstall/Selected Example", false, 79)]
        public static void RemoveSelectedExample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string guid = SelectedExampleGuid();
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            RemoveSampleRoots(new[] { guid });
            instance.m_ModuleSamples.Remove(guid);
            if (instance.m_Sample == guid)
            {
                instance.m_Sample = string.Empty;
            }

            if (instance.m_PointSample == guid)
            {
                instance.m_PointSample = string.Empty;
            }

            if (instance.m_ModuleExamplesSample == guid)
            {
                instance.m_ModuleExamplesSample = string.Empty;
            }

            instance.Save(true);
        }

        /// <summary>
        /// 检查当前选中的示例是否可卸载。
        /// </summary>
        /// <returns>是否允许执行对应的卸载操作。</returns>
        [MenuItem("QGF/Uninstall/Selected Example", true)]
        private static bool HasSelectedExample()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(SelectedExampleGuid());
        }

        /// <summary>
        /// 获取当前选中的示例根目录 GUID。
        /// </summary>
        /// <returns>示例根目录 GUID；找不到时返回空。</returns>
        private static string SelectedExampleGuid()
        {
            string selected = AssetDatabase.GetAssetPath(Selection.activeObject);
            return instance.m_ModuleSamples.Concat(new[] { instance.m_Sample, instance.m_PointSample, instance.m_ModuleExamplesSample }).FirstOrDefault(guid =>
            {
                string root = AssetDatabase.GUIDToAssetPath(guid);
                return !string.IsNullOrEmpty(root) && (selected == root || selected.StartsWith(root + "/", StringComparison.Ordinal));
            });
        }

        /// <summary>
        /// 备份并删除指定示例根目录。
        /// </summary>
        /// <param name="guids">已登记的示例根目录 GUID 集合。</param>
        private static void RemoveSampleRoots(IEnumerable<string> guids)
        {
            string backup = NewBackup();
            string profile = AssetDatabase.GetAssetPath(LubanSettings.instance.Profile);
            var candidates = guids.Select(AssetDatabase.GUIDToAssetPath).Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
            var roots = candidates.Where(path => !candidates.Any(parent => parent != path && path.StartsWith(parent + "/", StringComparison.Ordinal))).ToArray();
            bool selectedProfileRemoved = roots.Any(root => profile.StartsWith(root + "/", StringComparison.Ordinal));
            foreach (string root in roots)
            {
                BackupAndDelete(root, backup);
            }

            if (selectedProfileRemoved)
            {
                LubanSettings.instance.Profile = LubanWorkspace.FindProfile(LubanSettings.DefaultWorkspaceRoot);
            }
        }

        /// <summary>
        /// 备份项目配置并卸载框架包。
        /// </summary>
        [MenuItem("QGF/Uninstall/Framework", false, 81)]
        public static void RemoveFramework()
        {
            if (!EditorUtility.DisplayDialog("卸载 QGF", "配置和示例将备份到 QgfBackup~ 后移除。", "卸载", "取消"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (RemoveProjectAssets())
            {
                Client.Remove("com.zheliku.unityqgameframework");
            }
        }

        /// <summary>
        /// 检查 QGF 框架目录是否可卸载。
        /// </summary>
        /// <returns>是否允许执行对应的卸载操作。</returns>
        [MenuItem("QGF/Uninstall/Framework", true)]
        private static bool CanRemoveFramework()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(QgfProjectAssets).Assembly).source != PackageSource.Embedded;
        }

        /// <summary>
        /// 卸载包时备份并删除 QGF 创建的项目资产。
        /// </summary>
        /// <returns>是否成功完成项目资产的备份和移除。</returns>
        private static bool RemoveProjectAssets()
        {
            if (s_Removing)
            {
                return true;
            }

            s_Removing = true;
            var state = instance;
            string backup = NewBackup();
            try
            {
                foreach (string guid in state.m_Workspaces.Concat(state.m_ModuleSamples).Concat(new[] { state.m_Sample, state.m_ModuleExamplesSample, state.m_PointSample }).Distinct().ToArray())
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        BackupAndDelete(path, backup);
                    }
                }

                foreach (string file in new[]
                {
                    "ProjectSettings/UnityQGameFrameworkSettings.asset",
                    "UserSettings/QgfProjectAssets.asset"
                }

                )
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                foreach (string key in new[]
                {
                    "Generator",
                    "Timeout",
                    "Profile"
                }

                )
                {
                    EditorPrefs.DeleteKey("QGF.Luban." + key + "." + Application.dataPath);
                }

                const string parent = "Assets/UnityQGameFramework";
                if (Directory.Exists(parent) && Directory.GetFileSystemEntries(parent).Length == 0)
                {
                    AssetDatabase.DeleteAsset(parent);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                s_Removing = false;
                return false;
            }
        }

        /// <summary>
        /// 创建本次卸载备份目录。
        /// </summary>
        /// <returns>本次卸载备份目录的绝对路径。</returns>
        private static string NewBackup()
        {
            return Path.GetFullPath("QgfBackup~/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        }

        /// <summary>
        /// 备份并删除指定项目目录。
        /// </summary>
        /// <param name="root">Assets 内已确认的目标目录。</param>
        /// <param name="backup">本次备份的根目录。</param>
        private static void BackupAndDelete(string root, string backup)
        {
            string absolute = Path.GetFullPath(root);
            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assets, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(absolute))
            {
                return;
            }

            string destination = Path.Combine(backup, root.Replace('/', Path.DirectorySeparatorChar));
            foreach (string file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, file.Substring(absolute.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(root + ".meta"))
            {
                File.Copy(root + ".meta", destination + ".meta", true);
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.StartsWith(root + "/", StringComparison.Ordinal))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }

            if (!AssetDatabase.DeleteAsset(root))
            {
                throw new IOException("无法移除目录：" + root);
            }

            Debug.Log("QGF 备份：" + destination);
        }
    }

    /// <summary>
    /// 在示例导入后更新项目资源记录。
    /// </summary>
    internal sealed class QgfSampleImportProcessor : AssetPostprocessor
    {
        /// <summary>
        /// 在资产导入后追踪配置和示例根目录。
        /// </summary>
        /// <param name="imported">导入的资产路径。</param>
        /// <param name="deleted">删除的资产路径。</param>
        /// <param name="moved">移动后的资产路径。</param>
        /// <param name="previous">移动前的资产路径。</param>
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
        {
            QgfProjectAssets.Schedule();
        }
    }
}
