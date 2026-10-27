using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core.Event;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.Config 从资源包读取 TOML 配置。
    /// </summary>
    public sealed class LoadConfigAsset : MonoBehaviour
    {
        [SerializeField] private Button loadButton;
        [SerializeField] private Text stateText;
        [SerializeField] private Text contextText;
        [SerializeField] private Text cacheText;
        [SerializeField] private Button reserveButton, freeButton;
        private const string ConfigAddress = "Config_Asset_Level";

        private void Start()
        {
            // 事件组件持有回调委托；离开流程或销毁对象时解除订阅。
            QGF.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            QGF.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            // 读取缓冲可预留容量，并在读取后释放。
            reserveButton.onClick.AddListener(() => QGF.Config.EnsureCachedBytesSize(4096));
            freeButton.onClick.AddListener(() => QGF.Config.FreeCachedBytes());
            loadButton.onClick.AddListener(() =>
            {
                // 只移除本次要重读的键，避免重复加载同名项。
                QGF.Config.RemoveConfig("Level.Name");
                stateText.text = "读取中";
                loadButton.interactable = false;
                // 字符串地址来自 YooAsset 的“分组名_文件名”规则；Config 负责解析 TOML。
                QGF.Config.ReadData(ConfigAddress, this);
            });
        }

        private void Update()
        {
            cacheText.text = $"读取缓冲：{QGF.Config.CachedBytesSize} 字节";
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var e = (LoadConfigSuccessEventArgs)args;
            if (e.ConfigAssetName != ConfigAddress)
            {
                return;
            }
            stateText.text = "已读取：" + QGF.Config.GetString("Level.Name");
            contextText.text = ReferenceEquals(e.UserData, this) ? "UserData：当前对象" : "UserData：未设置";
            loadButton.interactable = true;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (LoadConfigFailureEventArgs)args;
            if (e.ConfigAssetName != ConfigAddress)
            {
                return;
            }
            stateText.text = "读取失败：" + e.ErrorMessage;
            loadButton.interactable = true;
        }

        private void OnDestroy()
        {
            // EventComponent 持有这些回调；销毁时解除订阅。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events == null)
            {
                return;
            }
            if (events.Check(LoadConfigSuccessEventArgs.EventId, OnLoaded))
            {
                events.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            }
            if (events.Check(LoadConfigFailureEventArgs.EventId, OnFailed))
            {
                events.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            }
        }
    }
}
