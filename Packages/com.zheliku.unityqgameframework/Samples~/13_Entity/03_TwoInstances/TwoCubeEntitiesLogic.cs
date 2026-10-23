using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class TwoCubeEntitiesLogic : EntityLogic
    {
        protected override void OnShow(object userData)
        {
            // 每次显示（含复用）都读取本次 ShowEntity 传入的位置，清除上次的旋转状态。
            base.OnShow(userData);
            CachedTransform.position = (Vector3)userData;
            CachedTransform.rotation = Quaternion.identity;
        }
        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            CachedTransform.Rotate(20f * elapsed, 50f * elapsed, 0);
        }
    }
}
