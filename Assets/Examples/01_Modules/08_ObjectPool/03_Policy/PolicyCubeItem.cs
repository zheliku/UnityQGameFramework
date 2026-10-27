using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 携带名称和回收优先级的 Cube 条目。
    /// </summary>
    public sealed class PolicyCubeItem : ObjectBase
    {
        /// <summary>
        /// 从引用池获取并初始化 Cube 的对象池条目。
        /// </summary>
        /// <param name="name">条目名称。</param>
        /// <param name="cube">真正被池管理的 Cube。</param>
        /// <param name="priority">回收优先级。</param>
        /// <returns>尚未注册的 Cube 条目，调用者需通过对象池的 Register 登记。</returns>
        public static PolicyCubeItem Create(string name, GameObject cube, int priority)
        {
            var item = ReferencePool.Acquire<PolicyCubeItem>();
            item.Initialize(name, cube, priority);
            return item;
        }

        /// <summary>
        /// 最终淘汰或关闭池时释放 Target，随后框架会将本包装条目归还引用池。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
        protected override void Release(bool isShutdown)
        {
            // 普通回收遵守锁定条件；关闭整个池时仍会到这里释放 Target。
            var cube = (GameObject)Target;
            if (cube != null)
            {
                Object.Destroy(cube);
            }
        }
    }
}
