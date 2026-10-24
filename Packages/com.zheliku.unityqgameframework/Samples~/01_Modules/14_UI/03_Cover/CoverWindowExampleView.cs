using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class CoverWindowExampleView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;
        private string caption;
        public int CoverCalls { get; private set; }
        public int RevealCalls { get; private set; }
        public int PauseCalls { get; private set; }
        public int ResumeCalls { get; private set; }
        public int RefocusCalls { get; private set; }
        public string RefocusData { get; private set; }
        public float ActiveSeconds { get; private set; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            caption = userData as string ?? "窗口";
            ActiveSeconds = 0;
            CoverCalls = RevealCalls = PauseCalls = ResumeCalls = RefocusCalls = 0;
            RefocusData = null;
            var panel = (RectTransform)title.transform.parent;
            bool upper = caption.StartsWith("上层");
            // 错开窗口，只留窄角交叠，让两层状态都能看清。
            panel.anchorMin = panel.anchorMax = new Vector2(upper ? .83f : .63f, upper ? .60f : .40f);
            panel.GetComponent<Image>().color = upper ? new Color(.07f, .25f, .32f, .92f) : new Color(.03f, .09f, .13f, .85f);
        }

        // 覆盖只改变层级状态；是否停止更新由打开上层时的 pauseCovered 参数决定。
        protected override void OnCover()
        {
            base.OnCover();
            CoverCalls++;
        }

        // 首次显示也会触发 Reveal/Resume，计数包含这次进入可见状态。
        protected override void OnReveal()
        {
            base.OnReveal();
            RevealCalls++;
        }

        // 基类的 Pause/Resume 负责隐藏和恢复；覆盖但不暂停时，OnUpdate 仍然执行。
        protected override void OnPause()
        {
            base.OnPause();
            PauseCalls++;
        }

        protected override void OnResume()
        {
            base.OnResume();
            ResumeCalls++;
        }

        protected override void OnRefocus(object userData)
        {
            base.OnRefocus(userData);
            RefocusCalls++;
            RefocusData = userData as string;
        }

        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            ActiveSeconds += elapsed;
            title.text = $"{caption}\n更新 {ActiveSeconds:F1} 秒\n深度 {UIForm.DepthInUIGroup}";
        }

        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            canvas.sortingOrder = groupDepth * 100 + depth;
        }
    }
}
