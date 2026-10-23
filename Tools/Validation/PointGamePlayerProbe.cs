using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Examples.PointPointPoint;
using UnityQGameFramework.Runtime;

// 仅复制到隔离验收工程，不随 Sample 发布。通过命令行参数启用。
public sealed class PointGamePlayerProbe : MonoBehaviour
{
    private static string resultPath, run;
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly Result result = new Result();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-qgf-point-result") resultPath = args[++i];
            else if (args[i] == "-qgf-point-run") run = args[++i];
        }
        if (!string.IsNullOrEmpty(resultPath)) new GameObject("Point Player Probe").AddComponent<PointGamePlayerProbe>();
    }

    private async void Start()
    {
        Application.logMessageReceived += OnLog;
        try
        {
            await Until(() => GameEntry.GetComponent<ProcedureComponent>()?.CurrentProcedure is PointGameMenu
                && Game() != null, "Package boot and asynchronous menu form");
            object game = Game();
            Require(!QGF.Base.EditorResourceMode && QGF.Resource.ResourceMode == ResourceMode.Package, "Actual Player Package backend");
            Require(QGF.Resource.ReadOnlyPath.Replace('\\', '/').EndsWith("/StreamingAssets/" + PointGameResources.Folder), "Sample-owned resource path");
            Require((int)Get(game, "TargetCount") == 10 && (float)Get(game, "Duration") == 20, "Rules loaded from bundled TOML");
            Require(GameEntry.GetComponent<BaseComponent>().gameObject.name == "QGF", "QGF root name");
            result.packageBoot = true;
            result.savePath = PointGameSettingHelper.SavePath;
            if (run == "first")
            {
                // 本进程建立已知纪录，第二个 Player 进程检查是否从磁盘读回。
                QGF.Setting.RemoveAllSettings();
                Set(game, "BestScore", 0);
                Click("startButton");
                await Until(() => QGF.Procedure.CurrentProcedure is PointGamePlay && Game() != null, "Start first round");
                for (int i = 0; i < 10; i++)
                {
                    Click("targetButton");
                    int expected = i + 1;
                    await Until(() => (int)Get(game, "Hits") == expected, "Target hit " + expected);
                }
                await Until(() => QGF.Procedure.CurrentProcedure is PointGameResult, "Win result");
                Require((int)Get(game, "Score") == 100 && QGF.Setting.GetInt("BestScore") == 100
                    && File.Exists(result.savePath), "Score saved by result procedure");
                Require(QGF.Event.Count(PointGameClick.EventId) == 0, "Play event handler removed");
                result.win = true;
            }
            else if (run == "second")
            {
                Require((int)Get(game, "BestScore") == 100, "Record read by a new Player process");
                result.persistedAcrossProcesses = true;
                Click("startButton");
                await Until(() => QGF.Procedure.CurrentProcedure is PointGamePlay && Game() != null, "Second process round");
                for (int i = 0; i < 3; i++)
                {
                    Click("missButton");
                    int expected = 2 - i;
                    await Until(() => (int)Get(game, "Lives") == expected, "Miss " + i);
                }
                await Until(() => QGF.Procedure.CurrentProcedure is PointGameResult && Game() != null, "Miss result");
                Require((int)Get(game, "Score") == -15, "Miss penalty");
                result.miss = true;
                Click("menuButton");
                await Until(() => QGF.Procedure.CurrentProcedure is PointGameMenu && Game() != null, "Back to menu");
                Click("startButton");
                await Until(() => QGF.Procedure.CurrentProcedure is PointGamePlay && Game() != null, "Timeout round");
                Set(game, "Remaining", 0.01f);
                await Until(() => QGF.Procedure.CurrentProcedure is PointGameResult, "Timeout result");
                Require((string)Get(game, "Result") == "时间到" && QGF.Event.Count(PointGameClick.EventId) == 0,
                    "Timeout result and event cleanup");
                result.timeout = true;
            }
            else throw new Exception("Unknown Point Player probe run.");
            Require(result.errors == 0, "Player runtime errors");
            result.passed = true;
        }
        catch (Exception exception) { result.failure = exception.ToString(); }
        finally
        {
            Application.logMessageReceived -= OnLog;
            result.unity = Application.unityVersion;
            result.run = run;
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 1);
        }
    }

    private void OnLog(string message, string stack, LogType kind)
    {
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        result.errors++;
        result.lastError = message + "\n" + stack;
    }

    private static object Game()
    {
        var view = UnityEngine.Object.FindFirstObjectByType<PointGameView>();
        return view == null ? null : Get(view, "Game");
    }
    private static object Get(object value, string name) => value.GetType().GetField(name, Fields)?.GetValue(value)
        ?? value.GetType().GetProperty(name, Fields)?.GetValue(value);
    private static void Set(object value, string name, object data) => value.GetType().GetField(name, Fields).SetValue(value, data);
    private static void Click(string name) => ((Button)Get(UnityEngine.Object.FindFirstObjectByType<PointGameView>(), name)).onClick.Invoke();
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition, string message)
    {
        double start = Time.realtimeSinceStartupAsDouble;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble - start > 15) throw new TimeoutException(message);
            await Task.Delay(25);
        }
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed, packageBoot, win, miss, timeout, persistedAcrossProcesses;
        public int errors;
        public string unity, run, savePath, failure, lastError;
    }
}
