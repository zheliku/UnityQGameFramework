using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectWindowsView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;
        private float seconds;
        public string CloseData { get; private set; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        // 每次打开都重置本次显示状态；同一个实例可能被不同组复用。
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            seconds = 0;
            CloseData = null;
            var panel = (RectTransform)title.transform.parent;
            bool popup = UIForm.UIGroup.Name == "Popup";
            panel.anchorMin = panel.anchorMax = new Vector2(popup ? .8f : .69f, popup ? .55f : .44f);
            panel.GetComponent<Image>().color = popup ? new Color(.07f, .25f, .32f, .9f) : new Color(.03f, .09f, .13f, .85f);
        }

        // UI 模块仅在窗口可更新时回调；暂停组后这里的计时会停止。
        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            seconds += elapsed;
            title.text = $"{UIForm.UIGroup.Name} / 序号 {UIForm.SerialId}\n更新 {seconds:F1} 秒\n组深度 {UIForm.UIGroup.Depth}";
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            CloseData = userData as string;
            base.OnClose(isShutdown, userData);
        }

        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            // 组的 Depth 决定组间顺序，depth 决定同组窗口顺序。
            canvas.sortingOrder = groupDepth * 100 + depth;
        }
    }
}
