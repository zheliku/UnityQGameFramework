using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class GrowingCubeItem : ObjectBase
    {
        public static GrowingCubeItem Create(GameObject target)
        {
            var item = ReferencePool.Acquire<GrowingCubeItem>();
            item.Initialize(target); // 空名称条目对应 Spawn()，命名条目应使用 Spawn(name)。
            return item;
        }

        protected override void OnSpawn()
        {
            // 创建和复用共用同一套恢复逻辑。位置由本次使用者安排。
            ((GameObject)Target).transform.rotation = Quaternion.Euler(20, 35, 0);
        }

        protected override void OnUnspawn()
        {
            var cube = (GameObject)Target;
            cube.transform.position = new Vector3(cube.transform.position.x, -2, 0);
            cube.transform.rotation = Quaternion.identity;
        }

        protected override void Release(bool isShutdown)
        {
            var cube = (GameObject)Target;
            if (cube != null) Object.Destroy(cube);
        }
    }
}
