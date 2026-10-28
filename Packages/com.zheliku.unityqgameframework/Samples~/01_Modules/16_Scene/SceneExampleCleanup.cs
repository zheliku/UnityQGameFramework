using System;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 等待场景请求结束并清理示例持有的场景。
    /// </summary>
    internal static class SceneExampleCleanup
    {
        /// <summary>
        /// 等待已有加载或卸载请求，再卸载仍然存在的场景。
        /// </summary>
        /// <param name="assetName">场景定位地址。</param>
        /// <returns>场景清理任务。</returns>
        public static async UniTask Release(string assetName)
        {
            var scenes = GameEntry.GetComponent<SceneComponent>();
            if (scenes == null || string.IsNullOrEmpty(assetName))
            {
                return;
            }

            // 控制脚本已经销毁，等待应跟随 Scene 组件的生命周期。
            var cancellation = scenes.destroyCancellationToken;
            try
            {
                if (scenes.SceneIsLoading(assetName) || scenes.SceneIsUnloading(assetName))
                {
                    // 取消 UniTask 等待不会中止 Unity 的场景操作，不能重复发起卸载。
                    await UniTask.WaitUntil(() => !scenes.SceneIsLoading(assetName) && !scenes.SceneIsUnloading(assetName), cancellationToken: cancellation);
                }

                if (scenes.SceneIsLoaded(assetName))
                {
                    await scenes.UnloadSceneAsync(assetName, cancellation);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }
    }
}
