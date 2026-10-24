using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ReopenWindowExampleView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;
        public int InitCalls { get; private set; }
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public int RecycleCalls { get; private set; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++;
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            // 回池保留这个实例；本次打开的数据仍要重新写入。
            OpenCalls++;
            title.text = userData as string ?? "我的窗口";
        }

        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            canvas.sortingOrder = groupDepth * 100 + depth;
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            CloseCalls++; // 结束本次打开序号，实例交给框架缓存。
        }

        protected override void OnRecycle()
        {
            base.OnRecycle();
            RecycleCalls++; // 回池后再次打开时执行 OnOpen，保留首次初始化结果。
        }
    }
}
