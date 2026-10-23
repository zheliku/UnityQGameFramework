#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class OpenWindowExample : MonoBehaviour
    {
        [SerializeField] private string windowGuid;
        [SerializeField] private Dropdown openMode, closeMode;
        [SerializeField] private Button openButton, closeButton, cancelButton;
        [SerializeField] private Text stateText, lifecycleText, resultText;
        private string asset;
        private int serialId;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                asset = UnityEditor.AssetDatabase.GUIDToAssetPath(windowGuid);
                // 先注册窗口组，再向这个组打开窗口。
                QGF.UI.AddUIGroup("Example", 0);
                // 失败事件与关闭结果都由事件模块转发；销毁时解除订阅。
                QGF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, OnFailed);
                QGF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnClosed);
                openMode.ClearOptions();
                openMode.AddOptions(new List<string> { "默认参数", "优先级", "暂停下层", "传入标题",
                    "优先级 + 暂停下层", "优先级 + 标题", "暂停下层 + 标题", "全部参数" });
                closeMode.ClearOptions();
                closeMode.AddOptions(new List<string> { "按序号", "按序号 + 数据", "按窗口", "按窗口 + 数据" });
                openButton.onClick.AddListener(Open);
                closeButton.onClick.AddListener(() =>
                {
                    var form = QGF.UI.GetUIForm(serialId);
                    // 关闭移除本次序号并回收实例；userData 传给 OnClose 和关闭事件。
                    switch (closeMode.value)
                    {
                        case 0: QGF.UI.CloseUIForm(serialId); break;
                        case 1: QGF.UI.CloseUIForm(serialId, "保存背包"); break;
                        case 2: QGF.UI.CloseUIForm(form); break;
                        case 3: QGF.UI.CloseUIForm(form, "保存背包"); break;
                    }
                });
                // 取消待打开请求；加载结束后也不会再显示这些窗口。
                cancelButton.onClick.AddListener(() => QGF.UI.CloseAllLoadingUIForms());
                ready = true;
                Open();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Open()
        {
            resultText.text = string.Empty;
            // 返回的是本次打开的序号，异步加载期间 GetUIForm 仍为 null。
            switch (openMode.value)
            {
                case 0: serialId = QGF.UI.OpenUIForm(asset, "Example"); break;
                case 1: serialId = QGF.UI.OpenUIForm(asset, "Example", 10); break;
                case 2: serialId = QGF.UI.OpenUIForm(asset, "Example", true); break;
                case 3: serialId = QGF.UI.OpenUIForm(asset, "Example", "背包窗口"); break;
                case 4: serialId = QGF.UI.OpenUIForm(asset, "Example", 10, true); break;
                case 5: serialId = QGF.UI.OpenUIForm(asset, "Example", 10, "背包窗口"); break;
                case 6: serialId = QGF.UI.OpenUIForm(asset, "Example", true, "背包窗口"); break;
                case 7: serialId = QGF.UI.OpenUIForm(asset, "Example", 10, true, "背包窗口"); break;
            }
        }

        private void Update()
        {
            if (!ready) return;
            bool loading = QGF.UI.IsLoadingUIForm(serialId);
            var form = QGF.UI.GetUIForm(serialId);
            bool valid = QGF.UI.IsValidUIForm(form); // 关闭后，即使仍持有旧实例引用，本次窗口也已无效。
            stateText.text = $"打开序号：{serialId}\n" + (loading ? "正在加载，可以取消" : form == null ? "窗口未打开" : "窗口已显示");
            var view = form == null ? null : (OpenWindowExampleView)form.Logic;
            lifecycleText.text = !valid ? string.Empty
                : $"本实例 OnInit：{view.InitCalls}    OnOpen：{view.OpenCalls}\n"
                    + $"标题数据：{(view.HasOpenData ? "已传入" : "未传入")}    "
                    + $"下层窗口：{(form.PauseCoveredUIForm ? "已暂停" : "继续运行")}";
            openButton.interactable = !loading && form == null;
            closeButton.interactable = form != null;
            cancelButton.interactable = loading;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (OpenUIFormFailureEventArgs)args;
            if (e.SerialId == serialId) resultText.text = "打开失败：" + e.ErrorMessage;
        }

        private void OnClosed(object sender, GameEventArgs args)
        {
            var e = (CloseUIFormCompleteEventArgs)args;
            if (e.SerialId == serialId) resultText.text = "已关闭，关闭数据：" + (e.UserData as string ?? "无");
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(OpenUIFormFailureEventArgs.EventId, OnFailed))
                    events.Unsubscribe(OpenUIFormFailureEventArgs.EventId, OnFailed);
                if (events.Check(CloseUIFormCompleteEventArgs.EventId, OnClosed))
                    events.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnClosed);
            }
            // 按自己的序号关闭；CloseUIForm 也能取消仍在加载的窗口。
            var ui = GameEntry.GetComponent<UIComponent>();
            if (ui != null && (ui.HasUIForm(serialId) || ui.IsLoadingUIForm(serialId))) ui.CloseUIForm(serialId);
        }
    }
}
#endif
