using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using Debug = UnityEngine.Debug;

namespace Luban.Validation
{
    public static class LubanPlayerValidation
    {
        public static async void ValidateCachedGenerator()
        {
            bool success = false;
            string failure = null;
            string previous = LubanGenerator.LocalGenerator;
            const string workspace = "Assets/CachedGeneratorValidation";
            bool created = false;
            EditorApplication.LockReloadAssemblies();
            try
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataTableComponent).Assembly);
                if (package.source != UnityEditor.PackageManager.PackageSource.LocalTarball && package.source != UnityEditor.PackageManager.PackageSource.Git)
                    throw new Exception("Expected a cached UPM tarball or Git package.");
                string[] arguments = Environment.GetCommandLineArgs();
                for (int i = 0; i < arguments.Length - 1; ++i)
                    if (arguments[i] == "-qgf-luban-generator") LubanGenerator.LocalGenerator = arguments[i + 1];
                if (Directory.Exists(workspace)) throw new Exception("Temporary workspace is occupied.");
                var profile = LubanWorkspace.Create(workspace);
                created = true;
                await LubanGenerator.GenerateAsync(profile);
                var catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(workspace + "/Data/Catalog.asset");
                if (catalog.GetAssets().Count != 1 || !File.ReadAllText(workspace + "/Generated/Tables.cs").Contains(catalog.SchemaHash))
                    throw new Exception("Cached package templates produced an invalid workspace.");
                success = true;
            }
            catch (Exception exception) { failure = exception.ToString(); Debug.LogException(exception); }
            finally
            {
                if (created) AssetDatabase.DeleteAsset(workspace);
                LubanGenerator.LocalGenerator = previous;
                EditorApplication.UnlockReloadAssemblies();
            }
            Directory.CreateDirectory("Results");
            File.WriteAllText("Results/generator-cache-result.json", JsonUtility.ToJson(new Summary
            { success = success, unityVersion = Application.unityVersion, failure = failure }, true));
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static void Run()
        {
            Directory.CreateDirectory("Results");
            bool success = false;
            string failure = null;
            try
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DataTableComponent).Assembly);
                if (package.source != UnityEditor.PackageManager.PackageSource.LocalTarball && package.source != UnityEditor.PackageManager.PackageSource.Git)
                    throw new Exception("Expected a real UPM tarball or Git consumer.");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/UnityQGameFramework.prefab");
                var framework = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var procedure = new SerializedObject(framework.GetComponentInChildren<ProcedureComponent>());
                procedure.FindProperty("m_AvailableProcedureTypeNames").arraySize = 1;
                procedure.FindProperty("m_AvailableProcedureTypeNames").GetArrayElementAtIndex(0).stringValue = typeof(EmptyProcedure).FullName;
                procedure.FindProperty("m_EntranceProcedureTypeName").stringValue = typeof(EmptyProcedure).FullName;
                procedure.ApplyModifiedPropertiesWithoutUndo();
                var component = new SerializedObject(framework.GetComponentInChildren<DataTableComponent>());
                component.FindProperty("m_LubanCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>("Assets/LubanValidation/Data/Catalog.asset");
                component.ApplyModifiedPropertiesWithoutUndo();
                new GameObject("Luban probe").AddComponent<LubanRuntimeProbe>();
                EditorSceneManager.SaveScene(scene, "Assets/LubanPlayer.unity");
                foreach (string dependency in AssetDatabase.GetDependencies("Assets/LubanPlayer.unity", true))
                    if (dependency.EndsWith(".xlsx") || dependency.EndsWith("luban.conf") || dependency.EndsWith("LubanProfile.asset"))
                        throw new Exception("Editor table source was included in scene dependencies: " + dependency);
                Directory.CreateDirectory("Assets/StreamingAssets");
                AssetDatabase.Refresh();
                var manifest = BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", new[] { new AssetBundleBuild
                { assetBundleName = "luban", assetNames = new[] { "Assets/LubanValidation/Data/Catalog.asset", "Assets/LubanValidation/GameConfig.toml" } } },
                    BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                if (manifest == null) throw new Exception("Luban catalog AssetBundle build failed.");
                AssetDatabase.Refresh();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                { scenes = new[] { "Assets/LubanPlayer.unity" }, locationPathName = "Results/Player/LubanValidation.exe",
                  target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Luban Player build failed.");
                string result = Path.GetFullPath("Results/player-result.json");
                using (var process = Process.Start(new ProcessStartInfo(Path.GetFullPath(report.summary.outputPath))
                { Arguments = "-batchmode -nographics -logFile \"" + Path.GetFullPath("Results/player.log") + "\" -luban-result \"" + result + "\"",
                  UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
                {
                    if (process == null || !process.WaitForExit(45000)) throw new Exception("Luban Player validation timed out.");
                    if (process.ExitCode != 0 || !File.Exists(result) || !JsonUtility.FromJson<Summary>(File.ReadAllText(result)).success)
                        throw new Exception("Luban Player reported a failure. Inspect Results/player.log.");
                }
                success = true;
            }
            catch (Exception exception) { failure = exception.ToString(); Debug.LogException(exception); }
            File.WriteAllText("Results/editor-result.json", JsonUtility.ToJson(new Summary
            { success = success, unityVersion = Application.unityVersion, failure = failure }, true));
            EditorApplication.Exit(success ? 0 : 1);
        }
        [Serializable] private sealed class Summary { public bool success; public string unityVersion; public string failure; }
    }
}
