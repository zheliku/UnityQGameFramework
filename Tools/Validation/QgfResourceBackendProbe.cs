using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 验证 QGF 组件通过 YooAsset 加载资源。
/// </summary>
public static class QgfResourceBackendProbe
{
    private const string Root = "Assets/QgfResourceProbe";
    private static readonly List<string> s_Assertions = new List<string>();
    private static readonly List<string> s_Errors = new List<string>();

    /// <summary>
    /// 在隔离工程执行检查并退出编辑器。
    /// </summary>
    public static async void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use an isolated validation project.");
        }
        s_Assertions.Clear();
        s_Errors.Clear();
        Application.logMessageReceived += OnLog;
        int exitCode = 1;
        string error = null;
        try
        {
            PrepareTableFixture();
            Prepare();
            if (File.Exists("ExternalDependencies~.json"))
            {
                var qgf = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ResourceComponent).Assembly);
                var unitask = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UniTask).Assembly);
                var yooasset = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(YooAssets).Assembly);
                Require(qgf.source == UnityEditor.PackageManager.PackageSource.LocalTarball, "QGF is loaded from the generated tgz");
                Require(unitask.name == "com.cysharp.unitask" && unitask.version == "2.5.11", "UniTask is loaded from the official external package");
                Require(yooasset.name == "com.tuyoogame.yooasset" && yooasset.version == "3.0.6", "YooAsset is loaded from the official external package");
                string fingerprint = File.ReadAllText("ExternalBackendFingerprint~.txt").Trim();
                using (var sha = SHA256.Create())
                {
                    string path = Path.Combine(qgf.resolvedPath, "Runtime/Unity/Resource/YooAssetResourceBackend.cs");
                    string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                    Require(actual == fingerprint, "The installed tgz contains the current resource backend");
                }
            }
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            await SetPlayMode(true);
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            ResourceComponent resource = GameEntry.GetComponent<ResourceComponent>();
            Require(resource != null && resource.Settings != null, "ResourceComponent uses the saved settings");

            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                var cancelled = await resource.InitializePackageAsync(cancellationToken: cancellation.Token).SuppressCancellationThrow();
                Require(cancelled.IsCanceled && !YooAssets.IsInitialized, "Pre-cancelled initialization creates no package");
            }
            using (var cancellation = new CancellationTokenSource())
            {
                UniTask<ResourcePackage> firstWaiter = resource.InitializePackageAsync(cancellationToken: cancellation.Token);
                cancellation.Cancel();
                ResourcePackage package = await resource.InitializePackageAsync();
                var result = await firstWaiter.SuppressCancellationThrow();
                Require(result.IsCanceled && package.PackageValid, "Initialization is shared and survives a cancelled waiter");
                string catalogLocation = Root + "/Content/Catalog.asset";
                var assetNames = new List<string>();
                foreach (YooAsset.AssetInfo assetInfo in package.GetAllAssetInfos())
                {
                    assetNames.Add(assetInfo.AssetPath);
                }
                File.WriteAllText("Artifacts~/qgf-resource-manifest-assets.txt", string.Join("\n", assetNames));
                Require(package.GetAssetInfo(catalogLocation).IsValid, "Luban Catalog is included in the simulated resource manifest");
            }

            string cubePath = Root + "/Content/Cube.prefab";
            GameObject first = await resource.LoadAssetAsync<GameObject>(cubePath);
            GameObject second = await resource.LoadAssetAsync<GameObject>(cubePath);
            AssetHandle other = await resource.LoadAssetHandleAsync<GameObject>(cubePath, "QgfProbeB");
            resource.UnloadAsset(first);
            Require(second != null && other.IsValid, "Repeated and cross-package borrows survive an independent return");
            bool rejected = false;
            try
            {
                await resource.DestroyPackageAsync();
            }
            catch (FrameworkException)
            {
                rejected = true;
            }
            Require(rejected, "Package destruction rejects an active implicit borrow");
            resource.UnloadAsset(second);
            other.Release();

            using (var cancellation = new CancellationTokenSource())
            {
                UniTask<AssetHandle> pending = resource.LoadAssetHandleAsync<GameObject>(Root + "/Content/Cube2.prefab", cancellationToken: cancellation.Token);
                cancellation.Cancel();
                var result = await pending.SuppressCancellationThrow();
                Require(result.IsCanceled, "An in-flight asset borrow can be cancelled");
                AssetHandle retry = await resource.LoadAssetHandleAsync<GameObject>(Root + "/Content/Cube2.prefab");
                Require(retry.IsValid && retry.AssetObject != null, "A cancelled asset can be loaded again");
                retry.Release();
            }

            int successes = 0;
            int failures = 0;
            object userData = new object();
            var callback = new UniTaskCompletionSource();
            resource.LoadAsset(cubePath, typeof(GameObject), new LoadAssetCallbacks(
                (name, asset, duration, context) =>
                {
                    successes++;
                    Require(ReferenceEquals(context, userData), "Callback preserves user data");
                    resource.UnloadAsset(asset);
                    callback.TrySetResult();
                },
                (name, status, message, context) => callback.TrySetException(new Exception(message))), userData);
            await callback.Task.Timeout(TimeSpan.FromSeconds(20));
            Require(successes == 1, "The callback entry completes once");
            callback = new UniTaskCompletionSource();
            resource.LoadAsset("missing", new LoadAssetCallbacks(
                (name, asset, duration, context) => successes++,
                (name, status, message, context) =>
                {
                    failures++;
                    callback.TrySetResult();
                }));
            await callback.Task.Timeout(TimeSpan.FromSeconds(20));
            Require(successes == 1 && failures == 1, "Missing assets report a single failure");

            byte[] bytes = await resource.LoadBinaryAsync(Root + "/Raw/payload.bin", "QgfProbeRaw");
            await resource.DestroyPackageAsync("QgfProbeRaw");
            Require(bytes.Length == 4 && bytes[3] == 7, "Raw bytes remain owned after package destruction");
            bytes = await resource.LoadBinaryAsync(Root + "/Content/settings.txt");
            Require(bytes.Length > 0, "TextAsset binary loading returns owned bytes");

            ConfigComponent config = GameEntry.GetComponent<ConfigComponent>();
            config.ReadData(Root + "/Content/settings.txt");
            await UniTask.WaitUntil(() => config.HasConfig("Game.Name")).Timeout(TimeSpan.FromSeconds(20));
            Require(config.GetString("Game.Name") == "QGF Probe", "Config loads TOML through the Core resource bridge");
            LocalizationComponent localization = GameEntry.GetComponent<LocalizationComponent>();
            localization.Language = Language.English;
            localization.ReadData(Root + "/Content/language.txt");
            await UniTask.WaitUntil(() => localization.HasRawString("Game.Name")).Timeout(TimeSpan.FromSeconds(20));
            Require(localization.GetString("Game.Name") == "Hello QGF", "Localization loads TOML through the same resource bridge");

            DataTableComponent tables = GameEntry.GetComponent<DataTableComponent>();
            tables.LoadDataTables<Tables>(Root + "/Content/Catalog.asset");
            await UniTask.WaitUntil(() => tables.IsLoaded).Timeout(TimeSpan.FromSeconds(20));
            Require(tables.GetDataTable<TbItem>().Count > 0, "Luban Catalog loads through YooAsset and publishes typed tables");
            tables.UnloadDataTables();

            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                bool cancelled = await tables.LoadDataTablesAsync<Tables>(Root + "/Content/Catalog.asset", cancellationToken: cancellation.Token).SuppressCancellationThrow();
                Require(cancelled && !tables.IsLoading && tables.Count == 0, "Pre-cancelled table loading publishes nothing");
            }
            using (var cancellation = new CancellationTokenSource())
            {
                UniTask pending = tables.LoadDataTablesAsync<Tables>(Root + "/Content/CatalogCancel.asset", cancellationToken: cancellation.Token);
                cancellation.Cancel();
                bool cancelled = await pending.SuppressCancellationThrow();
                Require(cancelled && !tables.IsLoading && tables.Count == 0, "Cancelled table loading does not publish partial tables");
                await tables.LoadDataTablesAsync<Tables>(Root + "/Content/Catalog.asset");
                Require(tables.GetDataTable<TbItem>().Count > 0, "Table loading supports immediate retry after cancellation");
                tables.UnloadDataTables();
            }
            bool tableFailure = false;
            try
            {
                await tables.LoadDataTablesAsync<Tables>("missing-catalog");
            }
            catch (FrameworkException)
            {
                tableFailure = true;
            }
            Require(tableFailure && !tables.IsLoading && tables.Count == 0, "Async table failures throw and leave no registered tables");
            bool loaderCancelled = await tables.LoadDataTablesAsync<CancellingTableLoader>(Root + "/Content/Catalog.asset").SuppressCancellationThrow();
            Require(loaderCancelled && tables.Count == 0, "Cancelling during parsing prevents table registration");
            await tables.LoadDataTablesAsync<Tables>(Root + "/Content/Catalog.asset");
            tables.UnloadDataTables();

            SceneComponent scenes = GameEntry.GetComponent<SceneComponent>();
            string scenePath = Root + "/Content/Target.unity";
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            using (var cancellation = new CancellationTokenSource())
            {
                UniTask pending = scenes.LoadSceneAsync(sceneGuid, cancellation.Token);
                cancellation.Cancel();
                Require(await pending.SuppressCancellationThrow(), "An in-flight scene load reports cancellation");
                await UniTask.WaitUntil(() => !scenes.SceneIsLoading(sceneGuid) && !scenes.SceneIsLoaded(sceneGuid))
                    .Timeout(TimeSpan.FromSeconds(20));
                Require(UnityEngine.SceneManagement.SceneManager.sceneCount == 1, "Cancelled scene loading completes its compensating unload");
            }
            await scenes.LoadSceneAsync(sceneGuid);
            UnityEngine.SceneManagement.Scene loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(Path.GetFileNameWithoutExtension(scenePath));
            string loadedSceneNames = string.Join(", ", Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).name));
            Require(resource != null && UnityEngine.SceneManagement.SceneManager.sceneCount == 2 && loadedScene.isLoaded,
                "SceneComponent loads a GUID-addressed scene and preserves the QGF bootstrap scene; expected="
                + Path.GetFileNameWithoutExtension(scenePath) + ", loaded=[" + loadedSceneNames + "]");
            await scenes.UnloadSceneAsync(sceneGuid);
            Require(UnityEngine.SceneManagement.SceneManager.sceneCount == 1, "Scene unload releases the scene and returns to bootstrap");

            await resource.UnloadUnusedAssetsAsync();
            await resource.ClearCacheAsync(ClearCacheMethods.ClearAllBundleFiles);
            s_Assertions.Add("Disk cache clear completes after memory cleanup");
            await resource.DestroyPackageAsync("QgfProbeB");
            await resource.DestroyPackageAsync();
            Require(!YooAssets.TryGetPackage("QgfProbeA", out _) && !YooAssets.TryGetPackage("QgfProbeB", out _), "Packages unregister after all borrows are returned");

            await resource.InitializePackageAsync();
            GameObject activeBorrow = await resource.LoadAssetAsync<GameObject>(cubePath);
            ResourceDownloaderOperation unstartedDownloader = resource.CreateResourceDownloader(new ResourceDownloaderOptions(1, 0));
            Require(!unstartedDownloader.IsDone, "A newly created downloader remains pending until it is started or cancelled");
            UniTask<AssetHandle> interrupted = resource.LoadAssetHandleAsync<GameObject>(Root + "/Content/Cube3.prefab");
            UnityEngine.Object.Destroy(resource.gameObject);
            var shutdownResult = await interrupted.SuppressCancellationThrow();
            Require(shutdownResult.IsCanceled, "Framework shutdown cancels an outstanding asset request");
            Require(unstartedDownloader.IsDone, "Framework shutdown cancels a downloader that was never started");
            await UniTask.WaitUntil(() => !YooAssets.TryGetPackage("QgfProbeA", out _)).Timeout(TimeSpan.FromSeconds(20));
            Require(GameEntry.GetComponent<ResourceComponent>() == null, "Framework shutdown clears component and package registration");
            UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/QGF.prefab"));
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            resource = GameEntry.GetComponent<ResourceComponent>();
            GameObject restarted = await resource.LoadAssetAsync<GameObject>(cubePath);
            resource.UnloadAsset(restarted);
            await resource.DestroyPackageAsync();
            Require(resource != null, "A new framework instance can reuse package names after shutdown");
            Require(s_Errors.Count == 0, "No unexpected Unity errors");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
        }
        finally
        {
            if (EditorApplication.isPlaying)
            {
                await SetPlayMode(false);
            }
            Application.logMessageReceived -= OnLog;
            if (s_Errors.Count > 0)
            {
                exitCode = 1;
            }
            Directory.CreateDirectory("Artifacts~");
            File.WriteAllText("Artifacts~/qgf-resource-backend.json", JsonUtility.ToJson(new Result
            {
                passed = exitCode == 0,
                unity = Application.unityVersion,
                assertions = s_Assertions.ToArray(),
                errors = s_Errors.ToArray(),
                error = error
            }, true));
            EditorApplication.Exit(exitCode);
        }
    }

    internal static void Prepare()
    {
        if (AssetDatabase.IsValidFolder(Root))
        {
            AssetDatabase.DeleteAsset(Root);
        }
        Directory.CreateDirectory(Root + "/Content");
        Directory.CreateDirectory(Root + "/Raw");
        Directory.CreateDirectory(Root + "/Editor");
        File.WriteAllText(Root + "/Content/settings.txt", "[Game]\nName = \"QGF Probe\"\n");
        File.WriteAllText(Root + "/Content/language.txt", "[English.Game]\nName = \"Hello QGF\"\n");
        File.WriteAllBytes(Root + "/Raw/payload.bin", new byte[] { 1, 3, 5, 7 });
        AssetDatabase.Refresh();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PrefabUtility.SaveAsPrefabAsset(cube, Root + "/Content/Cube.prefab");
        PrefabUtility.SaveAsPrefabAsset(cube, Root + "/Content/Cube2.prefab");
        PrefabUtility.SaveAsPrefabAsset(cube, Root + "/Content/Cube3.prefab");
        UnityEngine.Object.DestroyImmediate(cube);
        AssetDatabase.CopyAsset("Assets/TableFixture/Catalog.asset", Root + "/Content/Catalog.asset");
        AssetDatabase.CopyAsset("Assets/TableFixture/Catalog.asset", Root + "/Content/CatalogCancel.asset");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, Root + "/Content/Target.unity");
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var settings = ScriptableObject.CreateInstance<ResourceSettings>();
        JsonUtility.FromJsonOverwrite("{\"m_DefaultPackageName\":\"QgfProbeA\",\"m_UseEditorSimulation\":true,\"m_Packages\":[{\"m_Name\":\"QgfProbeA\"},{\"m_Name\":\"QgfProbeB\"},{\"m_Name\":\"QgfProbeRaw\"}]}", settings);
        AssetDatabase.CreateAsset(settings, Root + "/ResourceSettings.asset");
        var nativeCollector = ScriptableObject.CreateInstance<BundleCollectorSetting>();
        AssetDatabase.CreateAsset(nativeCollector, Root + "/Editor/YooAssetCollector.asset");
        var settingsBinding = new SerializedObject(settings);
        settingsBinding.FindProperty("m_EditorCollector").objectReferenceValue = nativeCollector;
        settingsBinding.ApplyModifiedPropertiesWithoutUndo();
        foreach (string name in new[] { "QgfProbeA", "QgfProbeB", "QgfProbeRaw" })
        {
            var package = new BundleCollectorPackage { PackageName = name, SupportExtensionless = false };
            nativeCollector.Packages.Add(package);
            package.PackageName = name;
            package.AutoCollectShaders = false;
            package.IncludeAssetGUID = true;
            if (name == "QgfProbeRaw")
            {
                package.IgnoreRuleName = nameof(RawFileIgnoreRule);
            }
            string path = Root + (name == "QgfProbeRaw" ? "/Raw" : "/Content");
            var group = new BundleCollectorGroup { GroupName = "Content" };
            group.Collectors.Add(new BundleCollector
            {
                CollectPath = path,
                CollectorGUID = AssetDatabase.AssetPathToGUID(path),
                PackRuleName = nameof(PackSeparately),
                AddressRuleName = nameof(AddressDisable)
            });
            if (name != "QgfProbeRaw")
            {
                group.Collectors.Add(new BundleCollector
                {
                    CollectPath = "Assets/TableFixture",
                    CollectorGUID = AssetDatabase.AssetPathToGUID("Assets/TableFixture"),
                    PackRuleName = nameof(PackSeparately),
                    AddressRuleName = nameof(AddressDisable)
                });
            }
            package.Groups.Add(group);
            EditorUtility.SetDirty(nativeCollector);
        }
        var root = new GameObject("QGF");
        root.AddComponent<BaseComponent>();
        root.AddComponent<EventComponent>();
        ResourceComponent resource = root.AddComponent<ResourceComponent>();
        var resourceSerialized = new SerializedObject(resource);
        resourceSerialized.FindProperty("m_Settings").objectReferenceValue = settings;
        resourceSerialized.ApplyModifiedPropertiesWithoutUndo();
        root.AddComponent<ConfigComponent>();
        root.AddComponent<LocalizationComponent>();
        root.AddComponent<DataTableComponent>();
        root.AddComponent<SceneComponent>();
        PrefabUtility.SaveAsPrefabAsset(root, Root + "/QGF.prefab");
        EditorSceneManager.SaveScene(scene, Root + "/Bootstrap.unity");
        AssetDatabase.SaveAssets();
    }

    private static void PrepareTableFixture()
    {
        const string source = "Assets/Examples/01_Modules/UnityQGameFramework/Config/Data";
        const string target = "Assets/TableFixture";
        if (!AssetDatabase.IsValidFolder(target))
        {
            Directory.CreateDirectory(target);
            AssetDatabase.Refresh();
        }
        foreach (string fileName in Directory.GetFiles(source, "*.bytes").Select(Path.GetFileName).Concat(new[] { "Catalog.asset" }))
        {
            string sourcePath = source + "/" + fileName;
            string targetPath = target + "/" + fileName;
            if (File.Exists(targetPath))
            {
                AssetDatabase.DeleteAsset(targetPath);
            }
            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                throw new IOException("Cannot copy Luban fixture: " + sourcePath);
            }
        }
        AssetDatabase.Refresh();
        LubanDataTableCatalog catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(target + "/Catalog.asset");
        var serialized = new SerializedObject(catalog);
        SerializedProperty entries = serialized.FindProperty("m_Entries");
        for (int index = 0; index < entries.arraySize; index++)
        {
            SerializedProperty data = entries.GetArrayElementAtIndex(index).FindPropertyRelative("Data");
            string fileName = entries.GetArrayElementAtIndex(index).FindPropertyRelative("Name").stringValue;
            data.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(target + "/" + fileName + ".bytes");
            if (data.objectReferenceValue == null)
            {
                throw new InvalidOperationException("Luban fixture bytes are missing: " + fileName);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 模拟在解析过程中取消的数据表加载器。
    /// </summary>
    public sealed class CancellingTableLoader : IDataTableLoader
    {
        /// <summary>
        /// 获取生成代码的结构版本。
        /// </summary>
        public string SchemaHash
        {
            get
            {
                return ((IDataTableLoader)new Tables()).SchemaHash;
            }
        }

        /// <summary>
        /// 构造数据后取消请求。
        /// </summary>
        /// <param name="loader">数据文件加载方法。</param>
        /// <returns>已解析但不应登记的数据表。</returns>
        public IReadOnlyList<UnityQGameFramework.Core.DataTable.DataTableBase> CreateTables(Func<string, UnityQGameFramework.Luban.ByteBuf> loader)
        {
            var result = ((IDataTableLoader)new Tables()).CreateTables(loader);
            GameEntry.GetComponent<DataTableComponent>().CancelLoadDataTables();
            return result;
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
        s_Assertions.Add(description);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            if (message.Contains("Location is invalid: 'missing'") || message.Contains("Location is invalid: 'missing-catalog'"))
            {
                return;
            }
            s_Errors.Add(message + "\n" + stack);
        }
    }

    private static async Task SetPlayMode(bool playing)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (playing ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
            {
                completion.TrySetResult(true);
            }
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            if (await Task.WhenAny(completion.Task, Task.Delay(20000)) != completion.Task)
            {
                throw new TimeoutException("Play Mode transition did not complete.");
            }
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }

    [Serializable]
    /// <summary>
    /// 资源集成检查结果。
    /// </summary>
    private sealed class Result
    {
        /// <summary>
        /// 检查是否通过。
        /// </summary>
        public bool passed;
        /// <summary>
        /// Unity 版本。
        /// </summary>
        public string unity;
        /// <summary>
        /// 已通过的断言。
        /// </summary>
        public string[] assertions;
        /// <summary>
        /// Unity 错误日志。
        /// </summary>
        public string[] errors;
        /// <summary>
        /// 未通过检查的异常。
        /// </summary>
        public string error;
    }
}
