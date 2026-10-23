#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadConfigAsset : MonoBehaviour
    {
        [SerializeField] private Dropdown modeDropdown;
        [SerializeField] private Button loadButton;
        [SerializeField] private Text stateText;
        [SerializeField] private Text contextText;
        [SerializeField] private Text cacheText;
        [SerializeField] private Button reserveButton, freeButton;
        private bool ready;
        [SerializeField] private TextAsset config;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);

                // 事件组件持有回调委托；离开流程或销毁对象时解除订阅。
                QGF.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
                QGF.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
                var mode = modeDropdown;
                mode.ClearOptions();
                mode.AddOptions(new System.Collections.Generic.List<string> { "默认优先级", "指定优先级", "携带上下文", "优先级和上下文" });
                mode.value = 0;
                // 读取缓冲可预留容量，并在读取后释放。
                reserveButton.onClick.AddListener(() => QGF.Config.EnsureCachedBytesSize(4096));
                freeButton.onClick.AddListener(() => QGF.Config.FreeCachedBytes());
                loadButton.onClick.AddListener(() =>
                {
                    // 只移除本次要重读的键，避免重复加载同名项。
                    QGF.Config.RemoveConfig("Level.Name");
                    stateText.text = "读取中";
                    loadButton.interactable = false;
                    string path = UnityEditor.AssetDatabase.GetAssetPath(config);
                    // EditorResourceMode 使用资产路径；发布游戏时由资源模块处理打包后的路径。
                    switch (mode.value)
                    {
                        case 0: QGF.Config.ReadData(path); break;
                        case 1: QGF.Config.ReadData(path, 10); break;
                        case 2: QGF.Config.ReadData(path, this); break;
                        case 3: QGF.Config.ReadData(path, 10, this); break;
                    }
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            // 显示资源读取缓冲的实际容量。
            if (ready) cacheText.text = $"读取缓冲：{QGF.Config.CachedBytesSize} 字节";
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var e = (LoadConfigSuccessEventArgs)args;
            if (e.ConfigAssetName != UnityEditor.AssetDatabase.GetAssetPath(config)) return;
            // 按点分键读取配置；带默认值的重载可处理尚未配置的可选项。
            stateText.text = "已读取：" + QGF.Config.GetString("Level.Name");
            contextText.text = ReferenceEquals(e.UserData, this) ? "UserData：当前对象" : "UserData：未设置";
            loadButton.interactable = true;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (LoadConfigFailureEventArgs)args;
            if (e.ConfigAssetName != UnityEditor.AssetDatabase.GetAssetPath(config)) return;
            stateText.text = "读取失败：" + e.ErrorMessage;
            loadButton.interactable = true;
        }

        private void OnDestroy()
        {
            // EventComponent 持有这些回调；销毁时解除订阅。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;
            if (events.Check(LoadConfigSuccessEventArgs.EventId, OnLoaded)) events.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            if (events.Check(LoadConfigFailureEventArgs.EventId, OnFailed)) events.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
        }
    }
}
#endif
