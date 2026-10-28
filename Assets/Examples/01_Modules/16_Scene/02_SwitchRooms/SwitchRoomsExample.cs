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
        private string current, target, pending, phase = "Ready";
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
                    phase = "Unloading previous room";
                    // UniTask 完成表示场景句柄已归还。
                    await QGF.Scene.UnloadSceneAsync(current, cancellation);
                    current = null;
                }

                phase = "Loading new room";
                // 旧房间已移除后才加载目标，避免两个关卡同时参与运行。
                pending = target;
                await QGF.Scene.LoadSceneAsync(target, cancellation);
                current = target;
                phase = "Switch complete";
                pending = null;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                phase = "Switch failed. Try again.";
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
            stateText.text = $"Phase: {phase}\nCurrent: {GetRoomLabel(current)}" + $"\nTarget: {GetRoomLabel(target)}" + $"\nElapsed: {(changing ? Time.unscaledTime - started : elapsed):F3} s";
            int loadedCount = QGF.Scene.GetLoadedSceneAssetNames().Length;
            int loadingCount = QGF.Scene.GetLoadingSceneAssetNames().Length;
            int unloadingCount = QGF.Scene.GetUnloadingSceneAssetNames().Length;
            stateText.text += $"\nLoaded: {loadedCount} / Loading: {loadingCount} / Unloading: {unloadingCount}";
        }

        private string GetRoomLabel(string assetAddress)
        {
            if (assetAddress == BlueRoomAddress)
            {
                return "Blue Room";
            }

            if (assetAddress == OrangeRoomAddress)
            {
                return "Orange Room";
            }

            return "None";
        }

        private void OnDestroy()
        {
            // 切换中的等待会取消；已发出的加载还需等待完成后卸载。
            SceneExampleCleanup.Release(current).Forget();
            SceneExampleCleanup.Release(pending).Forget();
        }
    }
}
