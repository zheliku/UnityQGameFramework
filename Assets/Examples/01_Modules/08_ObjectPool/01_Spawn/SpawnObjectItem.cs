using UnityEngine;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    // ObjectBase 保存池信息，Target 引用场景中的 Cube。
    // Target 的声明类型是 object，也可以登记 Image、AudioClip 或普通 C# 对象。
    /// <summary>
    /// 包装 Cube 的对象池登记条目，Target 是被管理的物体。
    /// </summary>
    public sealed class SpawnObjectItem : ObjectBase
    {
        /// <summary>
        /// 从引用池获取并初始化 Cube 的对象池条目。
        /// </summary>
        /// <param name="target">真正被池管理的 Cube。</param>
        /// <returns>尚未注册的 Cube 条目，调用者需通过对象池的 Register 登记。</returns>
        public static SpawnObjectItem Create(GameObject target)
        {
            var item = ReferencePool.Acquire<SpawnObjectItem>();
            item.Initialize(target); // 保存传入的 GameObject 引用。
            target.transform.position = new Vector3(2, 0, 0);
            return item;
        }

        /// <summary>
        /// 每次借出时调用，可恢复 Target 的状态；Register(spawned: true) 也会调用。
        /// </summary>
        protected override void OnSpawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(4, 0, 0);
            cube.transform.rotation = Quaternion.Euler(20, 35, 0);
        }

        /// <summary>
        /// 每次归还时调用，可重置或隐藏 Target；共享借用时不代表所有使用者都已归还。
        /// </summary>
        protected override void OnUnspawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(2, 0, 0);
            cube.transform.rotation = Quaternion.identity; // 归还时重置业务状态，留待复用。
        }

        /// <summary>
        /// 最终淘汰或关闭池时释放 Target，随后框架会将本包装条目归还引用池。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
        protected override void Release(bool isShutdown)
        {
            // 释放 Target 后，框架会将条目归还引用池。
            var cube = (GameObject)Target;
            if (cube != null)
            {
                Object.Destroy(cube);
            }
        }
    }
}
