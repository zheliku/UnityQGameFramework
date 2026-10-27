using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityQGameFramework.Core;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 在不修改 YooAsset 源码的情况下切换原生收集配置。
    /// </summary>
    [InitializeOnLoad]
    internal static class ResourceCollectorIntegration
    {
        private const string SourceKey = "QGF.Resource.CollectorGuid";
        private const string SelectorName = "QgfResourceSource";
        private static readonly string[] s_ThemedPanels =
        {
            "PublicContainer", "PackageContainer", "GroupContainer", "CollectorContainer",
            "PackageTitle", "GroupTitle", "CollectorTitle"
        };
        private static readonly FieldInfo s_SettingField;
        private static readonly FieldInfo s_BuilderViewsField;
        private static readonly MethodInfo s_RefreshCollector;
        private static BundleCollectorSetting s_Selected;
        private static UnityEngine.Object s_LastSelection;
        private static bool s_RefreshQueued;
        private static double s_NextCheck;

        /// <summary>
        /// 让已打开的原生窗口使用指定收集配置。
        /// </summary>
        /// <param name="source">收集配置。</param>
        internal static void UseCollector(BundleCollectorSetting source)
        {
            EnsureSupported();
            Select(source);
        }

        /// <summary>
        /// 初始化 QGF 与 YooAsset 编辑器窗口的集成。
        /// </summary>
        static ResourceCollectorIntegration()
        {
            // YooAsset 3.0.6 没有公开多配置入口。反射仅限原生配置缓存和窗口刷新，不介入收集或打包。
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            s_SettingField = typeof(BundleCollectorSettingData).GetFields(flags).SingleOrDefault(field => field.FieldType == typeof(BundleCollectorSetting));
            s_BuilderViewsField = typeof(BundleBuilderWindow).GetFields(flags).SingleOrDefault(field => field.FieldType == typeof(Dictionary<string, System.Type>));
            s_RefreshCollector = typeof(BundleCollectorWindow).GetMethod("RefreshWindow", flags, null, System.Type.EmptyTypes, null);
            if (!IsSupported)
            {
                return;
            }
            // 原生窗口可能先于 delayCall 恢复；先提供空缓存，避免官方单配置加载器因多份资产报错。
            if (s_SettingField.GetValue(null) == null && AssetDatabase.FindAssets("t:BundleCollectorSetting").Length > 1)
            {
                var temporary = ScriptableObject.CreateInstance<BundleCollectorSetting>();
                temporary.hideFlags = HideFlags.HideAndDontSave;
                s_SettingField.SetValue(null, temporary);
            }
            EditorApplication.delayCall += Initialize;
            EditorApplication.projectChanged += QueueRefresh;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += UpdateWindows;
        }

        private static bool IsSupported
        {
            get
            {
                return s_SettingField != null && s_BuilderViewsField != null && s_RefreshCollector != null;
            }
        }

        /// <summary>
        /// 打开原生 YooAsset 收集器窗口。
        /// </summary>
        /// <param name="source">收集器资产。</param>
        internal static void OpenCollector(BundleCollectorSetting source)
        {
            EnsureSupported();
            Initialize();
            source = source != null ? source : Selection.activeObject as BundleCollectorSetting;
            if (source != null)
            {
                Select(source);
            }
            BundleCollectorWindow.OpenWindow();
        }

        /// <summary>
        /// 响应收集器资产的双击打开。
        /// </summary>
        /// <param name="instanceId">选中资产实例 ID。</param>
        /// <param name="line">选中代码行号。</param>
        /// <returns>是否已由 QGF 接管该资产的打开操作。</returns>
        [OnOpenAsset]
        private static bool OpenAsset(int instanceId, int line)
        {
#if UNITY_6000_3_OR_NEWER
            var source = EditorUtility.EntityIdToObject(instanceId) as BundleCollectorSetting;
#else
            var source = EditorUtility.InstanceIDToObject(instanceId) as BundleCollectorSetting;
#endif
            if (source == null)
            {
                return false;
            }
            OpenCollector(source);
            return true;
        }

        /// <summary>
        /// 打开指定资源包的原生构建窗口。
        /// </summary>
        /// <param name="source">收集器资产。</param>
        /// <param name="packageName">资源包名称。</param>
        internal static void OpenCollectorBuilder(BundleCollectorSetting source, string packageName = null)
        {
            OpenCollector(source);
            BundleBuilderWindow.OpenWindow();
            var window = Resources.FindObjectsOfTypeAll<BundleBuilderWindow>().FirstOrDefault();
            if (window != null && !string.IsNullOrEmpty(packageName))
            {
                window.rootVisualElement.schedule.Execute(() =>
                {
                    var menu = window.rootVisualElement.Q<Toolbar>("Toolbar")?.Children().OfType<ToolbarMenu>().FirstOrDefault();
                    var action = menu?.menu.MenuItems().OfType<DropdownMenuAction>().FirstOrDefault(item => item.name == packageName);
                    action?.Execute();
                });
            }
        }

        /// <summary>
        /// 检查当前 YooAsset 编辑器版本是否支持集成。
        /// </summary>
        private static void EnsureSupported()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException("This YooAsset Editor version requires an updated QGF ResourceCollectorIntegration adapter.");
            }
        }

        /// <summary>
        /// 订阅编辑器选择和窗口事件。
        /// </summary>
        private static void Initialize()
        {
            if (s_Selected != null)
            {
                return;
            }
            string path = AssetDatabase.GUIDToAssetPath(SessionState.GetString(SourceKey, string.Empty));
            var selected = Selection.activeObject as BundleCollectorSetting
                ?? AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(path) ?? GetSources().FirstOrDefault();
            if (selected != null)
            {
                Select(selected);
            }
            else if ((BundleCollectorSetting)s_SettingField.GetValue(null) == null)
            {
                // 删除最后一份配置后清空原生缓存，不保留已删除 Sample 的包列表。
                var empty = ScriptableObject.CreateInstance<BundleCollectorSetting>();
                empty.hideFlags = HideFlags.HideAndDontSave;
                s_SettingField.SetValue(null, empty);
                SessionState.EraseString(SourceKey);
            }
        }

        /// <summary>
        /// 获取项目内的 YooAsset 收集器配置。
        /// </summary>
        /// <returns>收集器配置数组。</returns>
        private static BundleCollectorSetting[] GetSources()
        {
            return AssetDatabase.FindAssets("t:BundleCollectorSetting", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>).Where(source => source != null).ToArray();
        }

        /// <summary>
        /// 为收集器应用 QGF 的默认构建选项。
        /// </summary>
        /// <param name="source">资源收集配置。</param>
        internal static void EnsureBuildDefaults(BundleCollectorSetting source)
        {
            foreach (BundleCollectorPackage package in source.Packages)
            {
                // Raw 收集规则可确定首次使用的管线；用户在原生窗口选择后以窗口设置为准。
                string key = PlayerSettings.productGUID + "_" + package.PackageName + "_BuildPipelineName";
                if (!EditorPrefs.HasKey(key) && package.IgnoreRuleName == nameof(RawFileIgnoreRule))
                {
                    BundleBuilderSetting.SetPackageBuildPipeline(package.PackageName, nameof(EBuildPipeline.RawFileBuildPipeline));
                }
            }
        }

        /// <summary>
        /// 获取指定资源包的构建版本。
        /// </summary>
        /// <param name="source">资源收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <returns>构建版本字符串。</returns>
        internal static string GetBuildVersion(BundleCollectorSetting source, string packageName)
        {
            return EditorPrefs.GetString(GetVersionKey(source, packageName),
                DateTime.Now.ToString("yyyy-MM-dd") + "-" + (DateTime.Now.Hour * 60 + DateTime.Now.Minute));
        }

        /// <summary>
        /// 生成构建版本的 EditorPrefs 键。
        /// </summary>
        /// <param name="source">资源收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <returns>版本配置键。</returns>
        private static string GetVersionKey(BundleCollectorSetting source, string packageName)
        {
            return "QGF.Resource.Version." + PlayerSettings.productGUID + "."
                + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) + "." + packageName;
        }

        /// <summary>
        /// 将指定收集器设置为原生窗口的数据源。
        /// </summary>
        /// <param name="source">收集器资产。</param>
        private static void Select(BundleCollectorSetting source)
        {
            if (source == null || (s_Selected == source && (BundleCollectorSetting)s_SettingField.GetValue(null) == source))
            {
                return;
            }
            if (s_Selected != null && BundleCollectorSettingData.IsDirty)
            {
                BundleCollectorSettingData.SaveFile();
            }
            EnsureBuildDefaults(source);
            s_Selected = source;
            s_SettingField.SetValue(null, source);
            SessionState.SetString(SourceKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)));
            RefreshWindows();
        }

        /// <summary>
        /// 向 YooAsset 窗口添加资源包选择器。
        /// </summary>
        /// <param name="window">要添加配置选择器的 YooAsset 窗口。</param>
        private static void AddSourceSelector(EditorWindow window)
        {
            VisualElement root = window.rootVisualElement;
            ApplyWindowTheme(root);
            if (root.Q(SelectorName) != null || root.childCount == 0)
            {
                return;
            }
            var selector = new ObjectField("YooAsset Collector")
            {
                name = SelectorName,
                objectType = typeof(BundleCollectorSetting),
                allowSceneObjects = false,
                value = s_Selected,
                tooltip = "Select or drag a YooAsset Collector asset. ResourceSettings uses this same asset."
            };
            selector.RegisterValueChangedCallback(change =>
            {
                var source = change.newValue as BundleCollectorSetting;
                if (source != null)
                {
                    Select(source);
                }
                else
                {
                    selector.SetValueWithoutNotify(s_Selected);
                }
            });
            root.Insert(0, selector);
            if (window is BundleBuilderWindow builder)
            {
                ResourceBatchBuilder.Draw(builder, s_Selected);
            }
        }

        /// <summary>
        /// 为集成控件应用当前编辑器主题。
        /// </summary>
        /// <param name="root">YooAsset 窗口的根元素。</param>
        private static void ApplyWindowTheme(VisualElement root)
        {
            // UXML 的固定深色背景不随皮肤变化；只在 QGF 集成层覆盖底色，文字和输入控件沿用 Unity 主题。
            bool dark = EditorGUIUtility.isProSkin;
            Color surface = dark ? new Color(.22f, .22f, .22f) : new Color(.76f, .76f, .76f);
            Color heading = dark ? new Color(.28f, .28f, .28f) : new Color(.70f, .70f, .70f);
            foreach (string name in s_ThemedPanels)
            {
                VisualElement panel = root.Q(name);
                if (panel == null)
                {
                    continue;
                }
                panel.style.backgroundColor = panel is Label ? heading : surface;
                panel.style.borderLeftWidth = 0;
                panel.style.borderRightWidth = 0;
                panel.style.borderTopWidth = 0;
                panel.style.borderBottomWidth = 0;
                panel.style.paddingLeft = 5;
                panel.style.paddingRight = 5;
                panel.style.paddingTop = 5;
                panel.style.paddingBottom = 5;
            }
        }

        /// <summary>
        /// 处理编辑器选中资源变化。
        /// </summary>
        private static void OnSelectionChanged()
        {
            s_LastSelection = Selection.activeObject;
            if (IsSupported && !ResourceBuildService.IsBuilding && !EditorApplication.isPlayingOrWillChangePlaymode
                && Selection.activeObject is BundleCollectorSetting source)
            {
                Select(source);
            }
        }

        /// <summary>
        /// 同步已打开的 YooAsset 窗口。
        /// </summary>
        private static void UpdateWindows()
        {
            if (EditorApplication.timeSinceStartup < s_NextCheck || EditorApplication.isCompiling)
            {
                return;
            }
            s_NextCheck = EditorApplication.timeSinceStartup + .5;
            // Project 选择通知可能延迟到 GUI 事件；窗口轮询同时同步尚未分发的选择变化。
            if (Selection.activeObject != s_LastSelection)
            {
                OnSelectionChanged();
            }
            if (s_Selected == null)
            {
                Initialize();
            }
            foreach (BundleCollectorWindow window in Resources.FindObjectsOfTypeAll<BundleCollectorWindow>())
            {
                AddSourceSelector(window);
            }
            foreach (BundleBuilderWindow window in Resources.FindObjectsOfTypeAll<BundleBuilderWindow>())
            {
                AddSourceSelector(window);
                BindBuildVersion(window);
            }
        }

        /// <summary>
        /// 绑定构建窗口中的版本字段。
        /// </summary>
        /// <param name="window">YooAsset 编辑器窗口。</param>
        private static void BindBuildVersion(BundleBuilderWindow window)
        {
            var menu = window.rootVisualElement.Q<Toolbar>("Toolbar")?.Children().OfType<ToolbarMenu>().FirstOrDefault();
            var version = window.rootVisualElement.Q<TextField>("BuildVersion");
            if (menu == null || version == null || version.userData != null)
            {
                return;
            }
            if (s_Selected == null)
            {
                return;
            }
            string key = GetVersionKey(s_Selected, menu.text);
            version.userData = key;
            version.SetValueWithoutNotify(GetBuildVersion(s_Selected, menu.text));
            EditorPrefs.SetString(key, version.value);
            version.RegisterValueChangedCallback(change => EditorPrefs.SetString(key, change.newValue));
        }

        /// <summary>
        /// 安排下一帧刷新集成窗口。
        /// </summary>
        internal static void QueueRefresh()
        {
            if (s_RefreshQueued)
            {
                return;
            }
            s_RefreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                s_RefreshQueued = false;
                Initialize();
                if (!ResourceBuildService.IsBuilding)
                {
                    RefreshWindows();
                }
            };
        }

        /// <summary>
        /// 刷新已打开的构建和收集窗口。
        /// </summary>
        private static void RefreshWindows()
        {
            foreach (BundleCollectorWindow window in Resources.FindObjectsOfTypeAll<BundleCollectorWindow>())
            {
                if (window.rootVisualElement.Q<ListView>("PackageListView") != null)
                {
                    s_RefreshCollector.Invoke(window, null);
                }
                window.rootVisualElement.Q(SelectorName)?.RemoveFromHierarchy();
                AddSourceSelector(window);
            }
            foreach (BundleBuilderWindow window in Resources.FindObjectsOfTypeAll<BundleBuilderWindow>())
            {
                ((IDictionary)s_BuilderViewsField.GetValue(window)).Clear();
                window.rootVisualElement.Clear();
                window.CreateGUI();
                AddSourceSelector(window);
                BindBuildVersion(window);
            }
        }
    }
}
