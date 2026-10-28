using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;
using Object = UnityEngine.Object;

/// <summary>
/// 检查资源模式、旧配置迁移和官方文件系统扩展。
/// </summary>
public static class ResourceConfigurationValidation
{
    private const BindingFlags InternalMembers = BindingFlags.Instance | BindingFlags.NonPublic;
    private static ResourceSettings s_CustomSettings;

    /// <summary>
    /// 在隔离工程构造五种模式并实际读取自定义包。
    /// </summary>
    /// <returns>运行模式与配置归属检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }
        const string path = "Assets/Examples/01_Modules/UnityQGameFramework/Resource/ResourceSettings.asset";
        var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        bool oldOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions oldOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        await SetPlayMode(true);
        var original = AssetDatabase.LoadAssetAtPath<ResourceSettings>(path);
        var settings = Object.Instantiate(original);
        var serialized = new SerializedObject(settings);
        Type backendType = typeof(ResourceComponent).Assembly.GetType("UnityQGameFramework.Runtime.YooAssetResourceBackend", true);
        object backend = Activator.CreateInstance(backendType, InternalMembers, null,
            new object[] { settings, Application.streamingAssetsPath, Application.temporaryCachePath }, null);
        var modes = new Dictionary<string, string>();
        try
        {
            ResourcePackageSettings package = settings.GetPackage();
            int packageIndex = settings.Packages.ToList().FindIndex(item => item.Name == settings.DefaultPackageName);
            var packageProperty = serialized.FindProperty("m_Packages").GetArrayElementAtIndex(packageIndex);
            Require(package.PlayMode == EPlayMode.EditorSimulateMode, "Samples must explicitly select Editor simulation.");
            Require(typeof(ResourcePackageSettings).GetProperty("EditorPlayMode") == null
                && packageProperty.FindPropertyRelative("m_EditorMode") == null
                && packageProperty.FindPropertyRelative("m_RuntimePlayMode") == null, "An environment-specific mode remains.");
            Require(serialized.FindProperty("m_UseEditorSimulation") == null, "A global simulation override remains.");
            foreach (EPlayMode mode in Enum.GetValues(typeof(EPlayMode)))
            {
                if (mode == EPlayMode.None)
                {
                    continue;
                }
                packageProperty.FindPropertyRelative("m_Mode").intValue = (int)mode;
                packageProperty.FindPropertyRelative("m_InitializerTypeName").stringValue = typeof(CustomInitializer).AssemblyQualifiedName;
                packageProperty.FindPropertyRelative("m_HostServer").stringValue = "http://127.0.0.1:18000";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                s_CustomSettings = settings;
                var options = (InitializePackageOptions)backendType.GetMethod("CreateOptions", InternalMembers).Invoke(backend, new object[] { package });
                Type expected = mode == EPlayMode.EditorSimulateMode ? typeof(EditorSimulateModeOptions)
                    : mode == EPlayMode.OfflinePlayMode ? typeof(OfflinePlayModeOptions)
                    : mode == EPlayMode.HostPlayMode ? typeof(HostPlayModeOptions)
                    : mode == EPlayMode.WebPlayMode ? typeof(WebPlayModeOptions) : typeof(CustomPlayModeOptions);
                Require(options.GetType() == expected, "Mode did not produce its native YooAsset options: " + mode);
                Require(package.PlayMode == mode, "The configured mode was replaced by an environment override.");
                modes.Add(mode.ToString(), options.GetType().Name);
            }

            var customOptions = (CustomPlayModeOptions)backendType.GetMethod("CreateOptions", InternalMembers).Invoke(backend, new object[] { package });
            Require(customOptions.FileSystemParameterList.Count == 1, "Custom options did not expose the configured file system.");
            var initialize = backendType.GetMethods(InternalMembers).Single(method => method.Name == "InitializePackageAsync"
                && method.GetParameters().Length == 4 && method.GetParameters()[0].ParameterType == typeof(string));
            var initializeTask = (Cysharp.Threading.Tasks.UniTask<ResourcePackage>)initialize.Invoke(backend,
                new object[] { null, null, null, CancellationToken.None });
            ResourcePackage resourcePackage = await initializeTask;
            AssetHandle handle = resourcePackage.LoadAssetAsync<GameObject>("Resource_Cube");
            while (!handle.IsDone)
            {
                await Task.Delay(10);
            }
            Require(handle.Status == EOperationStatus.Succeeded && handle.AssetObject != null, "Custom mode failed to load its asset.");
            handle.Release();
            var destroy = (Cysharp.Threading.Tasks.UniTask)backendType.GetMethod("DestroyPackageAsync", InternalMembers).Invoke(backend,
                new object[] { null, CancellationToken.None });
            await destroy;

            var inspector = UnityEditor.Editor.CreateEditor(original);
            Require(inspector.GetType().Name == "ResourceSettingsInspector", "ResourceSettings does not use the unified QGF Inspector.");
            Object.DestroyImmediate(inspector);
            string[] serializedFields = typeof(ResourceComponent).GetFields(InternalMembers)
                .Where(field => field.GetCustomAttribute<SerializeField>() != null).Select(field => field.Name).ToArray();
            Require(serializedFields.SequenceEqual(new[] { "m_Settings" }), "ResourceComponent still stores duplicate runtime configuration.");
            return new
            {
                passed = true,
                nativeModes = modes,
                customAssetLoaded = true,
                legacyModesMigrated = true,
                oneSettingsSource = true,
                componentSerializedFields = serializedFields
            };
        }
        finally
        {
            backendType.GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.Public).Invoke(backend, null);
            var completion = (Cysharp.Threading.Tasks.UniTaskCompletionSource)backendType.GetField("m_ShutdownCompletion", InternalMembers).GetValue(backend);
            await completion.Task;
            s_CustomSettings = null;
            Object.DestroyImmediate(settings);
            await SetPlayMode(false);
            EditorSettings.enterPlayModeOptionsEnabled = oldOptionsEnabled;
            EditorSettings.enterPlayModeOptions = oldOptions;
            if (sceneSetup.Length == 0)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
            else
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }
    }

    /// <summary>
    /// 等待编辑器完成运行状态切换。
    /// </summary>
    /// <param name="playing">目标运行状态。</param>
    /// <returns>状态切换任务。</returns>
    private static async Task SetPlayMode(bool playing)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (playing && state == PlayModeStateChange.EnteredPlayMode || !playing && state == PlayModeStateChange.EnteredEditMode)
            {
                completion.TrySetResult(true);
            }
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            await completion.Task;
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }

    /// <summary>
    /// 检查旧版本迁移、新配置默认值和缓存设置。
    /// </summary>
    /// <returns>序列化检查结果。</returns>
    public static object RunAssetMigration()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }
        var defaults = ScriptableObject.CreateInstance<ResourceSettings>();
        try
        {
            defaults.OnBeforeSerialize();
            defaults.OnAfterDeserialize();
            Require(defaults.GetPackage().PlayMode == EPlayMode.EditorSimulateMode, "New settings must keep the simulation default.");
        }
        finally
        {
            Object.DestroyImmediate(defaults);
        }
        for (int version = 0; version <= 3; version++)
        {
            for (int mode = 0; mode < 5; mode++)
            {
                ValidateModeAsset(version, version < 2 ? mode : mode + 1, mode % 2 == 0);
            }
        }
        ValidateModeAsset(0, 2, false);
        ValidateModeAsset(0, 2, true);
        return new
        {
            passed = true,
            legacyModeCases = 15,
            nativeModeRoundTrips = 5,
            globalOverrideIgnored = true,
            newSettingsUseSimulation = true,
            cacheSelection = true,
            cleanupIntervals = true
        };
    }

    /// <summary>
    /// 导入各版本运行模式，并验证保存后仍使用同一个值。
    /// </summary>
    /// <param name="version">原配置版本。</param>
    /// <param name="mode">原模式编号。</param>
    /// <param name="oldSimulation">旧全局模拟开关。</param>
    private static void ValidateModeAsset(int version, int mode, bool oldSimulation)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ResourceModeMigration.asset");
        string yaml = File.ReadAllText("Assets/Examples/01_Modules/UnityQGameFramework/Resource/ResourceSettings.asset");
        yaml = Regex.Replace(yaml, @"(?m)^[ \t]+m_Legacy(?:UseEditorSimulation|EditorMode|PlayMode):[^\r\n]*\r?\n", string.Empty);
        yaml = Regex.Replace(yaml, @"(?m)^  m_SerializationVersion:.*$", version == 0
            ? "  m_UseEditorSimulation: " + (oldSimulation ? "1" : "0") : "  m_SerializationVersion: " + version);
        string modeField = version < 2 ? "m_PlayMode" : version == 2 ? "m_RuntimePlayMode" : "m_Mode";
        string editorField = version == 1 ? "    m_EditorPlayMode: 0\n" : version == 2 ? "    m_EditorMode: 1\n" : string.Empty;
        yaml = Regex.Replace(yaml, @"(?m)^    m_Mode:[^\r\n]*", editorField + "    " + modeField + ": " + mode);
        EPlayMode expected = (EPlayMode)(mode + (version < 2 ? 1 : 0));
        try
        {
            File.WriteAllText(path, yaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(path);
            Require(settings != null, "The mode migration fixture could not be imported.");
            Require(settings.Packages.All(package => package.PlayMode == expected), "Migration changed the original Player mode: version " + version);
            AssetDatabase.ForceReserializeAssets(new[] { path });
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(path);
            Require(settings.Packages.All(package => package.PlayMode == expected), "Saving changed the selected mode.");
            Require(new SerializedObject(settings).FindProperty("m_SerializationVersion").intValue == 3, "The current mode schema was not saved.");
            string saved = File.ReadAllText(path);
            Require(!saved.Contains("m_EditorMode:") && !saved.Contains("m_RuntimePlayMode:")
                && !saved.Contains("m_EditorPlayMode:") && !saved.Contains("m_UseEditorSimulation:"), "Old environment overrides remained in the asset.");
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_CachePathType").intValue = (int)ReadWritePathType.TemporaryCache;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(settings.GetCacheRoot() == Application.temporaryCachePath, "Temporary cache selection was ignored.");
            serialized.FindProperty("m_CachePathType").intValue = (int)ReadWritePathType.PersistentData;
            serialized.FindProperty("m_MinUnloadUnusedAssetsInterval").floatValue = -2f;
            serialized.FindProperty("m_MaxUnloadUnusedAssetsInterval").floatValue = -1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(settings.GetCacheRoot() == Application.persistentDataPath, "Persistent cache selection was ignored.");
            Require(settings.MinUnloadUnusedAssetsInterval == 0f && settings.MaxUnloadUnusedAssetsInterval == 0f, "Invalid cleanup intervals were not corrected.");
        }
        finally
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    /// <summary>
    /// 使用官方编辑器文件系统演示自定义模式。
    /// </summary>
    public sealed class CustomInitializer : IResourcePackageInitializer
    {
        /// <summary>
        /// 创建实际可读取资产的官方 Custom 参数。
        /// </summary>
        /// <param name="settings">资源包配置。</param>
        /// <param name="builtinRoot">内置资源目录。</param>
        /// <param name="cacheRoot">缓存资源目录。</param>
        /// <returns>包含编辑器文件系统的自定义选项。</returns>
        public InitializePackageOptions CreateOptions(ResourcePackageSettings settings, string builtinRoot, string cacheRoot)
        {
            string output = ResourceComponent.EditorPackageBuilder(s_CustomSettings, settings.Name, true);
            var options = new CustomPlayModeOptions();
            options.FileSystemParameterList.Add(FileSystemParameters.CreateDefaultEditorFileSystemParameters(output));
            return options;
        }
    }

    /// <summary>
    /// 检查验证条件。
    /// </summary>
    /// <param name="condition">验证条件。</param>
    /// <param name="message">失败原因。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
