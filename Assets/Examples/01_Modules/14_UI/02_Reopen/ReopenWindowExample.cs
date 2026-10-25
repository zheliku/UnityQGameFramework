using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示 QGF.UI 关闭后复用窗口实例。
    /// </summary>
    public sealed class ReopenWindowExample : MonoBehaviour
    {
        [SerializeField] private string windowGuid;
        [SerializeField] private Slider idleSlider;
        [SerializeField] private Toggle lockedToggle;
        [SerializeField] private Button reopenButton;
        [SerializeField] private Text stateText, lifecycleText, policyText;
        private string asset;
        private int serialId, previousSerial, previousInstance, openCount;
        private float remaining;
        private bool ready, changing;

        private void Start()
        {
            asset = windowGuid;
            QGF.UI.AddUIGroup("Example", 0);
            // UI 组件管理实例池，容量用于空闲回收。
            QGF.UI.InstanceAutoReleaseInterval = .2f;
            QGF.UI.InstanceCapacity = 8;
            QGF.UI.InstanceExpireTime = 1;
            QGF.UI.InstancePriority = 0;
            reopenButton.onClick.AddListener(CloseAndReopen);
            // 返回值是打开请求的序号；窗口加载完后 GetUIForm 才能取得实例。
            serialId = QGF.UI.OpenUIForm(asset, "Example", $"第 {++openCount} 次打开");
            ready = true;
        }

        private async void CloseAndReopen()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                changing = true;
                var form = QGF.UI.GetUIForm(serialId);
                previousSerial = serialId;
                previousInstance = form.gameObject.GetInstanceID();
                // 锁定保留的是实例缓存；关闭仍会结束本次序号。
                QGF.UI.SetUIFormInstanceLocked(form, lockedToggle.isOn);
                QGF.UI.SetUIFormInstancePriority(form, 10);
                QGF.UI.CloseUIForm(form);
                // 帧末等待回池完成，再比较立即重开与空闲过期。
                await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, cancellation);
                float end = Time.realtimeSinceStartup + idleSlider.value;
                while (Time.realtimeSinceStartup < end)
                {
                    remaining = end - Time.realtimeSinceStartup;
                    await UniTask.NextFrame(cancellation);
                }
                remaining = 0;
                // 每次打开都会产生新序号；是否复用旧 GameObject 由实例池的缓存状态决定。
                serialId = QGF.UI.OpenUIForm(asset, "Example", $"第 {++openCount} 次打开");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
            finally { changing = false; }
        }

        private void Update()
        {
            if (!ready) return;
            var form = QGF.UI.GetUIForm(serialId);
            string source = previousInstance == 0 ? "首次创建"
                : form != null && form.gameObject.GetInstanceID() == previousInstance ? "复用上次实例" : "新建实例";
            stateText.text = changing ? $"窗口已关闭，等待 {remaining:F1} 秒"
                : form == null ? "正在加载" : $"本次序号：{serialId}    上次序号：{previousSerial}\n"
                    + $"实例：{form.gameObject.GetInstanceID()}\n本次来源：{source}";
            var view = form == null ? null : (ReopenWindowExampleView)form.Logic;
            lifecycleText.text = view == null
                ? string.Empty
                : $"本实例 OnInit：{view.InitCalls}    OnOpen：{view.OpenCalls}\n"
                    + $"OnClose：{view.CloseCalls}    OnRecycle：{view.RecycleCalls}";
            policyText.text = $"空闲过期：{QGF.UI.InstanceExpireTime:F1} 秒    本次等待：{idleSlider.value:F1} 秒\n锁定后，过期也保留缓存实例";
            reopenButton.interactable = form != null && !changing;
            idleSlider.interactable = lockedToggle.interactable = !changing;
        }

        private void OnDestroy()
        {
            // 等待重开的 UniTask 会随脚本取消，当前打开或加载中的序号也要关闭。
            var ui = GameEntry.GetComponent<UIComponent>();
            if (ui != null && (ui.HasUIForm(serialId) || ui.IsLoadingUIForm(serialId))) ui.CloseUIForm(serialId);
        }
    }
}
