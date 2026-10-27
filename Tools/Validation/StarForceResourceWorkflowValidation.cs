using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using StarForce;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;
using Entry = StarForce.GameEntry;
using Object = UnityEngine.Object;

/// <summary>
/// 在隔离工程检查 StarForce 的真实资源包更新流程。
/// </summary>
public static class StarForceResourceWorkflowValidation
{
    private const string FixtureRoot = "Assets/Authoring/StarForceWorkflow";
    private static readonly List<string> Checks = new List<string>();

    /// <summary>
    /// 验证确认下载、失败重试、取消、清理和缓存启动。
    /// </summary>
    /// <returns>检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Unsaved scene.");
            }
        }

        Checks.Clear();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool background = Application.runInBackground;
        string savePath = Path.Combine(Application.persistentDataPath, "GameFrameworkSetting.dat");
        byte[] saved = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
        var visited = new HashSet<string>();
        var errors = new List<string>();
        bool injectingFailure = false;
        int expectedErrors = 0;
        ResourceSettings settings = null;
        FastServer server = null;
        EditorApplication.CallbackFunction observe = () =>
        {
            if (EditorApplication.isPlaying && Entry.Procedure != null && Entry.Procedure.CurrentProcedure != null)
            {
                visited.Add(Entry.Procedure.CurrentProcedure.GetType().Name);
            }
        };
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind != LogType.Error && kind != LogType.Assert && kind != LogType.Exception)
            {
                return;
            }
            if (injectingFailure)
            {
                expectedErrors++;
            }
            else
            {
                errors.Add(message + "\n" + stack);
            }
        };

        try
        {
            if (AssetDatabase.IsValidFolder(FixtureRoot))
            {
                throw new InvalidOperationException("Remove the previous isolated fixture before running.");
            }
            Directory.CreateDirectory(FixtureRoot);
            AssetDatabase.Refresh();
            var original = AssetDatabase.LoadAssetAtPath<ResourceSettings>("Assets/Examples/02_StarForce/UnityQGameFramework/Resource/ResourceSettings.asset");
            settings = Object.Instantiate(original);
            string packageName = "WF" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_DefaultPackageName").stringValue = packageName;
            var package = serialized.FindProperty("m_Packages").GetArrayElementAtIndex(0);
            package.FindPropertyRelative("m_Name").stringValue = packageName;
            package.FindPropertyRelative("m_Mode").intValue = (int)EPlayMode.HostPlayMode;
            package.FindPropertyRelative("m_UseBuiltinFiles").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(settings, FixtureRoot + "/ResourceSettings.asset");
            var nativeCollector = Object.Instantiate(ResourceBuildService.GetCollector(original));
            nativeCollector.Packages.Single(item => item.PackageName == original.DefaultPackageName).PackageName = packageName;
            AssetDatabase.CreateAsset(nativeCollector, FixtureRoot + "/YooAssetCollector.asset");
            serialized.FindProperty("m_EditorCollector").objectReferenceValue = nativeCollector;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string output = ResourceBuildService.Build(nativeCollector, packageName, false);
            server = new FastServer(output);
            settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(FixtureRoot + "/ResourceSettings.asset");
            serialized = new SerializedObject(settings);
            package = serialized.FindProperty("m_Packages").GetArrayElementAtIndex(0);
            package.FindPropertyRelative("m_HostServer").stringValue = server.Url;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
            // 临时 Launcher 只替换运行设置，资源仍使用导入的 Sample；保存临时场景前解析其资源根目录。
            Check(AssetUtility.GameMainPath == "Assets/Examples/02_StarForce/GameMain",
                "Validation resolves the imported Sample assets before saving its temporary Launcher");
            // 打开场景可能卸载只由本方法局部变量持有的资产，赋值前重新取得持久化对象。
            settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(FixtureRoot + "/ResourceSettings.asset");
            Check(settings != null, "Validation resource settings remain loadable after opening the scene");
            ResourceComponent component = Object.FindFirstObjectByType<ResourceComponent>();
            var resource = new SerializedObject(component);
            resource.FindProperty("m_Settings").objectReferenceValue = settings;
            resource.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), FixtureRoot + "/Launcher.unity");
            Check(component.Settings == settings, "Validation Launcher references its own resource settings");
            Check(File.ReadAllText(FixtureRoot + "/Launcher.unity").Contains(AssetDatabase.AssetPathToGUID(FixtureRoot + "/ResourceSettings.asset")),
                "Saved Launcher contains the resource settings asset reference");
            Directory.CreateDirectory("Artifacts~/StarForceWorkflow");
            File.Copy(FixtureRoot + "/Launcher.unity", "Artifacts~/StarForceWorkflow/launcher.txt", true);
            File.Copy(FixtureRoot + "/ResourceSettings.asset", "Artifacts~/StarForceWorkflow/settings.txt", true);
            File.Copy(FixtureRoot + "/ResourceSettings.asset.meta", "Artifacts~/StarForceWorkflow/settings-meta.txt", true);
            Application.logMessageReceived += capture;
            EditorApplication.update += observe;
            SetCachePath();
            await Play(true);
            Application.runInBackground = true;
            await Until(() => Entry.Resource != null && Entry.Resource == Object.FindFirstObjectByType<ResourceComponent>(), "GameEntry component cache");
            Check(Entry.Resource.Settings != null && Entry.Resource.Settings.DefaultPackageName == packageName,
                "Play Mode preserves the validation resource package: " + Entry.Resource.Settings?.DefaultPackageName + " / " + packageName
                + "; scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            await Until(() => ActiveDialog() != null && Entry.Procedure.CurrentProcedure is ProcedureUpdateResources, "download confirmation");
            var confirmation = ActiveDialog();
            var pending = (ResourceDownloaderOperation)typeof(ProcedureUpdateResources)
                .GetField("m_Downloader", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(Entry.Procedure.CurrentProcedure);
            Check(pending.TotalDownloadCount > 0 && pending.TotalDownloadBytes > 0 && pending.Status == EOperationStatus.None,
                "Prompt shows a real pending download before it starts");
            Check(!Entry.DataTable.IsLoaded, "Preload waits for the download decision");

            ResourceDownloaderOperation cancelled = Entry.Resource.CreateResourceDownloader(new ResourceDownloaderOptions(1, 0));
            using (var cancellation = new CancellationTokenSource())
            {
                UniTask wait = Entry.Resource.DownloadResourcesAsync(cancelled, cancellation.Token);
                cancellation.Cancel();
                bool wasCancelled = await wait.SuppressCancellationThrow();
                Check(wasCancelled && cancelled.IsDone && cancelled.Status == EOperationStatus.Failed,
                    "UniTask cancellation terminates its downloader");
                Check(pending.Status == EOperationStatus.None, "Cancellation leaves the confirmation downloader untouched");
            }

            bool cacheProtected = false;
            try
            {
                await Entry.Resource.ClearCacheAsync(ClearCacheMethods.ClearUnusedBundleFiles);
            }
            catch (FrameworkException)
            {
                cacheProtected = true;
            }
            Check(cacheProtected, "Cache cleanup rejects an outstanding download decision");

            injectingFailure = true;
            server.RejectResources = true;
            confirmation.OnConfirmButtonClick();
            await Until(() => ActiveDialog() != null, "download failure and retry prompt");
            Check(Entry.Procedure.CurrentProcedure is ProcedureUpdateResources && !Entry.DataTable.IsLoaded,
                "Failed downloads do not advance to preload");
            var retry = ActiveDialog();
            server.RejectResources = false;
            injectingFailure = false;
            retry.OnConfirmButtonClick();
            var nextConfirmation = ActiveDialog();
            Check(nextConfirmation != null && nextConfirmation != retry, "Retry can open the next confirmation in the same frame");
            nextConfirmation.OnConfirmButtonClick();
            await Until(() => Entry.Procedure.CurrentProcedure is ProcedureMenu && Entry.UI.GetAllLoadedUIForms().Any(form => form.Logic is MenuForm),
                "download, clear cache and preload to menu", 120);
            Check(visited.Contains(nameof(ProcedureClearUnusedCache)), "Successful download enters the cache cleanup procedure");
            Check(visited.Contains(nameof(ProcedurePreload)) && Entry.DataTable.IsLoaded && Entry.Config.GetInt("Scene.Menu") == 1,
                "Luban and TOML preload through QGF after the resource update");
            Check(ActiveDialog() == null, "Startup dialogs are gone after success");
            int downloads = pending.TotalDownloadCount;
            await Play(false);

            visited.Clear();
            SetCachePath();
            await Play(true);
            Application.runInBackground = true;
            await Until(() => Entry.Procedure.CurrentProcedure is ProcedureMenu && Entry.UI.GetAllLoadedUIForms().Any(form => form.Logic is MenuForm),
                "cached restart to menu");
            Check(!visited.Contains(nameof(ProcedureClearUnusedCache)) && ActiveDialog() == null,
                "Cached startup skips download confirmation and post-download cleanup");
            Check(errors.Count == 0, "No unexpected runtime errors: " + string.Join("\n", errors));
            var result = new { passed = true, unity = Application.unityVersion, downloads, expectedErrors, checks = Checks.ToArray() };
            Directory.CreateDirectory("Artifacts~/StarForceWorkflow");
            File.WriteAllText("Artifacts~/StarForceWorkflow/check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                await Play(false);
            }
            EditorApplication.update -= observe;
            Application.logMessageReceived -= capture;
            server?.Dispose();
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (setup.Any(scene => scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            if (AssetDatabase.IsValidFolder(FixtureRoot))
            {
                AssetDatabase.DeleteAsset(FixtureRoot);
            }
            if (saved != null)
            {
                File.WriteAllBytes(savePath, saved);
            }
            else if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
    }

    private static void SetCachePath()
    {
        UnityQGameFramework.Core.FrameworkEntry.GetModule<IResourceManager>()
            .SetReadWritePath(Path.GetFullPath("Artifacts~/StarForceWorkflow/Runtime"));
    }

    private static BuiltinDialogForm ActiveDialog()
    {
        return Object.FindFirstObjectByType<BuiltinDialogForm>();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
        Checks.Add(message);
    }

    private static async Task Until(Func<bool> condition, string step, int seconds = 45)
    {
        double start = EditorApplication.timeSinceStartup;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup - start > seconds)
            {
                throw new TimeoutException(step + "; procedure=" + Entry.Procedure?.CurrentProcedure?.GetType().Name);
            }
            await Task.Delay(30);
        }
    }

    private static async Task Play(bool enabled)
    {
        EditorApplication.isPlaying = enabled;
        await Until(() => EditorApplication.isPlaying == enabled && EditorApplication.isPlayingOrWillChangePlaymode == enabled, "Play transition");
        await Task.Delay(100);
    }

    private sealed class FastServer : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly string root;
        private readonly Task accept;
        private readonly List<Task> workers = new List<Task>();
        private int reject;

        internal string Url { get; }

        internal bool RejectResources
        {
            get
            {
                return Volatile.Read(ref reject) != 0;
            }
            set
            {
                Volatile.Write(ref reject, value ? 1 : 0);
            }
        }

        internal FastServer(string folder)
        {
            root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            accept = Task.Run(async () =>
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        workers.Add(Serve(client));
                    }
                }
                catch (SocketException) when (stop.IsCancellationRequested) { }
                catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
            });
        }

        private async Task Serve(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    var header = new StringBuilder();
                    var buffer = new byte[1024];
                    while (!header.ToString().Contains("\r\n\r\n"))
                    {
                        int count = await stream.ReadAsync(buffer, 0, buffer.Length, stop.Token).ConfigureAwait(false);
                        if (count == 0 || header.Length > 8192) return;
                        header.Append(Encoding.ASCII.GetString(buffer, 0, count));
                    }
                    string relative = Uri.UnescapeDataString(header.ToString().Split(' ')[1].Split('?')[0]).TrimStart('/');
                    string path = Path.GetFullPath(Path.Combine(root, relative));
                    bool manifest = Path.GetFileName(path).StartsWith("PackageManifest_", StringComparison.Ordinal);
                    bool found = path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path) && (manifest || !RejectResources);
                    byte[] bytes = found ? File.ReadAllBytes(path) : Array.Empty<byte>();
                    byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 " + (found ? "200 OK" : "404 Not Found")
                        + "\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(response, 0, response.Length, stop.Token).ConfigureAwait(false);
                    await stream.WriteAsync(bytes, 0, bytes.Length, stop.Token).ConfigureAwait(false);
                }
            }
            catch (IOException) { }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }

        /// <summary>
        /// 关闭验证服务器。
        /// </summary>
        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            accept.GetAwaiter().GetResult();
            Task.WhenAll(workers).GetAwaiter().GetResult();
            stop.Dispose();
        }
    }
}
