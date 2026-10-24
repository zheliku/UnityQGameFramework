using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Core.ObjectPool;

using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateResourceExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use the isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        UnityQGameFramework.Examples.Modules.Editor.BuildResourceExamples.Build();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "12_Resource" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .SelectMany(pair => Enumerable.Range(0, pair.Name == "VerifyCachedResources" || pair.Name == "SelectResourceVariant" ? 4 : pair.Name == "UpdateRemoteResources" ? 3 : pair.Name == "LoadResourcesOnDemand" ? 2 : 1)
                .Select(mode => new { name = pair.Name, layout = pair.Value, mode })).ToList();
        if (names != null) cases = cases.Where(test => names.Contains(test.name)).ToList();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool originalBackground = Application.runInBackground;
        var inputSettings = InputSystem.settings;
        var background = inputSettings.backgroundBehavior;
        var editorInput = inputSettings.editorInputBehaviorInPlayMode;
        var disabled = new HashSet<InputDevice>();
        var errors = new List<string>();
        var completed = new List<object>();
        Mouse mouse = null;
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        File.WriteAllText("Artifacts~/ResourceExamples/active-run.json", JsonConvert.SerializeObject(new {
            active = true, startedUtc = DateTime.UtcNow, optionsEnabled, options, originalBackground,
            inputBackground = background, inputEditor = editorInput, originalScene = setup
        }, Formatting.Indented));
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (var test in cases)
            {
                string sourcePath = (string)test.layout["script"];
                string anchor = AssetDatabase.GUIDToAssetPath("1fde9459f6765bdeacb1340f310a5a10");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Resource sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/01_Modules".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(LoadResourcePackage).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                await Play(false);
                Require(GameEntry.GetComponent<ResourceComponent>() == null, "No retained resource component after stop");
                Require(errors.Count == 0, test.name + " runtime errors: " + string.Join("\n", errors));
                completed.Add(new { example = test.name, scene = script.Substring(0, script.Length - 3) + ".unity", startMode = test.mode, assertions = assertions.Skip(start).ToArray() });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled)) InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = background;
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.WriteAllText("Artifacts~/ResourceExamples/active-run.json", JsonConvert.SerializeObject(new { active = false, stoppedUtc = DateTime.UtcNow }, Formatting.Indented));
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new { passed = true, unity = Application.unityVersion, nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown, Slider and InputField values set by the test; buttons and toggles use native mouse clicks",
            settingsUnchanged = true, scenes = completed, assertionCount = assertions.Count };
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        string report = names == null ? "resource-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/ResourceExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        var resource = QGF.Resource;
        if (name == "LoadResourcesOnDemand")
        {
            await Click(button("prepareButton"), mouse);
            await Until(() => Field<bool>(example, "checkedResources"), 8000, "On-demand version and local check");
            var server = Field<ResourceHttpServer>(example, "server");
            var onDemandFixture = ResourceExamplePaths.Fixture;
            var baseGroup = resource.GetResourceGroup("Base");
            var dataGroup = resource.GetResourceGroup("Data");
            Require(resource.ResourceMode == ResourceMode.UpdatableWhilePlaying && resource.UpdateCandidateCount == 3,
                "On-demand starts with all three resources missing");
            Require(baseGroup.ReadyCount == 0 && dataGroup.ReadyCount == 0 && server.Requests == 1,
                "Check downloads only the version list");
            Require(resource.HasAsset(onDemandFixture.PrefabAssetName) == HasAssetResult.AssetOnDisk
                && resource.HasAsset(onDemandFixture.DiskAssetName) == HasAssetResult.BinaryOnDisk,
                "HasAsset reports registered location even when resource is missing in this mode");
            Require(resource.GenerateReadWriteVersionListLength == 16 * 1024 && resource.UpdatingResourceGroup == null,
                "Writable-list threshold applied; no group update started");
            await Click(button("readButton"), mouse);
            Require(text("binaryText").text.Contains("NotReady") && server.Requests == 1
                && resource.UpdateWaitingWhilePlayingCount == 0, "LoadBinary returns NotReady without starting a download");
            if (mode == 1) await Click(toggle("failureToggle"), mouse);
            await Click(button("loadButton"), mouse);
            await Until(() => resource.UpdateWaitingWhilePlayingCount == 1, 4000, "LoadAsset queues its missing Bundle download");
            Require(resource.LoadWaitingTaskCount + resource.LoadWorkingAgentCount == 1
                && resource.UpdatingResourceGroup == null && resource.UpdateWaitingCount == 0,
                "One load waits for one on-demand download without a group update");
            await Screenshot("resource-on-demand-pending-" + mode);
            if (mode == 1)
            {
                await Until(() => Field<bool>(example, "downloadFailed"), 8000, "On-demand download exhausts retries");
                await Until(() => QGF.Download.WorkingAgentCount + QGF.Download.WaitingTaskCount == 0, 4000, "Failed download queue drained");
                Require(server.Requests == 3 && resource.UpdateWaitingWhilePlayingCount == 0 && !baseGroup.Ready,
                    "Initial download and one retry failed; resource remains missing");
                await Task.Delay(500);
                Require(Field<bool>(example, "loading") && Field<int>(example, "loadCount") == 0
                    && Field<int>(example, "loadFailureCount") == 0 && resource.LoadWaitingTaskCount + resource.LoadWorkingAgentCount == 1,
                    "Download failure event does not complete or fail the pending LoadAsset task");
                await Screenshot("resource-on-demand-recover");
                await Click(toggle("failureToggle"), mouse);
                await Click(button("recoverButton"), mouse);
                Require(resource.UpdatingResourceGroup?.Name == "Base", "Recovery updates Base without resubmitting LoadAsset");
            }
            await Until(() => Field<GameObject>(example, "instance") != null && !Field<bool>(example, "updating"), 12000, "Original LoadAsset completes after download");
            Require(baseGroup.Ready && !dataGroup.Ready && resource.UpdateCandidateCount == 2,
                "Loading Cube downloads only Base; Data stays missing");
            Require(Field<int>(example, "loadCount") == 1 && Field<int>(example, "loadFailureCount") == 0
                && resource.UpdateWaitingWhilePlayingCount == 0, "Original asset request completes exactly once");
            Require(File.Exists(Path.Combine(resource.ReadWritePath, "GameFrameworkList.dat")), "Completed batch persists writable resource list");
            int requests = server.Requests;
            await Click(button("readButton"), mouse);
            Require(text("binaryText").text.Contains("NotReady") && server.Requests == requests,
                "Binary is still unavailable after loading an unrelated Bundle");
            await Click(button("updateDataButton"), mouse);
            Require(resource.UpdatingResourceGroup?.Name == "Data" && resource.UpdateWaitingWhilePlayingCount == 0,
                "Explicit Data update has its own queue and group");
            await Until(() => !Field<bool>(example, "updating") && dataGroup.Ready, 12000, "Data group completes");
            await Click(button("readButton"), mouse);
            await Until(() => text("binaryText").text.Contains("读取成功"), 5000, "Binary reads after explicit update");
            Require(resource.UpdateCandidateCount == 0 && resource.GetResourceGroup().Ready
                && resource.UpdatingResourceGroup == null && server.Requests == requests + 2,
                "All three resources ready; Data downloaded its two resources exactly once");
            Require(text("binaryText").text.Contains(Encoding.UTF8.GetString(File.ReadAllBytes(onDemandFixture.DiskAssetName))),
                "Downloaded binary bytes match source content");
            await Screenshot("resource-on-demand-ready-" + mode);
            return;
        }
        if (name == "LoadCubePrefab")
        {
            await Until(() => Field<GameObject>(example, "asset") != null, 6000, "Editor prefab load");
            Require(QGF.Base.EditorResourceMode && Field<GameObject>(example, "first") != null && Field<GameObject>(example, "second") != null,
                "Editor source asset produces two scene instances");
            await Click(button("removeButton"), mouse);
            Require(Field<GameObject>(example, "first") == null && Field<GameObject>(example, "second") != null, "Destroying one instance preserves the other");
            await Click(button("releaseButton"), mouse);
            await Until(() => Field<GameObject>(example, "asset") == null, 4000, "Asset released after instances");
            Require(Field<GameObject>(example, "second") == null, "All scene instances removed before release");
            return;
        }
        if (name == "InspectResourceCache")
        {
            await Click(button("initializeButton"), mouse);
            await Until(() => Field<bool>(example, "initialized"), 10000, "Cache scene Package initialization");
            var assetPool = Field<ObjectPoolBase>(example, "assetPool");
            var resourcePool = Field<ObjectPoolBase>(example, "resourcePool");
            Require(assetPool.Count == 0 && resourcePool.Count == 0 && resource.ResourceMode == ResourceMode.Package,
                "Initialization creates empty real asset/resource pools");
            for (int policy = 0; policy < 3; policy++)
            {
                dropdown("policyMode").value = policy;
                await Task.Delay(100);
                Require(resource.AssetCapacity == (policy == 1 ? 0 : 4) && resource.ResourceCapacity == resource.AssetCapacity
                    && resource.AssetExpireTime == (policy == 2 ? 2 : 30) && resource.ResourceExpireTime == resource.AssetExpireTime
                    && resource.AssetAutoReleaseInterval == (policy == 2 ? .25f : 30) && resource.ResourceAutoReleaseInterval == resource.AssetAutoReleaseInterval,
                    "Policy updates component and Core pool settings " + policy);
                Require(assetPool.Capacity == resource.AssetCapacity && resourcePool.Capacity == resource.ResourceCapacity
                    && assetPool.ExpireTime == resource.AssetExpireTime && resourcePool.AutoReleaseInterval == resource.ResourceAutoReleaseInterval
                    && assetPool.Priority == resource.AssetPriority && resourcePool.Priority == resource.ResourcePriority,
                    "Live settings reach the actual asset and resource pools " + policy);
                await Click(button("loadButton"), mouse);
                await Until(() => Field<GameObject>(example, "instance") != null && !Field<bool>(example, "busy"), 6000, "Cache Cube load " + policy);
                Require(assetPool.Count == 1 && resourcePool.Count == 1 && assetPool.GetAllObjectInfos().Single().SpawnCount == 1
                    && resourcePool.GetAllObjectInfos().Single().SpawnCount == 1, "One scene instance holds asset and bundle references " + policy);
                Require(resource.LoadWorkingAgentCount == 0 && resource.LoadWaitingTaskCount == 0, "Load task queue drained " + policy);
                if (policy == 0)
                {
                    // 清理计时从本次强制请求开始，预约检查不依赖前面步骤的耗时。
                    int cleanups = Field<int>(example, "cleanupCount");
                    await Click(button("forceButton"), mouse);
                    await Until(() => Field<int>(example, "cleanupCount") > cleanups, 5000, "Forced Unity cleanup resets elapsed timer");
                    Require(Field<GameObject>(example, "instance") != null && assetPool.Count == 1 && resourcePool.Count == 1,
                        "Unity cleanup preserves in-use QGF assets and instance");
                    await Screenshot("resource-cache-loaded");
                    int id = Field<int>(example, "currentAssetId");
                    await Click(button("returnButton"), mouse);
                    await Until(() => Field<GameObject>(example, "asset") == null && !Field<bool>(example, "busy"), 4000, "Return cached asset after instance destruction");
                    Require(assetPool.Count == 1 && assetPool.GetAllObjectInfos().Single().SpawnCount == 0 && assetPool.CanReleaseCount == 1
                        && resourcePool.GetAllObjectInfos().Single().SpawnCount == 1,
                        "Returned asset remains cached and still holds its bundle");
                    await Screenshot("resource-cache-retained");
                    cleanups = Field<int>(example, "cleanupCount");
                    await Click(toggle("gcToggle"), mouse);
                    await Click(button("forceButton"), mouse);
                    await Until(() => Field<int>(example, "cleanupCount") > cleanups, 5000, "Forced Unity cleanup with GC request");
                    Require(assetPool.Count == 1 && resourcePool.Count == 1, "Unity cleanup does not empty QGF caches");
                    ((Slider)data.FindProperty("minimumSlider").objectReferenceValue).value = 5;
                    ((Slider)data.FindProperty("maximumSlider").objectReferenceValue).value = 12;
                    cleanups = Field<int>(example, "cleanupCount");
                    await Click(button("reserveButton"), mouse);
                    await Task.Delay(400);
                    Require(Field<int>(example, "cleanupCount") == cleanups && resource.LastUnloadUnusedAssetsOperationElapseSeconds < resource.MinUnloadUnusedAssetsInterval,
                        "Reserved cleanup waits for minimum interval instead of running immediately");
                    await Until(() => Field<int>(example, "cleanupCount") > cleanups, 7000, "Reserved Unity cleanup executes at minimum interval");
                    Require(resource.MinUnloadUnusedAssetsInterval == 5 && resource.MaxUnloadUnusedAssetsInterval == 12,
                        "GUI interval settings applied");
                    await Click(button("loadButton"), mouse);
                    await Until(() => Field<GameObject>(example, "instance") != null && !Field<bool>(example, "busy"), 6000, "Reload from retained cache");
                    Require(Field<int>(example, "currentAssetId") == id, "Reload reuses retained prefab asset");
                    await Click(button("returnButton"), mouse);
                    await Until(() => Field<GameObject>(example, "asset") == null && !Field<bool>(example, "busy"), 4000, "Return reloaded asset");
                }
                else
                {
                    if (policy == 2)
                    {
                        await Task.Delay(2300);
                        Require(assetPool.Count == 1 && resourcePool.Count == 1 && Field<GameObject>(example, "instance") != null,
                            "Expired policy cannot release assets while they remain borrowed");
                    }
                    await Click(button("returnButton"), mouse);
                    await Until(() => Field<GameObject>(example, "asset") == null && !Field<bool>(example, "busy"), 4000, "Return asset under policy " + policy);
                    await Until(() => assetPool.Count == 0 && resourcePool.Count == 0, 6000, "Policy releases asset then bundle " + policy);
                    Require(assetPool.CanReleaseCount == 0 && resourcePool.CanReleaseCount == 0 && Field<GameObject>(example, "instance") == null,
                        "No cache or live instance after pool eviction " + policy);
                    await Screenshot("resource-cache-policy-" + policy);
                }
            }
            int lastCleanup = Field<int>(example, "cleanupCount");
            await Click(button("forceButton"), mouse);
            await Until(() => Field<int>(example, "cleanupCount") > lastCleanup, 5000, "Reset timer for automatic maximum-interval check");
            lastCleanup = Field<int>(example, "cleanupCount");
            await Task.Delay(500);
            Require(Field<int>(example, "cleanupCount") == lastCleanup, "No immediate automatic cleanup before maximum interval");
            await Until(() => Field<int>(example, "cleanupCount") > lastCleanup, 14000, "Unity cleanup runs at maximum interval without a reservation");
            await Screenshot("resource-cache-cleanup");
            return;
        }
        if (name == "SelectResourceVariant")
        {
            var variantFixture = ResourceExamplePaths.Fixture;
            bool hard = mode >= 2, custom = mode % 2 == 1;
            dropdown("variantMode").value = hard ? 1 : 0;
            if (toggle("customDecryptToggle").isOn != custom) await Click(toggle("customDecryptToggle"), mouse);
            await Click(button("initializeButton"), mouse);
            await Until(() =>
            {
                if (text("stateText").text.StartsWith("初始化失败")) throw new Exception(text("stateText").text);
                return Field<bool>(example, "initialized");
            }, 10000, "Variant Package initialized " + mode);
            try
            {
                resource.SetDecryptResourceCallback(null);
                throw new Exception("Changing the decrypt callback after agent creation was accepted.");
            }
            catch (FrameworkException exception)
            {
                Require(exception.Message.Contains("before add load resource agent"), "Callback configuration locks after Resource.Start");
            }
            Require(resource.ResourceMode == ResourceMode.Package && resource.CurrentVariant == (hard ? "hard" : "normal"),
                "Selected variant set before initialization");
            Require(resource.AssetCount == 2 && resource.ResourceCount == 2 && resource.ResourceGroupCount == 3,
                "One variant and one common resource registered");
            Require(resource.InternalResourceVersion == 2 && resource.ApplicableGameVersion == Application.version,
                "Variant package version metadata");
            string selected = hard ? variantFixture.HardVariantAsset : variantFixture.NormalVariantAsset;
            string other = hard ? variantFixture.NormalVariantAsset : variantFixture.HardVariantAsset;
            Require(resource.HasAsset(selected) == HasAssetResult.BinaryOnFileSystem && resource.HasAsset(other) == HasAssetResult.NotExist
                && resource.HasAsset(variantFixture.PrefabAssetName) == HasAssetResult.AssetOnDisk, "Current-only assets and common Cube availability");
            Require(!dropdown("variantMode").interactable && !toggle("customDecryptToggle").interactable,
                "Initialization locks variant choice for this run");
            await Click(button("loadButton"), mouse);
            await Until(() => Field<object>(example, "payload") != null && !Field<bool>(example, "loading"), 10000, "Decrypted variant payload applied " + mode);
            var variantCube = Field<GameObject>(example, "instance");
            Require(variantCube != null && Mathf.Approximately(variantCube.transform.localScale.x, hard ? 1.7f : 1f), "Loaded payload changes visible Cube size");
            var block = new MaterialPropertyBlock();
            variantCube.GetComponent<Renderer>().GetPropertyBlock(block);
            ColorUtility.TryParseHtmlString(hard ? "#FF9457" : "#37B8FF", out var expectedColor);
            Require(Vector4.Distance(block.GetColor("_BaseColor"), expectedColor) < .001f,
                "Loaded payload changes visible Cube color without changing shared material");
            Require(text("payloadText").text.Contains(hard ? "挑战模式" : "标准模式") && text("payloadText").text.Contains(hard ? "伤害 25" : "伤害 10"),
                "GUI shows actual decoded payload");
            Require(Field<int>(example, "decryptCount") == (custom ? 1 : 0), "Default/custom decryption route counted correctly");
            await Screenshot("resource-variant-" + mode);
            byte[] originalVariant = File.ReadAllBytes(selected);
            var buffer = Enumerable.Repeat((byte)205, originalVariant.Length + 5).ToArray();
            int loaded = resource.LoadBinaryFromFileSystem(selected, buffer, 2, originalVariant.Length);
            Require(loaded == originalVariant.Length && buffer.Skip(2).Take(loaded).SequenceEqual(originalVariant)
                && buffer.Take(2).Concat(buffer.Skip(2 + loaded)).All(value => value == 205),
                "Encrypted complete-file read respects target offset and preserves buffer guards");
            foreach (int offset in new[] { 0, 3, 17, 217, 223 })
            {
                byte[] segment = resource.LoadBinarySegmentFromFileSystem(selected, offset, 8);
                Require(segment.SequenceEqual(originalVariant.Skip(offset).Take(8)), "Encrypted segment source offset " + offset);
                var target = Enumerable.Repeat((byte)205, 13).ToArray();
                int read = resource.LoadBinarySegmentFromFileSystem(selected, offset, target, 2, 8);
                Require(read == 8 && target.Skip(2).Take(read).SequenceEqual(originalVariant.Skip(offset).Take(read))
                    && target.Take(2).Concat(target.Skip(2 + read)).All(value => value == 205),
                    "Encrypted segment target guards and source offset " + offset);
            }
            var emptyTarget = Enumerable.Repeat((byte)205, 5).ToArray();
            Require(resource.LoadBinarySegmentFromFileSystem(selected, originalVariant.Length + 10, emptyTarget, 2, 2) == 0
                && emptyTarget.All(value => value == 205), "Encrypted read beyond end preserves destination");
            Require(variantCube.GetComponent<Renderer>().sharedMaterial.shader.isSupported, "Variant Cube uses supported URP shader");
            return;
        }
        if (name == "ApplyResourcePack")
        {
            var packFixture = ResourceExamplePaths.Fixture;
            Require(!QGF.Base.EditorResourceMode && resource.ResourceMode == ResourceMode.Updatable,
                "Pack scene uses Updatable, not Package initialization");
            Require(Directory.GetFiles(resource.ReadOnlyPath).Length == 0, "Resource pack starts with empty read-only area");
            await Click(button("prepareButton"), mouse);
            await Until(() => Field<bool>(example, "prepared"), 10000, "Version list and missing-resource check");
            Require(resource.UpdateCandidateCount == 3 && !resource.GetResourceGroup().Ready,
                "Three missing resources before applying the pack");
            var server = Field<ResourceHttpServer>(example, "server");
            Require(server.Requests == 1, "Only the remote version list has been downloaded");
            byte[] original = File.ReadAllBytes(Field<string>(example, "pack"));
            byte[] damaged = File.ReadAllBytes(Field<string>(example, "damagedPack"));
            Require(original.Length == damaged.Length && !original.SequenceEqual(damaged), "Damaged copy has same length but different content");
            await Click(toggle("corruptToggle"), mouse);
            await Click(button("verifyButton"), mouse);
            Require(!Field<bool>(example, "verified") && text("stateText").text.Contains("校验失败") && !button("applyButton").interactable,
                "CRC rejects damaged resource pack and prevents application");
            await Screenshot("resource-pack-rejected");
            await Click(toggle("corruptToggle"), mouse);
            await Click(button("verifyButton"), mouse);
            Require(Field<bool>(example, "verified") && resource.UpdateCandidateCount == 3,
                "Verifying the intact pack does not apply or download resources");
            await Click(button("applyButton"), mouse);
            await Until(() => Field<bool>(example, "applied"), 10000, "Apply real resource pack");
            await Until(() => Field<int>(example, "appliedCount") == 3, 2000, "Three application-success events");
            Require(resource.GetResourceGroup().Ready && resource.UpdateCandidateCount == 0 && resource.ApplyWaitingCount == 0
                && resource.ApplyingResourcePackPath == null, "Pack completes all resources and retires updater");
            Require(server.Requests == 1, "Pack application does not issue individual resource HTTP downloads");
            Require(resource.LoadBinaryFromFileSystem(packFixture.FileSystemAssetName).SequenceEqual(File.ReadAllBytes(packFixture.FileSystemAssetName)),
                "Applied binary file-system resource matches original");
            Require(File.ReadAllBytes(Field<string>(example, "pack")).SequenceEqual(original), "Intact source pack remains unchanged");
            await Click(button("loadButton"), mouse);
            await Until(() => Field<GameObject>(example, "instance") != null, 6000, "Load Cube after resource pack application");
            Require(Field<GameObject>(example, "instance").GetComponent<Renderer>().sharedMaterial.shader.isSupported,
                "Applied Cube material supports current URP");
            await Screenshot("resource-pack-loaded");
            return;
        }
        if (name == "VerifyCachedResources")
        {
            var cacheFixture = ResourceExamplePaths.Fixture;
            Require(resource.ResourceMode == ResourceMode.Updatable && Directory.GetFiles(resource.ReadOnlyPath).Length == 0
                && File.Exists(Path.Combine(resource.ReadWritePath, "GameFrameworkList.dat")), "Nonempty writable cache seeded in isolated session");
            dropdown("damageMode").value = mode;
            if ((mode % 2 == 1) != toggle("budgetToggle").isOn) await Click(toggle("budgetToggle"), mouse);
            await Click(button("versionButton"), mouse);
            await Until(() => Field<bool>(example, "versionReady"), 10000, "Cache example remote version list");
            await Click(button("verifyButton"), mouse);
            await Until(() => Field<bool>(example, "verified"), 10000, "Verify nonempty cache entry " + mode);
            await Until(() => Field<int>(example, "valid") + Field<int>(example, "invalid") == 3, 2000, "All cache verify events");
            await Screenshot("resource-cache-counters-" + mode);
            Require(Field<int>(example, "total") == 3 && Field<int>(example, "invalid") == (mode == 0 ? 0 : 1)
                && Field<int>(example, "valid") == (mode == 0 ? 3 : 2), $"Correct healthy/corrupt resource counters entry {mode}: total={Field<int>(example, "total")}, valid={Field<int>(example, "valid")}, invalid={Field<int>(example, "invalid")}");
            Require(Field<bool>(example, "verificationPassed") == (mode == 0), "CRC/length verification result entry " + mode);
            if (mode == 1 || mode == 2)
                Require(!File.Exists(Path.Combine(resource.ReadWritePath, "example/message.bytes")), "Damaged loose resource removed before checks");
            await Screenshot("resource-cache-verified-" + mode);
            await Click(button("checkButton"), mouse);
            await Until(() => Field<bool>(example, "checkedResources"), 10000, "Check after cache verification");
            Require(resource.UpdateCandidateCount == (mode == 0 ? 0 : 1) && resource.GetResourceGroup().ReadyCount == (mode == 0 ? 3 : 2),
                "Only corrupted resource becomes a missing update candidate");
            if (mode > 0)
            {
                await Click(button("repairButton"), mouse);
                await Until(() => !Field<bool>(example, "busy") && resource.GetResourceGroup().Ready, 10000, "Download replacement after verification");
            }
            Require(Field<ResourceHttpServer>(example, "server").Requests == (mode == 0 ? 1 : 2),
                "Healthy cache avoids resource downloads; corrupted cache downloads only one resource");
            Require(resource.LoadBinaryFromFileSystem(cacheFixture.FileSystemAssetName).SequenceEqual(File.ReadAllBytes(cacheFixture.FileSystemAssetName)),
                "Container data matches source after verification/repair");
            await Click(button("readButton"), mouse);
            await Until(() => text("resultText").text.Contains("65536"), 5000, "Read repaired or healthy binary contents");
            Require(text("resultText").text.Contains("00-01-02-03-04-05-06-07")
                && text("resultText").text.Contains(System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(cacheFixture.DiskAssetName))),
                "GUI shows verified binary bytes and original UTF-8 message");
            await Screenshot("resource-cache-ready-" + mode);
            return;
        }
        if (name == "UpdateRemoteResources")
        {
            Require(!QGF.Base.EditorResourceMode && resource.ResourceMode == ResourceMode.Updatable, "Uses real Updatable backend");
            Require(ResourceExamplePaths.Fixture != null && Directory.GetFiles(resource.ReadOnlyPath).Length == 0,
                "Fresh empty read-only area, not the prebuilt Package");
            var updateFixture = ResourceExamplePaths.Fixture;
            dropdown("verifyMode").value = mode;
            if (mode == 2) await Click(toggle("ignoreVariantToggle"), mouse);
            await Click(button("versionButton"), mouse);
            await Until(() => Field<bool>(example, "versionReady"), 10000, "HTTP version list download and CRC");
            Require(resource.CheckVersionList(updateFixture.ResourceVersion) == CheckVersionListResult.Updated, "Downloaded version is current");
            await Click(button("checkButton"), mouse);
            await Until(() => Field<bool>(example, "checkedResources"), 10000, "Resource check entry " + mode);
            Require(resource.UpdateCandidateCount == 3 && resource.GetResourceGroup().ReadyCount == 0,
                "Three real resources missing from fresh writable cache");
            var allGroups = Field<IResourceGroupCollection>(example, "allGroups");
            Require(!allGroups.Ready && allGroups.ReadyCompressedLength == 0 && allGroups.TotalCompressedLength > 0,
                "Merged groups start unready with zero completed compressed bytes");
            await Until(() => text("groupsText").text.Contains("合并 Base + Data：准备中")
                && text("groupsText").text.Contains($"进度 {allGroups.Progress:P0}"), 2000,
                "Live merged group preparation status is visible before download");
            Require(mode == 0 || Field<bool>(example, "verificationPassed"), "Optional local verification entry " + mode);
            if (mode != 0)
            {
                dropdown("groupMode").value = mode == 1 ? 1 : 0;
                await Click(toggle("failureToggle"), mouse);
                int before = Field<ResourceHttpServer>(example, "server").Requests;
                await Click(button("updateButton"), mouse);
                await Until(() => !Field<bool>(example, "updating"), 20000, "Exhausted retries finish group update");
                await Until(() => text("transferText").text.Contains("1/1"), 2000, "Final failure event retry counter");
                Require(text("stateText").text.Contains("更新失败") && resource.UpdateCandidateCount == 3,
                    "Failed last task returns false and leaves resources available for retry");
                Require(Field<ResourceHttpServer>(example, "server").Requests - before == (mode == 1 ? 2 : 6),
                    "One initial attempt and one automatic retry per resource");
                await Screenshot("resource-update-failure-" + mode);
                await Click(toggle("failureToggle"), mouse);
            }
            else
            {
                await Click(button("updateButton"), mouse);
                await Until(() => QGF.Download.WorkingAgentCount > 0, 3000, "First resource download started");
                await Click(button("stopButton"), mouse);
                Require(!Field<bool>(example, "updating") && resource.UpdateWaitingCount == 0,
                    "Stop clears scheduled group without its completion callback");
                await Until(() => QGF.Download.WorkingAgentCount == 0, 10000, "In-flight file continues after stopping");
                Require(resource.GetResourceGroup().ReadyCount > 0 && resource.UpdateCandidateCount > 0,
                    "Current files finish; unstarted resources remain for the next update");
            }
            await Click(button("updateButton"), mouse);
            await Until(() => !Field<bool>(example, "updating"), 15000, "Resume/retry resource download");
            Require(resource.GetResourceGroup(mode == 1 ? "Base" : "").Ready && text("stateText").text.Contains("更新完成"),
                "Selected group ready after real HTTP download");
            if (mode == 1) Require(!resource.GetResourceGroup("Data").Ready && resource.UpdateCandidateCount == 2,
                "Base can be used before Data downloads");
            if (mode == 1)
            {
                Require(ReferenceEquals(allGroups, Field<IResourceGroupCollection>(example, "allGroups"))
                    && !allGroups.Ready && allGroups.ReadyCount == 1
                    && allGroups.ReadyCompressedLength == resource.GetResourceGroup("Base").ReadyCompressedLength,
                    "Retained group collection reflects partial Base-only readiness without rebuilding");
                await Until(() => text("groupsText").text.Contains($"合并 Base + Data：准备中，进度 {allGroups.Progress:P0}"),
                    2000, "Partial merged progress appears in UI");
            }
            await Click(button("loadButton"), mouse);
            await Until(() => Field<GameObject>(example, "instance") != null, 6000, "Cube loaded from downloaded bundle");
            Require(Field<GameObject>(example, "instance").GetComponent<Renderer>().sharedMaterial.shader.isSupported,
                "Downloaded bundle contains supported URP material");
            await Screenshot("resource-update-loaded-" + mode);
            if (mode == 1)
            {
                dropdown("groupMode").value = 2;
                await Click(button("updateButton"), mouse);
                await Until(() => !Field<bool>(example, "updating"), 15000, "Data group download");
            }
            Require(resource.GetResourceGroup().Ready && resource.UpdateCandidateCount == 0,
                "All resources ready; updater is safely retired");
            Require(allGroups.Ready && allGroups.ReadyCompressedLength == allGroups.TotalCompressedLength && allGroups.Progress == 1,
                "Merged groups become fully ready after remaining Data resources finish");
            await Until(() => text("groupsText").text.Contains("合并 Base + Data：已就绪")
                && text("groupsText").text.Contains("进度 100%"),
                2000, "Completed merged group status is visible");
            Require(resource.LoadBinaryFromFileSystem(updateFixture.FileSystemAssetName).SequenceEqual(File.ReadAllBytes(updateFixture.FileSystemAssetName)),
                "HTTP downloaded binary file system matches original bytes");
            await Until(() => !button("updateButton").interactable && !button("stopButton").interactable,
                2000, "Ready groups prevent duplicate update calls");
            return;
        }
        Require(!QGF.Base.EditorResourceMode && resource.ResourceMode == ResourceMode.Package, "Uses real Package backend");
        Require(ResourceExamplePaths.Fixture != null, "Fixture built for this Unity version and imported asset paths");
        Require(resource.ReadOnlyPath.StartsWith(ResourceExamplePaths.BuildRoot) && resource.ReadWritePath.StartsWith(ResourceExamplePaths.BuildRoot),
            "Read-only and write paths isolated under Library before agent creation");
        await Click(button("initializeButton"), mouse);
        await Until(() => Field<bool>(example, "initialized"), 10000, "Real version list initialization");
        Require(resource.AssetCount == 3 && resource.ResourceCount == 3 && !button("initializeButton").interactable,
            "Version list reports three real resources and prevents double initialization");
        var fixture = ResourceExamplePaths.Fixture;
        if (name == "LoadResourcePackage")
        {
            await Click(button("loadButton"), mouse);
            await Until(() => Field<GameObject>(example, "asset") != null, 6000, "Cube asset loads from Package");
            var asset = Field<GameObject>(example, "asset");
            var instance = Field<GameObject>(example, "instance");
            Require(asset != instance && instance != null && asset.name == "Cube",
                "Loaded prefab asset and instantiated scene object are distinct");
            Require(instance.GetComponent<Renderer>().sharedMaterial.shader.isSupported,
                "Package Cube uses a supported shader");
            await Screenshot("resource-package-loaded");
            await Click(button("releaseButton"), mouse);
            await Until(() => Field<GameObject>(example, "asset") == null && !Field<bool>(example, "releasing"),
                4000, "Instance is destroyed before its asset reference is released");
            Require(Field<GameObject>(example, "instance") == null && button("loadButton").interactable,
                "Package asset can be loaded again after release");
            Require(resource.HasAsset(fixture.PrefabAssetName) == HasAssetResult.AssetOnDisk && resource.HasAsset("Assets/DoesNotExist.prefab") == HasAssetResult.NotExist,
                "HasAsset distinguishes real and missing resources");
        }
        else if (name == "ReadBinaryResource")
        {
            byte[] original = File.ReadAllBytes(fixture.FileSystemAssetName), disk = File.ReadAllBytes(fixture.DiskAssetName);
            await Click(button("readButton"), mouse);
            byte[] segment = Field<byte[]>(example, "result");
            Require(segment.SequenceEqual(original.Skip(8).Take(16)), "File-system segment starts at the requested source offset");
            Require(text("bytesText").text.Contains("16 字节"), "Segment length is displayed");
            Require(resource.GetBinaryLength(fixture.FileSystemAssetName) == original.Length
                && resource.GetBinaryPath(fixture.FileSystemAssetName, out bool readOnly, out bool fileSystem, out _, out _)
                && readOnly && fileSystem && resource.GetBinaryPath(fixture.FileSystemAssetName) == null,
                "File-system binary reports storage metadata without exposing a direct disk path");
            Require(resource.GetBinaryLength(fixture.DiskAssetName) == disk.Length
                && resource.GetBinaryPath(fixture.DiskAssetName) != null,
                "Disk binary reports its length and direct path");
            Require(resource.HasAsset(fixture.FileSystemAssetName) == HasAssetResult.BinaryOnFileSystem
                && resource.HasAsset(fixture.DiskAssetName) == HasAssetResult.BinaryOnDisk, "Binary storage kinds reported accurately");
            await Screenshot("resource-binary");
        }
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

    private static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
        Require(condition(), message);
    }

    private static async Task Click(Selectable target, Mouse mouse)
    {
        await Until(() => target.interactable, 4000, "Enabled control " + target.name);
        Canvas.ForceUpdateCanvases();
        await Task.Delay(150);
        var rect = (RectTransform)target.transform;
        var scroll = target.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            float offset = scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y;
            if (overflow > 0) scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
        }
        bool received = false;
        UnityEngine.Events.UnityAction observed = () => received = true;
        var button = target as Button;
        var toggle = target as Toggle;
        bool oldValue = toggle != null && toggle.isOn;
        if (button != null) button.onClick.AddListener(observed);
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(150);
            if (!(button != null ? received : toggle != null && toggle.isOn != oldValue))
            {
                await Screenshot("failed-click");
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                throw new Exception("Native click " + target.name + " at " + position + "; raycast " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            }
            Require(true, "Native click " + target.name);
        }
        finally { if (button != null) button.onClick.RemoveListener(observed); }
    }

    private static async Task Screenshot(string name)
    {
        foreach (var scroll in UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }
        Canvas.ForceUpdateCanvases();
        await Task.Delay(160);
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/ResourceExamples/" + name + ".png"));
        await Task.Delay(160);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions.Add(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = enabled;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task) throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
