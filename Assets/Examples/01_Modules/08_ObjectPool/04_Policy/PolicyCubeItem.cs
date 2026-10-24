using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class PolicyCubeItem : ObjectBase
    {
        public static PolicyCubeItem Create(string name, GameObject cube, int priority)
        {
            var item = ReferencePool.Acquire<PolicyCubeItem>();
            item.Initialize(name, cube, priority);
            return item;
        }

        protected override void Release(bool isShutdown)
        {
            // 普通回收遵守锁定条件；关闭整个池时仍会到这里释放 Target。
            var cube = (GameObject)Target;
            if (cube != null) Object.Destroy(cube);
        }
    }
}
