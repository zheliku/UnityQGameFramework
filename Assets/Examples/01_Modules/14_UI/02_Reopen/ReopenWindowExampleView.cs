using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 记录缓存界面再次打开的生命周期。
    /// </summary>
    public sealed class ReopenWindowExampleView : UIFormLogic
    {
        [SerializeField] private TMP_Text title;
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
        /// 获取关闭回调次数。
        /// </summary>
        public int CloseCalls
        {
            get; private set;
        }

        /// <summary>
        /// 获取回收回调次数。
        /// </summary>
        public int RecycleCalls
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
            base.OnOpen(userData);
            // 回池保留这个实例；本次打开的数据仍要重新写入。
            OpenCalls++;
            title.text = userData as string ?? "My Window";
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

        /// <summary>
        /// 关闭界面并清理本次打开的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            CloseCalls++; // 结束本次打开序号，实例交给框架缓存。
        }

        /// <summary>
        /// 回收前重置本次使用的状态。
        /// </summary>
        protected override void OnRecycle()
        {
            base.OnRecycle();
            RecycleCalls++; // 回池后再次打开时执行 OnOpen，保留首次初始化结果。
        }
    }
}
