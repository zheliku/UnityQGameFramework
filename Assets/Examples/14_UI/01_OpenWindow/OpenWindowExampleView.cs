using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class OpenWindowExampleView : UIFormLogic
    {
        [SerializeField] private Text title;
        private Canvas canvas;
        public int InitCalls { get; private set; }
        public int OpenCalls { get; private set; }
        public bool HasOpenData { get; private set; }
        public string CloseData { get; private set; }

        // OnInit 在实例首次创建时执行；从池中复用不会再次初始化。
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++;
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        protected override void OnOpen(object userData)
        {
            // 基类先恢复 Available 与 Visible；复用时也会执行 OnOpen。
            base.OnOpen(userData);
            OpenCalls++;
            HasOpenData = userData != null;
            CloseData = null;
            title.text = userData as string ?? "默认标题";
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            CloseData = userData as string;
            base.OnClose(isShutdown, userData);
        }

        protected override void OnDepthChanged(int groupDepth, int depth)
        {
            base.OnDepthChanged(groupDepth, depth);
            // 把框架计算的组深度与组内深度同步给 uGUI Canvas。
            canvas.sortingOrder = groupDepth * 100 + depth;
        }
    }
}
