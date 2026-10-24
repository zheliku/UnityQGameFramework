#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class CoverWindowExample : MonoBehaviour
    {
        [SerializeField] private string windowGuid;
        [SerializeField] private Toggle pauseToggle, contextToggle;
        [SerializeField] private Button openButton, closeButton, refocusButton;
        [SerializeField] private Text stateText, callbacksText;
        private string asset;
        private int bottom, top;
        private bool ready;

        private void Start()
        {
            asset = UnityEditor.AssetDatabase.GUIDToAssetPath(windowGuid);
            QGF.UI.AddUIGroup("Example", 0);
            // 先打开底层窗口；Update 等它加载完成后才允许打开上层。
            bottom = QGF.UI.OpenUIForm(asset, "Example", "底层：背包");
            // pauseCovered 只影响同组被覆盖的窗口，不会暂停整个游戏。
            openButton.onClick.AddListener(() => top = QGF.UI.OpenUIForm(asset, "Example", pauseToggle.isOn, "上层：确认"));
            closeButton.onClick.AddListener(() => QGF.UI.CloseUIForm(top));
            refocusButton.onClick.AddListener(() =>
            {
                var form = QGF.UI.GetUIForm(bottom);
                // Refocus 将窗口移到同组顶层，保留打开序号。
                if (contextToggle.isOn) QGF.UI.RefocusUIForm(form, "再次查看背包");
                else QGF.UI.RefocusUIForm(form);
            });
            ready = true;
        }

        private void Update()
        {
            if (!ready) return;
            var lower = QGF.UI.GetUIForm(bottom);
            var upper = QGF.UI.GetUIForm(top);
            var view = lower == null ? null : (CoverWindowExampleView)lower.Logic;
            var group = QGF.UI.GetUIGroup("Example");
            stateText.text = $"组内窗口：{group.UIFormCount}    当前顶层：{group.CurrentUIForm?.SerialId}\n"
                + $"背包：{(view == null ? "加载中" : view.Visible ? "显示" : "隐藏")}    上层窗口：{(upper == null ? "未打开" : top.ToString())}";
            callbacksText.text = view == null ? "正在加载背包"
                : $"背包更新：{view.ActiveSeconds:F1} 秒\n覆盖 / 揭开：{view.CoverCalls} / {view.RevealCalls}\n"
                    + $"暂停 / 恢复：{view.PauseCalls} / {view.ResumeCalls}\n重新聚焦：{view.RefocusCalls}    数据：{view.RefocusData ?? "无"}";
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
            if (ui == null) return;
            foreach (int id in new[] { top, bottom })
                if (ui.HasUIForm(id) || ui.IsLoadingUIForm(id)) ui.CloseUIForm(id);
        }
    }
}
#endif
