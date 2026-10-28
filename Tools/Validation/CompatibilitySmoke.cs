using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityQGameFramework.Examples.PointPointPoint;
using UnityQGameFramework.Runtime;

// Only copied into disposable compatibility projects, never installed with QGF.
public static class CompatibilitySmoke
{
    public static async void Run()
    {
        int exitCode = 1;
        string failure = null;
        bool nextFrame = false, delay = false, cancellation = false, game = false;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/00_PointPointPoint/GameMain/Scenes/PointPointPoint.unity");
            EditorApplication.isPlaying = true;
            await Wait(() => EditorApplication.isPlaying && Current() != null && Current().Available, "menu");
            Require(EventSystem.current?.currentInputModule != null, "uGUI input module");
            int frame = Time.frameCount;
            await UniTask.NextFrame();
            nextFrame = Time.frameCount > frame;
            Require(nextFrame, "UniTask.NextFrame");
            Time.timeScale = 0;
            await UniTask.Delay(50, ignoreTimeScale: true);
            delay = true;
            Time.timeScale = 1;
            var owner = new GameObject("Cancellation owner");
            CancellationToken token = owner.GetCancellationTokenOnDestroy();
            var pending = UniTask.Delay(10000, cancellationToken: token);
            UnityEngine.Object.Destroy(owner);
            try
            {
                await pending;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { cancellation = true; }
            Require(cancellation, "Destruction cancellation");
            Click("startButton");
            await Wait(() => Phase() == "Play" && Current()?.Available == true, "play");
            for (int i = 0; i < 10; i++)
            {
                Click("targetButton");
                await UniTask.NextFrame();
            }
            await Wait(() => Phase() == "Result" && Current()?.Available == true, "result");
            object state = Current().GetType().GetProperty("Game", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Current());
            game = (int)state.GetType().GetField("Score").GetValue(state) == 100;
            Require(game && QGF.Event.EventHandlerCount == 0, "Game result and event cleanup");
            Click("menuButton");
            await Wait(() => Phase() == "Menu" && Current()?.Available == true, "return to menu");
            exitCode = 0;
        }
        catch (Exception exception) { failure = exception.ToString(); }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                for (int i = 0; i < 200 && EditorApplication.isPlayingOrWillChangePlaymode; i++)
                    await Task.Delay(50);
            }
            Directory.CreateDirectory("Artifacts~");
            File.WriteAllText("Artifacts~/compatibility.json", JsonUtility.ToJson(new Result
            {
                passed = exitCode == 0,
                unity = Application.unityVersion,
                uniTaskNextFrame = nextFrame,
                unscaledDelay = delay,
                destructionCancellation = cancellation,
                pointGame = game,
                input = "Programmatic uGUI button actions; native pointer input is checked separately",
                error = failure
            }, true));
            EditorApplication.Exit(exitCode);
        }
    }

    private static PointGameView Current() => UnityEngine.Object.FindFirstObjectByType<PointGameView>();
    private static string Phase()
    {
        var view = Current();
        if (view == null || !view.Available)
            return null;
        object state = view.GetType().GetProperty("Game", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        return state?.GetType().GetField("Phase").GetValue(state) as string;
    }
    private static void Click(string name) => ((Button)Current().GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Current())).onClick.Invoke();
    private static async Task Wait(Func<bool> condition, string step)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException(step);
            await Task.Delay(20);
        }
    }
    private static void Require(bool condition, string step)
    {
        if (!condition)
            throw new Exception(step);
    }
    [Serializable]
    private class Result
    {
        public bool passed, uniTaskNextFrame, unscaledDelay, destructionCancellation, pointGame;
        public string unity, input, error;
    }
}
