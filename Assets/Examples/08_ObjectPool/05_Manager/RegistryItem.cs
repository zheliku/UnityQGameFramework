using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples
{
    public sealed class RegistryItem : ObjectBase
    {
        public static RegistryItem Create(GameObject cube)
        {
            var item = ReferencePool.Acquire<RegistryItem>();
            item.Initialize(cube); // 每个池登记一个 Cube，随池销毁。
            return item;
        }

        protected override void Release(bool isShutdown)
        {
            var cube = (GameObject)Target;
            if (cube != null) Object.Destroy(cube);
        }
    }
}
