using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 让运行配置的包列表跟随官方收集配置。
    /// </summary>
    [InitializeOnLoad]
    public static class ResourceSettingsEditor
    {
        private static bool s_Pending;

        /// <summary>
        /// 订阅配置变更和撤销事件。
        /// </summary>
        static ResourceSettingsEditor()
        {
            EditorApplication.projectChanged += Schedule;
            Undo.undoRedoPerformed += Schedule;
            Schedule();
        }

        /// <summary>
        /// 指定收集配置并同步运行包与原生窗口。
        /// </summary>
        /// <param name="settings">资源运行配置。</param>
        /// <param name="collector">YooAsset 收集配置。</param>
        public static void SetCollector(ResourceSettings settings, BundleCollectorSetting collector)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_EditorCollector").objectReferenceValue = collector;
            serialized.ApplyModifiedProperties();
            Synchronize(settings);
            if (collector != null)
            {
                ResourceCollectorIntegration.UseCollector(collector);
            }
        }

        /// <summary>
        /// 同步包名称，保留同名包的运行设置并移除已删除的包。
        /// </summary>
        /// <param name="settings">资源运行配置。</param>
        /// <returns>是否修改了运行配置。</returns>
        public static bool Synchronize(ResourceSettings settings)
        {
            var serialized = new SerializedObject(settings);
            var collector = serialized.FindProperty("m_EditorCollector").objectReferenceValue as BundleCollectorSetting;
            if (collector == null)
            {
                return false;
            }
            string[] names = collector.Packages.Select(package => package.PackageName).ToArray();
            // 原生 Collector 编辑到一半时可能暂时存在空名或重名，等待有效配置后再同步。
            if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            {
                return false;
            }
            SerializedProperty packages = serialized.FindProperty("m_Packages");
            SerializedProperty defaultName = serialized.FindProperty("m_DefaultPackageName");
            string[] existing = Enumerable.Range(0, packages.arraySize)
                .Select(index => packages.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name").stringValue).ToArray();
            string nextDefault = names.Contains(defaultName.stringValue) ? defaultName.stringValue : names.FirstOrDefault() ?? string.Empty;
            if (existing.SequenceEqual(names) && defaultName.stringValue == nextDefault)
            {
                return false;
            }
            Undo.RecordObject(settings, "Synchronize resource packages");
            for (int index = 0; index < names.Length; index++)
            {
                int found = -1;
                for (int candidate = index; candidate < packages.arraySize; candidate++)
                {
                    if (packages.GetArrayElementAtIndex(candidate).FindPropertyRelative("m_Name").stringValue == names[index])
                    {
                        found = candidate;
                        break;
                    }
                }
                if (found >= 0)
                {
                    packages.MoveArrayElement(found, index);
                    continue;
                }
                packages.InsertArrayElementAtIndex(index);
                SerializedProperty package = packages.GetArrayElementAtIndex(index);
                package.FindPropertyRelative("m_Name").stringValue = names[index];
                package.FindPropertyRelative("m_Mode").intValue = (int)YooAsset.EPlayMode.EditorSimulateMode;
                package.FindPropertyRelative("m_UseBuiltinFiles").boolValue = true;
                foreach (string field in new[] { "m_HostServer", "m_FallbackServer", "m_PackageVersion", "m_InitializerTypeName" })
                {
                    package.FindPropertyRelative(field).stringValue = string.Empty;
                }
            }
            packages.arraySize = names.Length;
            defaultName.stringValue = nextDefault;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return true;
        }

        /// <summary>
        /// 将配置扫描安排到资源导入结束后。
        /// </summary>
        internal static void Schedule()
        {
            if (s_Pending)
            {
                return;
            }
            s_Pending = true;
            EditorApplication.update += UpdateSettings;
        }

        /// <summary>
        /// 同步项目中已引用收集配置的运行设置。
        /// </summary>
        private static void UpdateSettings()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || ResourceBuildService.IsBuilding)
            {
                return;
            }
            EditorApplication.update -= UpdateSettings;
            s_Pending = false;
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceSettings", new[] { "Assets" }))
            {
                var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null)
                {
                    if (Synchronize(settings))
                    {
                        AssetDatabase.SaveAssetIfDirty(settings);
                    }
                }
            }
            ResourceCollectorIntegration.QueueRefresh();
        }
    }

    /// <summary>
    /// 在原生 Collector 保存后安排运行设置和窗口同步。
    /// </summary>
    internal sealed class ResourceSettingsSaveProcessor : AssetModificationProcessor
    {
        /// <summary>
        /// 合并本帧的配置保存通知。
        /// </summary>
        /// <param name="paths">将要保存的资产路径。</param>
        /// <returns>原始资产路径，不拦截原生保存。</returns>
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(path => path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
            {
                ResourceSettingsEditor.Schedule();
            }
            return paths;
        }
    }
}
