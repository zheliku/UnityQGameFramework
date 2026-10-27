using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 工厂创建的 Cube 条目，借还时调整 Target 的位置和旋转。
    /// </summary>
    public sealed class GrowingCubeItem : ObjectBase
    {
        /// <summary>
        /// 从引用池获取并初始化 Cube 的对象池条目。
        /// </summary>
        /// <param name="target">真正被池管理的 Cube。</param>
        /// <returns>尚未注册的 Cube 条目，调用者需通过对象池的 Register 登记。</returns>
        public static GrowingCubeItem Create(GameObject target)
        {
            var item = ReferencePool.Acquire<GrowingCubeItem>();
            item.Initialize(target); // 空名称条目对应 Spawn()，命名条目应使用 Spawn(name)。
            return item;
        }

        /// <summary>
        /// 每次借出时调用，可恢复 Target 的状态；Register(spawned: true) 也会调用。
        /// </summary>
        protected override void OnSpawn()
        {
            // 创建和复用共用同一套恢复逻辑。位置由本次使用者安排。
            ((GameObject)Target).transform.rotation = Quaternion.Euler(20, 35, 0);
        }

        /// <summary>
        /// 每次归还时调用，可重置或隐藏 Target；共享借用时不代表所有使用者都已归还。
        /// </summary>
        protected override void OnUnspawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(cube.transform.position.x, -2, 0);
            cube.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 最终淘汰或关闭池时释放 Target，随后框架会将本包装条目归还引用池。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
        protected override void Release(bool isShutdown)
        {
            var cube = (GameObject)Target;
            if (cube != null)
            {
                Object.Destroy(cube);
            }
        }
    }
}
