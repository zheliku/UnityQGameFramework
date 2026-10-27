using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制 YooAsset 包的运行配置与编辑器入口。
    /// </summary>
    [CustomEditor(typeof(ResourceSettings))]
    internal sealed class ResourceSettingsInspector : UnityEditor.Editor
    {
        private const string DefaultRoot = "Assets/UnityQGameFramework/Resource";
        private static readonly int[] s_PlayModes =
        {
            (int)EPlayMode.EditorSimulateMode, (int)EPlayMode.OfflinePlayMode, (int)EPlayMode.HostPlayMode,
            (int)EPlayMode.WebPlayMode, (int)EPlayMode.CustomPlayMode
        };
        private static readonly string[] s_PlayModeNames =
        {
            "EditorSimulateMode", "OfflinePlayMode", "HostPlayMode", "WebPlayMode", "CustomPlayMode"
        };
        private bool m_ShowAdvanced;

        /// <summary>
        /// 绘制收集配置、包运行方式和缓存策略。
        /// </summary>
        public override void OnInspectorGUI()
        {
            var settings = (ResourceSettings)target;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || ResourceBuildService.IsBuilding))
            {
                ResourceSettingsEditor.Synchronize(settings);
                serializedObject.Update();
                var current = serializedObject.FindProperty("m_EditorCollector").objectReferenceValue as BundleCollectorSetting;
                EditorGUI.BeginChangeCheck();
                var collector = (BundleCollectorSetting)EditorGUILayout.ObjectField(
                    new GUIContent("YooAsset Collector", "The source of package names, groups, addresses and collection rules."),
                    current, typeof(BundleCollectorSetting), false);
                if (EditorGUI.EndChangeCheck())
                {
                    ResourceSettingsEditor.SetCollector(settings, collector);
                    serializedObject.Update();
                }
                using (new EditorGUI.DisabledScope(collector == null))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Open Collector"))
                        {
                            ResourceCollectorIntegration.OpenCollector(collector);
                        }
                        if (GUILayout.Button("Open Builder"))
                        {
                            ResourceCollectorIntegration.OpenCollectorBuilder(collector, settings.DefaultPackageName);
                        }
                    }
                }
                if (collector == null)
                {
                    EditorGUILayout.HelpBox("Assign a YooAsset Collector to synchronize packages. Collection and build options are edited in the native YooAsset windows.", MessageType.Info);
                    return;
                }
                SerializedProperty packages = serializedObject.FindProperty("m_Packages");
                string[] names = Enumerable.Range(0, packages.arraySize)
                    .Select(index => packages.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name").stringValue).ToArray();
                if (names.Length == 0)
                {
                    EditorGUILayout.HelpBox("Add a package in the YooAsset Collector. Runtime package settings appear here automatically.", MessageType.Info);
                    return;
                }
                SerializedProperty defaultPackage = serializedObject.FindProperty("m_DefaultPackageName");
                int selected = Math.Max(0, Array.IndexOf(names, defaultPackage.stringValue));
                selected = EditorGUILayout.Popup(new GUIContent("Default Package", "Used when a resource API does not specify a package name."), selected, names);
                defaultPackage.stringValue = names[selected];
                EditorGUILayout.HelpBox("Each package uses one YooAsset Play Mode in both the Editor and a built Player. EditorSimulateMode is Editor-only; select OfflinePlayMode, HostPlayMode or another supported mode before building a Player.", MessageType.Info);
                for (int index = 0; index < packages.arraySize; index++)
                {
                    DrawPackage(packages.GetArrayElementAtIndex(index));
                }
                m_ShowAdvanced = EditorGUILayout.Foldout(m_ShowAdvanced, "Cache and Memory", true);
                if (m_ShowAdvanced)
                {
                    SerializedProperty cache = serializedObject.FindProperty("m_CachePathType");
                    int location = cache.intValue == (int)ReadWritePathType.TemporaryCache ? 1 : 0;
                    location = EditorGUILayout.Popup(new GUIContent("Download Cache", "PersistentDataPath is recommended. TemporaryCachePath may be cleared by the operating system."), location,
                        new[] { "PersistentDataPath", "TemporaryCachePath" });
                    cache.intValue = (int)(location == 0 ? ReadWritePathType.PersistentData : ReadWritePathType.TemporaryCache);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinUnloadUnusedAssetsInterval"), new GUIContent("Minimum Unload Interval", "Minimum delay in seconds before a requested unused-asset cleanup."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxUnloadUnusedAssetsInterval"), new GUIContent("Automatic Unload Interval", "Time between automatic unused-asset cleanups, in seconds."));
                }
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 绘制单个包的原生模式及相关初始化参数。
        /// </summary>
        /// <param name="package">资源包序列化属性。</param>
        private static void DrawPackage(SerializedProperty package)
        {
            string name = package.FindPropertyRelative("m_Name").stringValue;
            package.isExpanded = EditorGUILayout.Foldout(package.isExpanded, name, true);
            if (!package.isExpanded)
            {
                return;
            }
            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty mode = package.FindPropertyRelative("m_Mode");
                mode.intValue = EditorGUILayout.IntPopup("Play Mode", mode.intValue, s_PlayModeNames, s_PlayModes);
                bool host = mode.intValue == (int)EPlayMode.HostPlayMode;
                bool web = mode.intValue == (int)EPlayMode.WebPlayMode;
                if (host || web)
                {
                    EditorGUILayout.PropertyField(package.FindPropertyRelative("m_HostServer"), new GUIContent("Host Server", "Base URL for the version file, manifest and bundle files."));
                    EditorGUILayout.PropertyField(package.FindPropertyRelative("m_FallbackServer"), new GUIContent("Fallback Server", "Optional fallback URL. Uses Host Server when empty."));
                }
                if (host)
                {
                    EditorGUILayout.PropertyField(package.FindPropertyRelative("m_UseBuiltinFiles"), new GUIContent("Use Builtin Files", "Include StreamingAssets in HostPlayMode. Disable for remote-only content."));
                }
                if (mode.intValue == (int)EPlayMode.CustomPlayMode)
                {
                    EditorGUILayout.PropertyField(package.FindPropertyRelative("m_InitializerTypeName"), new GUIContent("Package Initializer", "Full type name implementing IResourcePackageInitializer with a parameterless constructor."));
                }
                EditorGUILayout.PropertyField(package.FindPropertyRelative("m_PackageVersion"), new GUIContent("Manifest Version", "Optional pinned version. Leave empty to request the current package version."));
            }
        }

        /// <summary>
        /// 获取默认配置，不存在时创建运行配置和官方 Collector。
        /// </summary>
        /// <returns>默认资源配置资产。</returns>
        internal static ResourceSettings GetOrCreateDefaultSettings()
        {
            const string path = DefaultRoot + "/ResourceSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(path);
            if (settings != null)
            {
                return settings;
            }

            LubanWorkspace.EnsureFolder(DefaultRoot);
            settings = CreateInstance<ResourceSettings>();
            AssetDatabase.CreateAsset(settings, path);
            const string collectorPath = DefaultRoot + "/YooAssetCollector.asset";
            var collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(collectorPath);
            if (collector == null)
            {
                collector = CreateInstance<BundleCollectorSetting>();
                collector.ShowPackageView = true;
                collector.UniqueBundleName = true;
                collector.Packages.Add(new BundleCollectorPackage { PackageName = settings.DefaultPackageName, SupportExtensionless = false });
                AssetDatabase.CreateAsset(collector, collectorPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_EditorCollector").objectReferenceValue = collector;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
