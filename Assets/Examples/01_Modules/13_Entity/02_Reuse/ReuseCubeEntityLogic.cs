using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ReuseCubeEntityLogic : EntityLogic
    {
        public int InitCalls { get; private set; }
        public int ShowCalls { get; private set; }
        public int HideCalls { get; private set; }
        public int RecycleCalls { get; private set; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++;
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ShowCalls++;
            // 实例可能来自缓存，每次显示都重置本次位置与旋转。
            CachedTransform.position = (Vector3)userData;
            CachedTransform.rotation = Quaternion.identity;
        }

        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            CachedTransform.Rotate(20f * elapsed, 50f * elapsed, 0);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            HideCalls++; // 本次显示结束，实例仍可能留在缓存中。
        }

        protected override void OnRecycle()
        {
            base.OnRecycle();
            RecycleCalls++; // 回池后才能再次借出；复用时不会重新执行 OnInit。
        }
    }
}
