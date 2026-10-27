using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 记录实体隐藏、回收和再次显示的过程。
    /// </summary>
    public sealed class ReuseCubeEntityLogic : EntityLogic
    {
        /// <summary>
        /// 获取初始化回调次数。
        /// </summary>
        public int InitCalls { get; private set; }

        /// <summary>
        /// 获取显示回调次数。
        /// </summary>
        public int ShowCalls { get; private set; }

        /// <summary>
        /// 获取隐藏回调次数。
        /// </summary>
        public int HideCalls { get; private set; }

        /// <summary>
        /// 获取回收回调次数。
        /// </summary>
        public int RecycleCalls { get; private set; }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++;
        }

        /// <summary>
        /// 显示实体并接收业务数据。
        /// </summary>
        /// <param name="userData">显示时传入的 Vector3 位置。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ShowCalls++;
            // 实例可能来自缓存，每次显示都重置本次位置与旋转。
            CachedTransform.position = (Vector3)userData;
            CachedTransform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 更新实体状态。
        /// </summary>
        /// <param name="elapsed">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapsed">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            CachedTransform.Rotate(20f * elapsed, 50f * elapsed, 0);
        }

        /// <summary>
        /// 隐藏实体并清理本次使用的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            HideCalls++; // 本次显示结束，实例仍可能留在缓存中。
        }

        /// <summary>
        /// 回收前重置本次使用的状态。
        /// </summary>
        protected override void OnRecycle()
        {
            base.OnRecycle();
            RecycleCalls++; // 回池后才能再次借出；复用时不会重新执行 OnInit。
        }
    }
}
