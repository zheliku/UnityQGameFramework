// Copy into a fresh Git consumer's Assets AFTER its first successful generation.
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityQGameFramework.Config;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;
#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
#endif

namespace QgfDefaultValidation
{
    public sealed class GameStartup : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            var dataTable = GameEntry.GetComponent<DataTableComponent>();
            dataTable.LoadDataTables(loader => new Tables(loader));
            if (!dataTable.HasDataTable<TbItem>()) return;
            DRItem item = dataTable.GetDataTable<TbItem>().GetDataRow(1);
            UnityEngine.Debug.Log("Loaded default Item: " + item?.Name);
        }
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class DefaultLubanPlayerValidation : MonoBehaviour
    {
        private int m_Errors;
        private void Awake() => Application.logMessageReceived += Capture;
        private void OnDestroy() => Application.logMessageReceived -= Capture;
        private void Capture(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) ++m_Errors; }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            var result = new Result { unityVersion = Application.unityVersion };
            try
            {
                var component = GameEntry.GetComponent<DataTableComponent>();
                var row = component.GetDataTable<TbItem>()?.GetDataRow(1);
                if (component.Count != 1 || row == null || string.IsNullOrEmpty(row.Name) || m_Errors != 0)
                    throw new Exception("Fresh generated default configuration failed to initialize.");
                if (!ReferenceEquals(row, component.GetDataTable<TbItem>().DataMap[1]))
                    throw new Exception("Default native/QGF records diverged.");
                result.itemName = row.Name;
                result.success = true;
            }
            catch (Exception exception) { result.failure = exception.ToString(); }
            result.errors = m_Errors;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; ++i)
                if (args[i] == "-qgf-default-result") File.WriteAllText(args[i + 1], JsonUtility.ToJson(result, true));
            Application.Quit(result.success ? 0 : 1);
        }

        [Serializable] private sealed class Result { public bool success; public string unityVersion; public string itemName; public int errors; public string failure; }
#if UNITY_EDITOR
        public static void Run()
        {
            bool success = false;
            string failure = null;
            Directory.CreateDirectory("Results");
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                new GameObject("Default configuration probe").AddComponent<DefaultLubanPlayerValidation>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/UnityQGameFramework.prefab");
                var framework = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var procedure = new SerializedObject(framework.GetComponentInChildren<ProcedureComponent>());
                procedure.FindProperty("m_AvailableProcedureTypeNames").arraySize = 1;
                procedure.FindProperty("m_AvailableProcedureTypeNames").GetArrayElementAtIndex(0).stringValue = typeof(GameStartup).FullName;
                procedure.FindProperty("m_EntranceProcedureTypeName").stringValue = typeof(GameStartup).FullName;
                procedure.ApplyModifiedPropertiesWithoutUndo();
                var component = new SerializedObject(framework.GetComponentInChildren<DataTableComponent>());
                component.FindProperty("m_LubanCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>("Assets/UnityQGameFramework/Config/Data/Catalog.asset");
                component.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, "Assets/DefaultLubanPlayer.unity");
                foreach (string dependency in AssetDatabase.GetDependencies("Assets/DefaultLubanPlayer.unity", true))
                    if (dependency.EndsWith(".xlsx") || dependency.EndsWith("luban.conf") || dependency.EndsWith("LubanProfile.asset"))
                        throw new Exception("Editor configuration leaked into Player dependencies: " + dependency);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                { scenes = new[] { "Assets/DefaultLubanPlayer.unity" }, locationPathName = "Results/DefaultPlayer/Validation.exe",
                  target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Default Player build failed.");
                string result = Path.GetFullPath("Results/default-player-result.json");
                using (var process = Process.Start(new ProcessStartInfo(Path.GetFullPath(report.summary.outputPath))
                { Arguments = "-batchmode -nographics -logFile \"" + Path.GetFullPath("Results/default-player.log") + "\" -qgf-default-result \"" + result + "\"",
                  UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
                {
                    if (process == null || !process.WaitForExit(45000)) throw new Exception("Default Player timed out.");
                    if (process.ExitCode != 0 || !File.Exists(result) || !JsonUtility.FromJson<Summary>(File.ReadAllText(result)).success)
                        throw new Exception("Default Player reported failure.");
                }
                success = true;
            }
            catch (Exception exception) { failure = exception.ToString(); UnityEngine.Debug.LogException(exception); }
            File.WriteAllText("Results/default-editor-result.json", JsonUtility.ToJson(new Summary { success = success, failure = failure }, true));
            EditorApplication.Exit(success ? 0 : 1);
        }
        [Serializable] private sealed class Summary { public bool success; public string failure; }
#endif
    }
}
