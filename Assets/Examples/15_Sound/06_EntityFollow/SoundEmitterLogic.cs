using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SoundEmitterLogic : EntityLogic
    {
        private float elapsed;

        protected override void OnShow(object userData)
        {
            // 基类将实例标记为可用，声音辅助器据此判断绑定是否仍有效。
            base.OnShow(userData);
            elapsed = 0;
            CachedTransform.position = new Vector3(4, 0, 0);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            // 移动实体时，框架同步更新绑定的声源位置。
            elapsed += elapseSeconds;
            CachedTransform.position = new Vector3(4 + Mathf.Sin(elapsed * 1.5f) * 3, 0, Mathf.Cos(elapsed * 1.5f) * 2);
        }
    }
}
