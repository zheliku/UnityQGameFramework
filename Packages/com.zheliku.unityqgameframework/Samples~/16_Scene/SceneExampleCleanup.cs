using System;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    // 三个场景示例共用的退出清理：加载不能撤销，只能在完成后卸载。
    internal static class SceneExampleCleanup
    {
        public static async UniTask Release(string assetName)
        {
            var scenes = GameEntry.GetComponent<SceneComponent>();
            if (scenes == null || string.IsNullOrEmpty(assetName)) return;
            // 控制脚本已经销毁，等待应跟随 Scene 组件的生命周期。
            var cancellation = scenes.destroyCancellationToken;
            try
            {
                while (scenes.SceneIsLoading(assetName) || scenes.SceneIsUnloading(assetName))
                    await UniTask.NextFrame(cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (scenes.SceneIsLoaded(assetName)) scenes.UnloadScene(assetName);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }
    }
}
