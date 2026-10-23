#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectWindows : MonoBehaviour
    {
        [SerializeField] private string windowGuid;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Toggle pauseToggle, depthToggle, contextToggle;
        [SerializeField] private Button queryButton, closeButton, restoreButton;
        [SerializeField] private Text stateText, queryText;
        private string asset;
        private int hud, popup;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                asset = UnityEditor.AssetDatabase.GUIDToAssetPath(windowGuid);
                // 两个组互不覆盖；深度影响 Canvas 排序，Pause 暂停该组所有窗口。
                QGF.UI.AddUIGroup("Hud");
                QGF.UI.AddUIGroup("Popup", 10);
                pauseToggle.onValueChanged.AddListener(value => QGF.UI.GetUIGroup("Popup").Pause = value);
                depthToggle.onValueChanged.AddListener(value => QGF.UI.GetUIGroup("Popup").Depth = value ? -1 : 10);
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "全部窗口 / 数组", "全部窗口 / List", "同资源窗口 / 数组", "同资源窗口 / List",
                    "按序号与资源查询", "全部组 / 数组", "全部组 / List", "组内窗口 / 数组", "组内窗口 / List",
                    "组内同资源 / 数组", "组内同资源 / List", "组内按序号与资源查询", "待加载序号 / 数组", "待加载序号 / List" });
                queryButton.onClick.AddListener(Query);
                closeButton.onClick.AddListener(() =>
                {
                    // 两种调用都会关闭所有已加载窗口；带参数重载把 UserData 传给各窗口的 OnClose。
                    // 关闭窗口不会移除 UI 组。
                    if (contextToggle.isOn) QGF.UI.CloseAllLoadedUIForms("退出菜单");
                    else QGF.UI.CloseAllLoadedUIForms();
                });
                restoreButton.onClick.AddListener(Restore);
                ready = true;
                Restore();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Restore()
        {
            // 同一资源可有多个打开序号；只补回当前缺少的窗口。
            if (!QGF.UI.HasUIForm(hud) && !QGF.UI.IsLoadingUIForm(hud)) hud = QGF.UI.OpenUIForm(asset, "Hud", "Hud");
            if (!QGF.UI.HasUIForm(popup) && !QGF.UI.IsLoadingUIForm(popup)) popup = QGF.UI.OpenUIForm(asset, "Popup", "Popup");
        }

        private void Query()
        {
            var forms = new List<UIForm>();
            var groups = new List<IUIGroup>();
            var members = new List<IUIForm>();
            var loading = new List<int>();
            var group = QGF.UI.GetUIGroup("Popup");
            // 数组便于一次查询；List 重载清空并填入结果，可复用已有容器。
            switch (queryMode.value)
            {
                case 0: forms.AddRange(QGF.UI.GetAllLoadedUIForms()); break;
                case 1: QGF.UI.GetAllLoadedUIForms(forms); break;
                case 2: forms.AddRange(QGF.UI.GetUIForms(asset)); break;
                case 3: QGF.UI.GetUIForms(asset, forms); break;
                case 4:
                    queryText.text = $"Hud 序号：{(QGF.UI.HasUIForm(hud) ? "已打开" : "未打开")}    "
                        + $"Hud 资源：{(QGF.UI.HasUIForm(asset) ? "已打开" : "未打开")}\n"
                        + $"按序号：{QGF.UI.GetUIForm(hud)?.SerialId}    按资源首个：{QGF.UI.GetUIForm(asset)?.SerialId}\n"
                        + $"Hud 组：{(QGF.UI.HasUIGroup("Hud") ? "存在" : "未创建")}";
                    return;
                case 5: groups.AddRange(QGF.UI.GetAllUIGroups()); break;
                case 6: QGF.UI.GetAllUIGroups(groups); break;
                case 7: members.AddRange(group.GetAllUIForms()); break;
                case 8: group.GetAllUIForms(members); break;
                case 9: members.AddRange(group.GetUIForms(asset)); break;
                case 10: group.GetUIForms(asset, members); break;
                case 11:
                    queryText.text = $"Popup 序号：{(group.HasUIForm(popup) ? "已打开" : "未打开")}    "
                        + $"Popup 资源：{(group.HasUIForm(asset) ? "已打开" : "未打开")}\n"
                        + $"按序号：{group.GetUIForm(popup)?.SerialId}    按资源首个：{group.GetUIForm(asset)?.SerialId}";
                    return;
                case 12: loading.AddRange(QGF.UI.GetAllLoadingUIFormSerialIds()); break;
                case 13: QGF.UI.GetAllLoadingUIFormSerialIds(loading); break;
            }
            queryText.text = queryMode.value >= 12 ? "待加载：" + (loading.Count == 0 ? "无" : string.Join(", ", loading))
                // Helper 是组的 Unity 适配实例；窗口仍在 OnDepthChanged 中处理 Canvas 排序。
                : groups.Count > 0 ? string.Join("\n", groups.Select(g => $"{g.Name}：{g.UIFormCount} / {g.Helper.GetType().Name}"))
                : "窗口：\n" + string.Join("\n", forms.Cast<IUIForm>().Concat(members).Select(f => $"序号 {f.SerialId} / {f.UIGroup.Name}"));
        }

        private void Update()
        {
            if (!ready) return;
            var group = QGF.UI.GetUIGroup("Popup");
            stateText.text = $"窗口组：{QGF.UI.UIGroupCount}    已打开：{QGF.UI.GetAllLoadedUIForms().Length}\n"
                + $"Popup 深度：{group.Depth}    {(group.Pause ? "已暂停" : "运行中")}\n"
                + $"按资源加载：{(QGF.UI.IsLoadingUIForm(asset) ? "加载中" : "空闲")}";
            bool pending = QGF.UI.IsLoadingUIForm(hud) || QGF.UI.IsLoadingUIForm(popup);
            restoreButton.interactable = !pending && (!QGF.UI.HasUIForm(hud) || !QGF.UI.HasUIForm(popup));
            closeButton.interactable = QGF.UI.HasUIForm(asset);
        }

        private void OnDestroy()
        {
            // 清理自己的打开请求，不用 CloseAllLoadedUIForms 影响其他窗口。
            var ui = GameEntry.GetComponent<UIComponent>();
            if (ui == null) return;
            foreach (int id in new[] { popup, hud })
                if (ui.HasUIForm(id) || ui.IsLoadingUIForm(id)) ui.CloseUIForm(id);
        }
    }
}
#endif
