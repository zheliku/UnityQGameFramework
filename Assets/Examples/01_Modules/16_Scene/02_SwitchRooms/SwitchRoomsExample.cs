using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.Scene 顺序切换房间场景。
    /// </summary>
    public sealed class SwitchRoomsExample : MonoBehaviour
    {
        private const string BlueRoomAddress = "Scene_SwitchRooms_BlueRoom";
        private const string OrangeRoomAddress = "Scene_SwitchRooms_OrangeRoom";
        [SerializeField]
        private Button switchButton;
        [SerializeField]
        private Text stateText;
        private string current, target, pending, phase = "准备";
        private bool ready, changing;
        private float started, elapsed;
        private void Start()
        {
            // 等 Scene 完成初始化，启动时先显示蓝色房间。
            switchButton.onClick.AddListener(() => SwitchRoom().Forget());
            ready = true;
            SwitchRoom().Forget();
        }

        private async UniTask SwitchRoom()
        {
            if (changing)
            {
                return;
            }

            CancellationToken cancellation = destroyCancellationToken;
            try
            {
                changing = true;
                started = Time.unscaledTime;
                string blue = BlueRoomAddress;
                target = current == blue ? OrangeRoomAddress : blue;
                if (current != null)
                {
                    phase = "卸载旧房间";
                    // UniTask 完成表示场景句柄已归还。
                    await QGF.Scene.UnloadSceneAsync(current, cancellation);
                    current = null;
                }

                phase = "加载新房间";
                // 旧房间已移除后才加载目标，避免两个关卡同时参与运行。
                pending = target;
                await QGF.Scene.LoadSceneAsync(target, cancellation);
                current = target;
                phase = "切换完成";
                pending = null;
            }
            catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                phase = "切换失败，可再次尝试";
                Log.Error(exception);
            }
            finally
            {
                elapsed = Time.unscaledTime - started;
                changing = false;
            }
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            // changing 覆盖整个卸载/加载过程，防止重复提交切换。
            switchButton.interactable = !changing;
            stateText.text = $"阶段：{phase}\n当前：{GetRoomLabel(current)}" + $"\n目标：{GetRoomLabel(target)}" + $"\n耗时：{(changing ? Time.unscaledTime - started : elapsed):F3} 秒";
            int loadedCount = QGF.Scene.GetLoadedSceneAssetNames().Length;
            int loadingCount = QGF.Scene.GetLoadingSceneAssetNames().Length;
            int unloadingCount = QGF.Scene.GetUnloadingSceneAssetNames().Length;
            stateText.text += $"\n已加载：{loadedCount} / 加载中：{loadingCount} / 卸载中：{unloadingCount}";
        }

        private string GetRoomLabel(string assetAddress)
        {
            if (assetAddress == BlueRoomAddress)
            {
                return "蓝色房间";
            }

            if (assetAddress == OrangeRoomAddress)
            {
                return "橙色房间";
            }

            return "无";
        }

        private void OnDestroy()
        {
            // 切换中的等待会取消；已发出的加载还需等待完成后卸载。
            SceneExampleCleanup.Release(current).Forget();
            SceneExampleCleanup.Release(pending).Forget();
        }
    }
}
