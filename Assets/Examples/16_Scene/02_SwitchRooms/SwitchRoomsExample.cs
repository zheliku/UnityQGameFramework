#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SwitchRoomsExample : MonoBehaviour
    {
        [SerializeField] private string blueRoomGuid, orangeRoomGuid;
        [SerializeField] private Button switchButton;
        [SerializeField] private Text stateText;

        private string current, target, pending, phase = "准备";
        private bool ready, changing;
        private float started, elapsed;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等 Scene 完成初始化，启动时先显示蓝色房间。
                await UniTask.NextFrame(cancellation);
                switchButton.onClick.AddListener(SwitchRoom);
                ready = true;
                SwitchRoom();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private async void SwitchRoom()
        {
            if (changing) return;
            var cancellation = destroyCancellationToken;
            try
            {
                changing = true;
                started = Time.unscaledTime;
                string blue = UnityEditor.AssetDatabase.GUIDToAssetPath(blueRoomGuid);
                target = current == blue ? UnityEditor.AssetDatabase.GUIDToAssetPath(orangeRoomGuid) : blue;
                if (current != null)
                {
                    phase = "卸载旧房间";
                    // 先结束旧房间，等待过程中保留示例入口和框架所在场景。
                    QGF.Scene.UnloadScene(current, this);
                    while (QGF.Scene.SceneIsUnloading(current)) await UniTask.NextFrame(cancellation);
                    // 请求结束后还要检查结果，卸载失败时保留旧房间，不继续切换。
                    if (QGF.Scene.SceneIsLoaded(current)) { phase = "卸载失败，保留旧房间"; return; }
                    current = null;
                }
                phase = "加载新房间";
                // 旧房间已移除后才加载目标，避免两个关卡同时参与运行。
                QGF.Scene.LoadScene(target, this);
                pending = target;
                while (QGF.Scene.SceneIsLoading(target)) await UniTask.NextFrame(cancellation);
                // 请求结束后检查已加载集合，确认目标场景加载成功。
                if (QGF.Scene.SceneIsLoaded(target))
                {
                    current = target;
                    phase = "切换完成";
                }
                else phase = "加载失败，可再次尝试";
                pending = null;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
            finally
            {
                elapsed = Time.unscaledTime - started;
                changing = false;
            }
        }

        private void Update()
        {
            if (!ready) return;
            // changing 覆盖整个卸载/加载过程，防止重复提交切换。
            switchButton.interactable = !changing;
            stateText.text = $"阶段：{phase}\n当前：{(current == null ? "无" : SceneComponent.GetSceneName(current))}"
                + $"\n目标：{(target == null ? "无" : SceneComponent.GetSceneName(target))}"
                + $"\n耗时：{(changing ? Time.unscaledTime - started : elapsed):F3} 秒";
            int loadedCount = QGF.Scene.GetLoadedSceneAssetNames().Length;
            int loadingCount = QGF.Scene.GetLoadingSceneAssetNames().Length;
            int unloadingCount = QGF.Scene.GetUnloadingSceneAssetNames().Length;
            stateText.text += $"\n已加载：{loadedCount} / 加载中：{loadingCount} / 卸载中：{unloadingCount}";
        }

        private void OnDestroy()
        {
            // 切换中的等待会取消；已发出的加载还需等待完成后卸载。
            SceneExampleCleanup.Release(current).Forget();
            SceneExampleCleanup.Release(pending).Forget();
        }
    }
}
#endif
