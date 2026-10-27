using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 记录 Cube 显示和隐藏的生命周期。
    /// </summary>
    public sealed class ShowCubeEntityLogic : EntityLogic
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
        /// 获取显示时是否收到业务数据。
        /// </summary>
        public bool HasShowData { get; private set; }

        /// <summary>
        /// 获取隐藏时是否收到业务数据。
        /// </summary>
        public bool HasHideData { get; private set; }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            InitCalls++; // 一个池实例只初始化一次；再次显示走 OnShow。
        }

        /// <summary>
        /// 显示实体并接收业务数据。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ShowCalls++;
            HasShowData = userData is Vector3;
            CachedTransform.position = userData is Vector3 position ? position : new Vector3(3, 0, 0);
            CachedTransform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 隐藏实体并清理本次使用的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnHide(bool isShutdown, object userData)
        {
            HasHideData = userData != null;
            base.OnHide(isShutdown, userData);
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
    }
}
