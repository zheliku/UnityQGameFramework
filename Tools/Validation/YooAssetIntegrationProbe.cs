using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 检查 YooAsset 的构建配置隔离和资源生命周期。
/// </summary>
public static class YooAssetIntegrationProbe
{
    /// <summary>
    /// 在隔离工程执行检查并退出编辑器。
    /// </summary>
    public static async void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use an isolated validation project.");
        }
        Directory.CreateDirectory("Artifacts~");
        int exitCode = 1;
        string error = null;
        int assertions = 0;
        try
        {
            string probePath = Path.GetFullPath("Assets/Probe");
            string assetsPath = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!probePath.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Probe assets must remain inside the isolated project.");
            }
            if (AssetDatabase.IsValidFolder("Assets/Probe"))
            {
                AssetDatabase.DeleteAsset("Assets/Probe");
            }
            Directory.CreateDirectory("Assets/Probe/Content");
            Directory.CreateDirectory("Assets/Probe/Raw");
            Directory.CreateDirectory("Assets/Probe/Editor");
            File.WriteAllBytes("Assets/Probe/Raw/payload.bin", new byte[] { 1, 3, 5, 7 });
            AssetDatabase.Refresh();
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PrefabUtility.SaveAsPrefabAsset(cube, "Assets/Probe/Content/Cube.prefab");
            PrefabUtility.SaveAsPrefabAsset(cube, "Assets/Probe/Content/Cube2.prefab");
            UnityEngine.Object.DestroyImmediate(cube);

            var collector = ScriptableObject.CreateInstance<BundleCollectorSetting>();
            collector.Packages.Add(new BundleCollectorPackage { PackageName = "UserPackage" });
            AssetDatabase.CreateAsset(collector, "Assets/Probe/Editor/UserCollector.asset");
            var originalPackages = collector.Packages;
            string before = EditorJsonUtility.ToJson(collector);
            string diskBefore = Hash(AssetDatabase.GetAssetPath(collector));
            var sources = new List<BundleCollectorSetting>();
            foreach (string name in new[] { "ProbeA", "ProbeB", "ProbeRaw" })
            {
                var nativeCollector = ScriptableObject.CreateInstance<BundleCollectorSetting>();
                nativeCollector.Packages.Add(new BundleCollectorPackage { PackageName = name, SupportExtensionless = false });
                AssetDatabase.CreateAsset(nativeCollector, "Assets/Probe/Editor/" + name + "Collector.asset");
                var package = nativeCollector.GetPackage(name);
                package.AutoCollectShaders = false;
                var group = new BundleCollectorGroup { GroupName = "Content", AssetTags = "Base" };
                string path = name == "ProbeRaw" ? "Assets/Probe/Raw" : "Assets/Probe/Content";
                group.Collectors.Add(new BundleCollector
                {
                    CollectPath = path,
                    CollectorGUID = AssetDatabase.AssetPathToGUID(path),
                    PackRuleName = nameof(PackSeparately),
                    AddressRuleName = nameof(AddressDisable)
                });
                package.Groups.Add(group);
                if (name == "ProbeRaw")
                {
                    package.IgnoreRuleName = nameof(RawFileIgnoreRule);
                }
                EditorUtility.SetDirty(nativeCollector);
                sources.Add(nativeCollector);
            }
            string moveError = AssetDatabase.MoveAsset("Assets/Probe/Content", "Assets/Probe/MovedContent");
            if (!string.IsNullOrEmpty(moveError))
            {
                throw new InvalidOperationException(moveError);
            }

            var outputs = new List<string>();
            foreach (BundleCollectorSetting source in sources)
            {
                outputs.Add(ResourceBuildService.Build(source, source.Packages[0].PackageName, true));
                if (!ReferenceEquals(collector.Packages, originalPackages) || before != EditorJsonUtility.ToJson(collector)
                    || diskBefore != Hash(AssetDatabase.GetAssetPath(collector)) || EditorUtility.IsDirty(collector))
                {
                    throw new InvalidOperationException("The user collector was changed.");
                }
                assertions++;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            await SetPlayMode(true);
            YooAssets.Initialize();
            var packages = new List<ResourcePackage>();
            for (int index = 0; index < sources.Count; index++)
            {
                ResourcePackage package = YooAssets.CreatePackage(sources[index].Packages[0].PackageName);
                packages.Add(package);
                var initialize = package.InitializePackageAsync(new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(outputs[index])
                });
                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    bool cancelled = await WaitOperation(initialize, cancellation.Token).SuppressCancellationThrow();
                    if (!cancelled)
                    {
                        throw new InvalidOperationException("A cancelled initialization wait was not cancelled.");
                    }
                }
                await initialize;
                RequireSuccess(initialize);
                var version = package.RequestPackageVersionAsync();
                await version;
                RequireSuccess(version);
                var manifest = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(version.PackageVersion, 30));
                await manifest;
                RequireSuccess(manifest);
                assertions++;
            }

            var pending = packages[0].LoadAssetAsync<GameObject>("Assets/Probe/MovedContent/Cube2.prefab");
            using (var cancellation = new CancellationTokenSource())
            {
                UniTask waiting = WaitAsset(pending, cancellation.Token);
                if (waiting.Status != UniTaskStatus.Pending)
                {
                    throw new InvalidOperationException("Expected an in-flight asset load.");
                }
                cancellation.Cancel();
                if (!await waiting.SuppressCancellationThrow() || pending.IsValid)
                {
                    throw new InvalidOperationException("The cancelled request retained its asset handle.");
                }
                assertions++;
            }

            var first = packages[0].LoadAssetAsync<GameObject>("Assets/Probe/MovedContent/Cube.prefab");
            var second = packages[1].LoadAssetAsync<GameObject>("Assets/Probe/MovedContent/Cube.prefab");
            await first;
            await second;
            if (first.Status != EOperationStatus.Succeeded || second.Status != EOperationStatus.Succeeded
                || first.AssetObject == null || second.AssetObject == null)
            {
                throw new InvalidOperationException("The moved prefab did not load from both packages.");
            }
            first.Release();
            if (!second.IsValid || second.AssetObject == null)
            {
                throw new InvalidOperationException("Releasing one package invalidated another package's handle.");
            }
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                if (!await WaitAsset(second, cancellation.Token).SuppressCancellationThrow() || second.IsValid)
                {
                    throw new InvalidOperationException("Cancellation did not release an already completed handle.");
                }
                assertions++;
            }
            assertions++;

            var raw = packages[2].LoadAssetAsync<RawFileObject>("Assets/Probe/Raw/payload.bin");
            await raw;
            if (raw.Status != EOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Raw file loading failed.");
            }
            byte[] bytes = ((RawFileObject)raw.AssetObject).GetBytes();
            raw.Release();
            if (bytes.Length != 4 || bytes[3] != 7)
            {
                throw new InvalidOperationException("Raw file bytes were not retained after release.");
            }
            assertions++;
            foreach (ResourcePackage package in packages)
            {
                var destroy = package.DestroyPackageAsync();
                await destroy;
                RequireSuccess(destroy);
                YooAssets.RemovePackage(package.PackageName);
                assertions++;
            }
            YooAssets.Destroy();
            await SetPlayMode(false);
            exitCode = 0;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
        }
        finally
        {
            File.WriteAllText("Artifacts~/yooasset-integration.json", JsonUtility.ToJson(new Result
            {
                passed = exitCode == 0,
                unity = Application.unityVersion,
                assertions = assertions,
                error = error
            }, true));
            EditorApplication.Exit(exitCode);
        }
    }

    private static UniTask WaitOperation(AsyncOperationBase operation, CancellationToken cancellationToken)
    {
        Type type = typeof(ResourceComponent).Assembly.GetType("UnityQGameFramework.Runtime.ResourceOperation", true);
        return (UniTask)type.GetMethod("WaitAsync", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { operation, cancellationToken });
    }

    private static UniTask WaitAsset(AssetHandle handle, CancellationToken cancellationToken)
    {
        Type type = typeof(ResourceComponent).Assembly.GetType("UnityQGameFramework.Runtime.ResourceOperation", true);
        return (UniTask)type.GetMethod("WaitAssetAsync", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { handle, cancellationToken });
    }

    private static void RequireSuccess(AsyncOperationBase operation)
    {
        if (operation.Status != EOperationStatus.Succeeded)
        {
            throw new InvalidOperationException(operation.Error);
        }
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
        {
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
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
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
            {
                throw new TimeoutException("Play Mode transition did not complete.");
            }
            await Task.Delay(150);
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed;
        public string unity;
        public int assertions;
        public string error;
    }
}
