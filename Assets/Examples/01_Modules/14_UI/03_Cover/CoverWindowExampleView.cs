using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 记录界面遮挡、暂停、恢复和重新聚焦。
    /// </summary>
    public sealed class CoverWindowExampleView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;
        private string caption;

        /// <summary>
        /// 获取遮挡回调次数。
        /// </summary>
        public int CoverCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取重新显示回调次数。
        /// </summary>
        public int RevealCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取暂停回调次数。
        /// </summary>
        public int PauseCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取恢复回调次数。
        /// </summary>
        public int ResumeCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取重新聚焦回调次数。
        /// </summary>
        public int RefocusCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取重新聚焦时传入的数据。
        /// </summary>
        public string RefocusData
        {
            get; private set;
        }

        /// <summary>
        /// 获取界面未暂停时累计的更新秒数。
        /// </summary>
        public float ActiveSeconds
        {
            get; private set;
        }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        /// <summary>
        /// 打开界面并接收业务数据。
        /// </summary>
        /// <param name="userData">打开时传入的窗口标题；为空时使用默认标题。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            caption = userData as string ?? "Window";
            ActiveSeconds = 0;
            CoverCalls = RevealCalls = PauseCalls = ResumeCalls = RefocusCalls = 0;
            RefocusData = null;
            var panel = (RectTransform)title.transform.parent;
            bool upper = caption.StartsWith("Overlay");
            // 错开窗口，只留窄角交叠，让两层状态都能看清。
            panel.anchorMin = panel.anchorMax = new Vector2(upper ? .83f : .63f, upper ? .60f : .40f);
            panel.GetComponent<Image>().color = upper ? new Color(.07f, .25f, .32f, .92f) : new Color(.03f, .09f, .13f, .85f);
        }

        // 覆盖只改变层级状态；是否停止更新由打开上层时的 pauseCovered 参数决定。
        /// <summary>
        /// 处理界面被遮挡。
        /// </summary>
        protected override void OnCover()
        {
            base.OnCover();
            CoverCalls++;
        }

        // 首次显示也会触发 Reveal/Resume，计数包含这次进入可见状态。
        /// <summary>
        /// 处理界面重新显示。
        /// </summary>
        protected override void OnReveal()
        {
            base.OnReveal();
            RevealCalls++;
        }

        // 基类的 Pause/Resume 负责隐藏和恢复；覆盖但不暂停时，OnUpdate 仍然执行。
        /// <summary>
        /// 处理界面暂停。
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
            PauseCalls++;
        }

        /// <summary>
        /// 处理界面恢复。
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            ResumeCalls++;
        }

        /// <summary>
        /// 处理界面重新聚焦。
        /// </summary>
        /// <param name="userData">重新激活时传入的字符串。</param>
        protected override void OnRefocus(object userData)
        {
            base.OnRefocus(userData);
            RefocusCalls++;
            RefocusData = userData as string;
        }

        /// <summary>
        /// 更新界面状态。
        /// </summary>
        /// <param name="elapsed">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapsed">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            ActiveSeconds += elapsed;
            title.text = $"{caption}\nUpdates {ActiveSeconds:F1} s\nDepth {UIForm.DepthInUIGroup}";
        }

        /// <summary>
        /// 根据界面深度调整显示顺序。
        /// </summary>
        /// <param name="groupDepth">界面组深度。</param>
        /// <param name="depth">界面在界面组中的深度。</param>
        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            canvas.sortingOrder = groupDepth * 100 + depth;
        }
    }
}
