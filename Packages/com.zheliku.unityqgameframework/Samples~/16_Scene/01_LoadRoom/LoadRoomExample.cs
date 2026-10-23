#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadRoomExample : MonoBehaviour
    {
        [SerializeField] private string roomGuid;
        [SerializeField] private Dropdown loadMode, unloadMode, queryMode;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text stateText, roomsText;
        [SerializeField] private Button loadButton, unloadButton;

        private readonly List<string> loaded = new List<string>(), loading = new List<string>(), unloading = new List<string>();
        private string room, result = "点击加载，房间里的方块会出现在右侧。";
        private float progress, duration;
        private bool ready, ownsRoom;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // Scene 在 Start 中接入资源与事件模块，下一帧再发请求。
                await UniTask.NextFrame(cancellation);
                room = UnityEditor.AssetDatabase.GUIDToAssetPath(roomGuid);
                // 分别处理进度、成功和失败；只保存显示所需值，不持有池化事件参数。
                QGF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoaded);
                QGF.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnProgress);
                QGF.Event.Subscribe(UnloadSceneSuccessEventArgs.EventId, OnUnloaded);
                QGF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadFailed);
                QGF.Event.Subscribe(UnloadSceneFailureEventArgs.EventId, OnUnloadFailed);
                loadMode.ClearOptions();
                loadMode.AddOptions(new List<string> { "默认参数", "指定优先级", "传入上下文", "优先级与上下文" });
                unloadMode.ClearOptions();
                unloadMode.AddOptions(new List<string> { "资源路径", "路径 + 上下文" });
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "返回数组", "写入已有 List" });
                loadButton.onClick.AddListener(Load);
                unloadButton.onClick.AddListener(() =>
                {
                    // 卸载同一完整路径；正在加载时不能直接卸载。
                    if (unloadMode.value == 0) QGF.Scene.UnloadScene(room);
                    else QGF.Scene.UnloadScene(room, this);
                    result = "正在卸载";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Load()
        {
            progress = duration = 0;
            result = "正在加载";
            // 只影响加载请求的优先级；与多场景的激活顺序是两个不同设置。
            switch (loadMode.value)
            {
                case 0: QGF.Scene.LoadScene(room); break;
                case 1: QGF.Scene.LoadScene(room, 10); break;
                case 2: QGF.Scene.LoadScene(room, this); break;
                default: QGF.Scene.LoadScene(room, 10, this); break;
            }
            ownsRoom = true;
        }

        private void Update()
        {
            if (!ready) return;
            // HasScene 检查资源是否存在，SceneIsLoaded 检查本次运行是否已加载。
            bool exists = QGF.Scene.HasScene(room);
            bool present = QGF.Scene.SceneIsLoaded(room);
            bool busy = QGF.Scene.SceneIsLoading(room) || QGF.Scene.SceneIsUnloading(room);
            loadButton.interactable = exists && !present && !busy;
            unloadButton.interactable = present && !busy;
            progressSlider.SetValueWithoutNotify(progress);
            stateText.text = $"{SceneComponent.GetSceneName(room)}：{(busy ? "处理中" : present ? "已加载" : "未加载")}\n"
                + $"进度：{progress:P0}    最近加载耗时：{duration:F3} 秒\n" + result;
            // 数组方便取快照；List 重载会清空并写入结果，适合复用容器。
            if (queryMode.value == 0)
            {
                int loadedCount = QGF.Scene.GetLoadedSceneAssetNames().Length;
                int loadingCount = QGF.Scene.GetLoadingSceneAssetNames().Length;
                int unloadingCount = QGF.Scene.GetUnloadingSceneAssetNames().Length;
                roomsText.text = $"已加载 {loadedCount} / 加载中 {loadingCount} / 卸载中 {unloadingCount}";
            }
            else
            {
                QGF.Scene.GetLoadedSceneAssetNames(loaded);
                QGF.Scene.GetLoadingSceneAssetNames(loading);
                QGF.Scene.GetUnloadingSceneAssetNames(unloading);
                roomsText.text = $"已加载 {loaded.Count} / 加载中 {loading.Count} / 卸载中 {unloading.Count}";
            }
            roomsText.text += $"\nUnity 场景数：{SceneManager.sceneCount}（含示例主场景）";
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var e = (LoadSceneSuccessEventArgs)args;
            if (e.SceneAssetName != room) return;
            // 完成事件才代表成功；最后一次进度事件不保证恰好等于 1。
            progress = 1;
            duration = e.Duration;
            result = "加载成功；UserData：" + (ReferenceEquals(e.UserData, this) ? "当前对象" : "未设置");
        }

        private void OnProgress(object sender, GameEventArgs args)
        {
            // 进度来自实际加载操作，小场景可能一帧内完成。
            var e = (LoadSceneUpdateEventArgs)args;
            if (e.SceneAssetName == room) progress = e.Progress;
        }

        private void OnUnloaded(object sender, GameEventArgs args)
        {
            var e = (UnloadSceneSuccessEventArgs)args;
            if (e.SceneAssetName != room) return;
            ownsRoom = false;
            progress = 0;
            result = "卸载成功；UserData：" + (ReferenceEquals(e.UserData, this) ? "当前对象" : "未设置");
        }

        private void OnLoadFailed(object sender, GameEventArgs args)
        {
            var e = (LoadSceneFailureEventArgs)args;
            if (e.SceneAssetName == room)
            {
                ownsRoom = false;
                result = "加载失败：" + e.ErrorMessage;
            }
        }

        private void OnUnloadFailed(object sender, GameEventArgs args)
        {
            var e = (UnloadSceneFailureEventArgs)args;
            if (e.SceneAssetName == room) result = "卸载失败，请保留现有房间后重试。";
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(LoadSceneSuccessEventArgs.EventId, OnLoaded))
                    events.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoaded);
                if (events.Check(LoadSceneUpdateEventArgs.EventId, OnProgress))
                    events.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnProgress);
                if (events.Check(UnloadSceneSuccessEventArgs.EventId, OnUnloaded))
                    events.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, OnUnloaded);
                if (events.Check(LoadSceneFailureEventArgs.EventId, OnLoadFailed))
                    events.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadFailed);
                if (events.Check(UnloadSceneFailureEventArgs.EventId, OnUnloadFailed))
                    events.Unsubscribe(UnloadSceneFailureEventArgs.EventId, OnUnloadFailed);
            }
            // 不清空 Scene 管理器，只清理本控制脚本发起的房间请求。
            if (ownsRoom) SceneExampleCleanup.Release(room).Forget();
        }
    }
}
#endif
