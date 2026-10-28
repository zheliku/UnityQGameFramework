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
        private const string AssetName = "UI_Reopen_Window";
        [SerializeField]
        private Slider idleSlider;
        [SerializeField]
        private Toggle lockedToggle;
        [SerializeField]
        private Button reopenButton;
        [SerializeField]
        private Text stateText, lifecycleText, policyText;
        private int serialId, previousSerial, previousInstance, openCount;
        private float remaining;
        private bool ready, changing;
        private void Start()
        {
            QGF.UI.AddUIGroup("Example", 0);
            // UI 组件管理实例池，容量用于空闲回收。
            QGF.UI.InstanceAutoReleaseInterval = .2f;
            QGF.UI.InstanceCapacity = 8;
            QGF.UI.InstanceExpireTime = 1;
            QGF.UI.InstancePriority = 0;
            reopenButton.onClick.AddListener(CloseAndReopen);
            // 返回值是打开请求的序号；窗口加载完后 GetUIForm 才能取得实例。
            serialId = QGF.UI.OpenUIForm(AssetName, "Example", $"Open #{++openCount}");
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
                serialId = QGF.UI.OpenUIForm(AssetName, "Example", $"Open #{++openCount}");
            }
            catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
            finally
            {
                changing = false;
            }
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            var form = QGF.UI.GetUIForm(serialId);
            string source = previousInstance == 0 ? "First creation" : form != null && form.gameObject.GetInstanceID() == previousInstance ? "Reusing the previous instance" : "New instance";
            stateText.text = changing ? $"Window closed; waiting {remaining:F1} s" : form == null ? "Loading" : $"Current serial: {serialId}    Previous serial: {previousSerial}\n" + $"Instance: {form.gameObject.GetInstanceID()}\nInstance source: {source}";
            var view = form == null ? null : (ReopenWindowExampleView)form.Logic;
            lifecycleText.text = view == null ? string.Empty : $"Instance OnInit: {view.InitCalls}    OnOpen：{view.OpenCalls}\n" + $"OnClose：{view.CloseCalls}    OnRecycle：{view.RecycleCalls}";
            policyText.text = $"Idle timeout: {QGF.UI.InstanceExpireTime:F1} s    Wait this time: {idleSlider.value:F1} s\nLocked instances remain cached after their idle timeout.";
            reopenButton.interactable = form != null && !changing;
            idleSlider.interactable = lockedToggle.interactable = !changing;
        }

        private void OnDestroy()
        {
            // 等待重开的 UniTask 会随脚本取消，当前打开或加载中的序号也要关闭。
            var ui = GameEntry.GetComponent<UIComponent>();
            if (ui != null && (ui.HasUIForm(serialId) || ui.IsLoadingUIForm(serialId)))
            {
                ui.CloseUIForm(serialId);
            }
        }
    }
}
