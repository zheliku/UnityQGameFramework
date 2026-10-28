using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 记录界面打开、关闭与层级变化。
    /// </summary>
    public sealed class OpenWindowExampleView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;

        /// <summary>
        /// 获取初始化回调次数。
        /// </summary>
        public int InitCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取打开回调次数。
        /// </summary>
        public int OpenCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取打开时是否收到业务数据。
        /// </summary>
        public bool HasOpenData
        {
            get; private set;
        }

        /// <summary>
        /// 获取关闭时传入的数据。
        /// </summary>
        public string CloseData
        {
            get; private set;
        }

        // OnInit 在实例首次创建时执行；从池中复用不会再次初始化。
        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++;
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        /// <summary>
        /// 打开界面并接收业务数据。
        /// </summary>
        /// <param name="userData">打开时传入的标题字符串；为空时使用默认标题。</param>
        protected override void OnOpen(object userData)
        {
            // 基类先恢复 Available 与 Visible；复用时也会执行 OnOpen。
            base.OnOpen(userData);
            OpenCalls++;
            HasOpenData = userData != null;
            CloseData = null;
            title.text = userData as string ?? "Default Title";
        }

        /// <summary>
        /// 关闭界面并清理本次打开的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">关闭时传入的字符串。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            CloseData = userData as string;
            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 根据界面深度调整显示顺序。
        /// </summary>
        /// <param name="groupDepth">界面组深度。</param>
        /// <param name="depth">界面在界面组中的深度。</param>
        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            // 把框架计算的组深度与组内深度同步给 uGUI Canvas。
            canvas.sortingOrder = groupDepth * 100 + depth;
        }
    }
}
