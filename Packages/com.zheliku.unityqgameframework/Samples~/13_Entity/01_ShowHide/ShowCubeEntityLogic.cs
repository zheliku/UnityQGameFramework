using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ShowCubeEntityLogic : EntityLogic
    {
        public int InitCalls { get; private set; }
        public int ShowCalls { get; private set; }
        public bool HasShowData { get; private set; }
        public bool HasHideData { get; private set; }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++; // 一个池实例只初始化一次；再次显示走 OnShow。
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ShowCalls++;
            HasShowData = userData is Vector3;
            CachedTransform.position = userData is Vector3 position ? position : new Vector3(3, 0, 0);
            CachedTransform.rotation = Quaternion.identity;
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            HasHideData = userData != null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            CachedTransform.Rotate(20f * elapsed, 50f * elapsed, 0);
        }
    }
}
