using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples.Modules
{
    // ObjectBase 保存池信息，Target 引用场景中的 Cube。
    // Target 的声明类型是 object，也可以登记 Image、AudioClip 或普通 C# 对象。
    public sealed class SpawnObjectItem : ObjectBase
    {
        public static SpawnObjectItem Create(GameObject target)
        {
            var item = ReferencePool.Acquire<SpawnObjectItem>();
            item.Initialize(target); // 保存传入的 GameObject 引用。
            target.transform.position = new Vector3(2, 0, 0);
            return item;
        }

        protected override void OnSpawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(4, 0, 0);
            cube.transform.rotation = Quaternion.Euler(20, 35, 0);
        }

        protected override void OnUnspawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(2, 0, 0);
            cube.transform.rotation = Quaternion.identity; // 归还时重置业务状态，留待复用。
        }

        protected override void Release(bool isShutdown)
        {
            // 释放 Target 后，框架会将条目归还引用池。
            var cube = (GameObject)Target;
            if (cube != null) Object.Destroy(cube);
        }
    }
}
