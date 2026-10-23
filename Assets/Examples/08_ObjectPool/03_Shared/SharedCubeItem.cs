using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;

namespace UnityQGameFramework.Examples
{
    public sealed class SharedCubeItem : ObjectBase
    {
        private int users;

        public static SharedCubeItem Create(GameObject target)
        {
            var item = ReferencePool.Acquire<SharedCubeItem>();
            item.Initialize("Cube", target);
            return item;
        }

        protected override void OnSpawn()
        {
            users++;
            ((GameObject)Target).transform.rotation = Quaternion.Euler(20, 35, 0);
        }

        protected override void OnUnspawn()
        {
            // 每次归还都会回调；只有最后一个使用者归还后，才重置共享 Target。
            if (--users == 0) ((GameObject)Target).transform.rotation = Quaternion.identity;
        }

        protected override void Release(bool isShutdown)
        {
            var cube = (GameObject)Target;
            if (cube != null) Object.Destroy(cube);
        }

        public override void Clear()
        {
            base.Clear(); // 最终回收包装时清掉业务字段，不能留下旧使用者数量。
            users = 0;
        }
    }
}
