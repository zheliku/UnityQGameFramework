using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Foundation.Validation
{
    public static class FoundationValidation
    {
        private static readonly List<string> s_Checks = new List<string>();

        public static void Run()
        {
            var result = new ValidationResult { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("O") };
            Directory.CreateDirectory("Results");
            try
            {
                Require(!EditorApplication.isPlaying, "Validation must run outside Play Mode");
                ValidateCore();
                ValidateCollectionEdgeCases();
                ValidateTypes();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/UnityQGameFramework.prefab");
                Require(prefab != null, "Framework prefab is missing");
                var framework = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                ValidatePrefab(framework);
                var procedure = new SerializedObject(framework.GetComponentInChildren<ProcedureComponent>());
                procedure.FindProperty("m_AvailableProcedureTypeNames").arraySize = 1;
                procedure.FindProperty("m_AvailableProcedureTypeNames").GetArrayElementAtIndex(0).stringValue = typeof(ValidationProcedure).FullName;
                procedure.FindProperty("m_EntranceProcedureTypeName").stringValue = typeof(ValidationProcedure).FullName;
                procedure.ApplyModifiedPropertiesWithoutUndo();
                new GameObject("Runtime validation").AddComponent<RuntimeProbe>();
                EditorSceneManager.SaveScene(scene, "Assets/FoundationValidation.unity");
                ValidateAssetBundle();
                BuildAndRunPlayer();
                result.success = true;
            }
            catch (Exception exception)
            {
                result.failure = exception.ToString();
                Debug.LogException(exception);
            }
            finally
            {
                try { FrameworkEntry.Shutdown(); }
                catch (Exception exception)
                {
                    result.success = false;
                    result.failure = (result.failure ?? string.Empty) + "\nCleanup: " + exception;
                }
                result.checks = s_Checks.ToArray();
                result.finishedUtc = DateTime.UtcNow.ToString("O");
                File.WriteAllText("Results/editor-result.json", JsonUtility.ToJson(result, true));
            }
            EditorApplication.Exit(result.success ? 0 : 1);
        }

        private static void ValidateCore()
        {
            FrameworkEntry.Shutdown();
            var core = typeof(FrameworkEntry).Assembly;
            Require(!core.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine", StringComparison.Ordinal)), "Core must remain engine-independent");
            MethodInfo getModule = typeof(FrameworkEntry).GetMethod("GetModule", BindingFlags.Public | BindingFlags.Static);
            int modules = 0;
            foreach (var contract in core.GetTypes().Where(t => t.IsInterface && t.Name.EndsWith("Manager", StringComparison.Ordinal)))
            {
                var method = getModule.MakeGenericMethod(contract);
                object first = method.Invoke(null, null);
                Require(first != null && ReferenceEquals(first, method.Invoke(null, null)), "Module singleton: " + contract.FullName);
                try
                {
                    getModule.MakeGenericMethod(first.GetType()).Invoke(null, null);
                    throw new Exception("Concrete module types must be rejected");
                }
                catch (TargetInvocationException exception)
                {
                    Require(exception.InnerException is FrameworkException, "Interface-only lookup invariant");
                }
                modules++;
            }
            Require(modules == 19, "Expected all 19 core manager contracts; got " + modules);
            s_Checks.Add("19 module singletons, interface validation and engine independence");

            var events = FrameworkEntry.GetModule<IEventManager>();
            int received = 0;
            EventHandler<GameEventArgs> handler = (sender, data) => received++;
            events.Subscribe(ProbeEvent.EventId, handler);
            events.Fire(null, ReferencePool.Acquire<ProbeEvent>());
            Require(received == 0, "Queued events must not fire immediately");
            FrameworkEntry.Update(0.1f, 0.1f);
            Require(received == 1, "Queued events must dispatch on update");
            events.FireNow(null, ReferencePool.Acquire<ProbeEvent>());
            Require(received == 2, "Immediate events must dispatch synchronously");
            events.Unsubscribe(ProbeEvent.EventId, handler);
            Require(!events.Check(ProbeEvent.EventId, handler), "Event unsubscribe");
            s_Checks.Add("Queued and immediate events, unsubscribe and reference recycling");

            var pool = FrameworkEntry.GetModule<IObjectPoolManager>().CreateSingleSpawnObjectPool<ProbeObject>("Validation");
            var pooled = ReferencePool.Acquire<ProbeObject>();
            object target = new object();
            pooled.Setup(target);
            pool.Register(pooled, false);
            Require(ReferenceEquals(pool.Spawn("probe"), pooled), "Object pool spawn");
            Require(pool.Spawn("probe") == null, "Single-spawn object cannot be borrowed twice");
            pool.Unspawn(target);
            Require(ReferenceEquals(pool.Spawn("probe"), pooled), "Returned object must be reusable");
            pool.Unspawn(target);
            s_Checks.Add("Single-spawn object pool lifecycle");

            var previous = FrameworkEntry.GetModule<IEventManager>();
            FrameworkEntry.Shutdown();
            Require(!ReferenceEquals(previous, FrameworkEntry.GetModule<IEventManager>()), "Shutdown must discard module cache");
            FrameworkEntry.Shutdown();
            s_Checks.Add("Shutdown and fresh module creation");
        }

        private static void ValidateCollectionEdgeCases()
        {
            var list = new LinkedList<string>();
            var first = list.AddLast((string)null);
            list.AddLast("present");
            var terminal = list.AddLast("outside range");
            var range = new FrameworkLinkedListRange<string>(first, terminal);
            Require(range.Contains(null), "A range must support a null element");
            Require(range.Contains("present"), "A range must find an element after null");
            Require(!range.Contains("outside range"), "The terminal node must remain excluded");
            Require(!range.Contains("absent"), "A range must reject missing elements");

            var core = typeof(FrameworkEntry).Assembly;
            var pairType = core.GetType("UnityQGameFramework.Core.TypeNamePair", true);
            var emptyPair = Activator.CreateInstance(pairType);
            Require(emptyPair.GetHashCode() == 0 && emptyPair.Equals(Activator.CreateInstance(pairType)), "Default type/name keys must support hashing and equality");
            var pair = Activator.CreateInstance(pairType, new object[] { typeof(string), "probe" });
            Require(pair.GetHashCode() == (typeof(string).GetHashCode() ^ "probe".GetHashCode()), "Type/name hashing must preserve existing valid-key behavior");

            var resourceType = core.GetType("UnityQGameFramework.Core.Resource.ResourceManager+ResourceName", true);
            var emptyResource = Activator.CreateInstance(resourceType);
            Require(emptyResource.GetHashCode() == 0 && emptyResource.Equals(Activator.CreateInstance(resourceType)), "Default resource keys must support hashing and equality");
            var resource = Activator.CreateInstance(resourceType, new object[] { "ship", null, "ab" });
            Require(resource.GetHashCode() == ("ship".GetHashCode() ^ "ab".GetHashCode()), "Resource hashing must preserve existing valid-key behavior");
            s_Checks.Add("Null-safe range lookup and default/valid value-key hashing");
        }

        private static void ValidateTypes()
        {
            Utility.Assembly.GetAssemblies();
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("FoundationLateAssembly"), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("Late").DefineType("FoundationLate.Marker", TypeAttributes.Public).CreateType();
            Require(Utility.Assembly.GetType(type.FullName) == type, "Types loaded after first lookup must be discoverable");
            var editor = Assembly.Load("UnityQGameFramework.Editor").GetType("UnityQGameFramework.Editor.Type", true);
            var method = editor.GetMethod("GetRuntimeTypeNames", BindingFlags.NonPublic | BindingFlags.Static);
            var names = (string[])method.Invoke(null, new object[] { typeof(ProcedureBase) });
            Require(names.Contains(typeof(ValidationProcedure).FullName), "Custom runtime asmdef procedures must appear in the Inspector");
            s_Checks.Add("Late assembly loading and custom asmdef type discovery");
        }

        private static void ValidatePrefab(GameObject framework)
        {
            int helpers = 0;
            foreach (Transform transform in framework.GetComponentsInChildren<Transform>(true))
            {
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing prefab script: " + transform.name);
                foreach (var component in transform.GetComponents<MonoBehaviour>())
                {
                    var property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String || !property.name.EndsWith("HelperTypeName", StringComparison.Ordinal) || string.IsNullOrEmpty(property.stringValue)) continue;
                        Require(Utility.Assembly.GetType(property.stringValue) != null, "Unresolved serialized helper: " + property.stringValue);
                        helpers++;
                    }
                }
            }
            Require(helpers >= 15, "Validate the framework's serialized helper references");
            s_Checks.Add("Prefab GUID references and " + helpers + " serialized helper types");
        }

        private static void ValidateAssetBundle()
        {
            File.WriteAllText("Assets/ValidationPayload.txt", "Unity Q Game Framework bundle roundtrip");
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Results/Bundles");
            var manifest = BuildPipeline.BuildAssetBundles("Results/Bundles", new[]
            {
                new AssetBundleBuild { assetBundleName = "foundation", assetNames = new[] { "Assets/ValidationPayload.txt" } }
            }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            Require(manifest != null, "AssetBundle build failed");
            var bundle = AssetBundle.LoadFromFile("Results/Bundles/foundation");
            Require(bundle != null && bundle.LoadAsset<TextAsset>("ValidationPayload").text == "Unity Q Game Framework bundle roundtrip", "AssetBundle content roundtrip");
            bundle.Unload(true);
            s_Checks.Add("AssetBundle build, load and unload");
        }

        private static void BuildAndRunPlayer()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FoundationValidation.unity" },
                locationPathName = "Results/Player/FoundationValidation.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            Require(report.summary.result == BuildResult.Succeeded, "Standalone Player build failed");
            string playerResult = Path.GetFullPath("Results/player-result.json");
            var process = Process.Start(new ProcessStartInfo(Path.GetFullPath(report.summary.outputPath))
            {
                Arguments = "-batchmode -nographics -logFile \"" + Path.GetFullPath("Results/player.log") + "\" -foundation-result \"" + playerResult + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            Require(process != null && process.WaitForExit(45000), "Player validation timed out");
            Require(process.ExitCode == 0 && File.Exists(playerResult), "Player validation failed; inspect Results/player.log");
            Require(JsonUtility.FromJson<PlayerSummary>(File.ReadAllText(playerResult)).success, "Player reported failure");
            s_Checks.Add("Standalone Player build and headless framework startup");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        public sealed class ProbeEvent : GameEventArgs
        {
            public const int EventId = 87654321;
            public override int Id => EventId;
            public override void Clear() { }
        }

        public sealed class ProbeObject : ObjectBase
        {
            public void Setup(object target) { Initialize("probe", target); }
            protected override void Release(bool isShutdown) { }
        }

        [Serializable]
        private sealed class PlayerSummary { public bool success; }

        [Serializable]
        private sealed class ValidationResult
        {
            public bool success;
            public string unityVersion;
            public string startedUtc;
            public string finishedUtc;
            public string[] checks;
            public string failure;
        }
    }
}
