using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.UI 加载并关闭窗口。
    /// </summary>
    public sealed class OpenWindowExample : MonoBehaviour
    {
        private const string AssetName = "UI_OpenWindow_Window";
        [SerializeField]
        private Button openButton, closeButton;
        [SerializeField]
        private Text stateText;
        private string failure;
        private int serialId;
        private void Start()
        {
            QGF.UI.AddUIGroup("Example", 0);
            QGF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, OnFailed);
            openButton.onClick.AddListener(() =>
            {
                failure = null;
                // 返回本次窗口序号；标题通过 userData 交给 UIFormLogic.OnOpen。
                serialId = QGF.UI.OpenUIForm(AssetName, "Example", "Inventory");
            });
            closeButton.onClick.AddListener(() => QGF.UI.CloseUIForm(serialId));
        }

        private void Update()
        {
            bool loading = QGF.UI.IsLoadingUIForm(serialId);
            bool opened = QGF.UI.GetUIForm(serialId) != null;
            stateText.text = failure ?? (loading ? "Loading" : opened ? "Window open" : "Window closed");
            openButton.interactable = !loading && !opened;
            closeButton.interactable = opened;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (OpenUIFormFailureEventArgs)args;
            if (e.SerialId == serialId)
            {
                failure = e.ErrorMessage;
            }
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(OpenUIFormFailureEventArgs.EventId, OnFailed))
            {
                events.Unsubscribe(OpenUIFormFailureEventArgs.EventId, OnFailed);
            }

            var ui = GameEntry.GetComponent<UIComponent>();
            // 关闭只结束本次显示；实例由 UI 模块缓存，下次打开可复用。
            if (ui != null && (ui.HasUIForm(serialId) || ui.IsLoadingUIForm(serialId)))
            {
                ui.CloseUIForm(serialId);
            }
        }
    }
}
