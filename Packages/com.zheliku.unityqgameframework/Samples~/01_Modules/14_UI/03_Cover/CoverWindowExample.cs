using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示 QGF.UI 窗口覆盖和重新聚焦。
    /// </summary>
    public sealed class CoverWindowExample : MonoBehaviour
    {
        private const string AssetName = "UI_Cover_Window";
        [SerializeField]
        private Toggle pauseToggle, contextToggle;
        [SerializeField]
        private Button openButton, closeButton, refocusButton;
        [SerializeField]
        private Text stateText, callbacksText;
        private int bottom, top;
        private bool ready;
        private void Start()
        {
            QGF.UI.AddUIGroup("Example", 0);
            // 先打开底层窗口；Update 等它加载完成后才允许打开上层。
            bottom = QGF.UI.OpenUIForm(AssetName, "Example", "Base: Inventory");
            // pauseCovered 只影响同组被覆盖的窗口，不会暂停整个游戏。
            openButton.onClick.AddListener(() => top = QGF.UI.OpenUIForm(AssetName, "Example", pauseToggle.isOn, "Overlay: Confirm"));
            closeButton.onClick.AddListener(() => QGF.UI.CloseUIForm(top));
            refocusButton.onClick.AddListener(() =>
            {
                var form = QGF.UI.GetUIForm(bottom);
                // Refocus 将窗口移到同组顶层，保留打开序号。
                if (contextToggle.isOn)
                {
                    QGF.UI.RefocusUIForm(form, "View inventory again");
                }
                else
                {
                    QGF.UI.RefocusUIForm(form);
                }
            });
            ready = true;
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            var lower = QGF.UI.GetUIForm(bottom);
            var upper = QGF.UI.GetUIForm(top);
            var view = lower == null ? null : (CoverWindowExampleView)lower.Logic;
            var group = QGF.UI.GetUIGroup("Example");
            stateText.text = $"Forms in group: {group.UIFormCount}    Top form: {group.CurrentUIForm?.SerialId}\n" + $"Inventory: {(view == null ? "Loading" : view.Visible ? "Shown" : "Hidden")}    Overlay: {(upper == null ? "Not open" : top.ToString())}";
            callbacksText.text = view == null ? "Loading inventory" : $"Inventory updates: {view.ActiveSeconds:F1} s\nCover / Reveal: {view.CoverCalls} / {view.RevealCalls}\n" + $"Pause / Resume: {view.PauseCalls} / {view.ResumeCalls}\nRefocus: {view.RefocusCalls}    Data: {view.RefocusData ?? "None"}";
            bool pending = QGF.UI.IsLoadingUIForm(top);
            openButton.interactable = lower != null && upper == null && !pending;
            closeButton.interactable = upper != null;
            refocusButton.interactable = lower != null && upper != null && group.CurrentUIForm.SerialId != bottom;
            pauseToggle.interactable = upper == null && !pending;
        }

        private void OnDestroy()
        {
            // 只关闭本控制脚本创建的两个序号，保留组内其他窗口。
            var ui = GameEntry.GetComponent<UIComponent>();
            if (ui == null)
            {
                return;
            }

            foreach (int id in new[]
            {
                top,
                bottom
            }

            )
            {
                if (ui.HasUIForm(id) || ui.IsLoadingUIForm(id))
                {
                    ui.CloseUIForm(id);
                }
            }
        }
    }
}
