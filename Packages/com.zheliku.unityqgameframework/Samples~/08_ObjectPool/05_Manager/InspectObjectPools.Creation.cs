using System;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed partial class InspectObjectPools
    {
        // 对照同一组参数的四种创建入口；界面和池的清理在 InspectObjectPools.cs。
        // int 是容量或优先级，float 是秒数；完整重载可分别设置检查间隔与过期时间。
        private ObjectPoolBase CreateComparisonPool()
        {
            if (creationKind.value == 0)
            {
                switch (creationArguments.value)
                {
                    case 0: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>();
                    case 1: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare");
                    case 2: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(8);
                    case 3: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(4f);
                    case 4: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 8);
                    case 5: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 4f);
                    case 6: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(8, 4f);
                    case 7: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(8, 5);
                    case 8: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(4f, 5);
                    case 9: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 8, 4f);
                    case 10: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 8, 5);
                    case 11: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 4f, 5);
                    case 12: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>(8, 4f, 5);
                    case 13: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", 8, 4f, 5);
                    case 14: return (ObjectPoolBase)QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Compare", .25f, 8, 4f, 5);
                }
            }
            if (creationKind.value == 1)
            {
                switch (creationArguments.value)
                {
                    case 0: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem));
                    case 1: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare");
                    case 2: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 8);
                    case 3: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 4f);
                    case 4: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8);
                    case 5: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 4f);
                    case 6: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 8, 4f);
                    case 7: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 8, 5);
                    case 8: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 4f, 5);
                    case 9: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 4f);
                    case 10: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 5);
                    case 11: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 4f, 5);
                    case 12: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), 8, 4f, 5);
                    case 13: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 4f, 5);
                    case 14: return QGF.ObjectPool.CreateSingleSpawnObjectPool(typeof(PolicyCubeItem), "Compare", .25f, 8, 4f, 5);
                }
            }
            if (creationKind.value == 2)
            {
                switch (creationArguments.value)
                {
                    case 0: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>();
                    case 1: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare");
                    case 2: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(8);
                    case 3: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(4f);
                    case 4: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 8);
                    case 5: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 4f);
                    case 6: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(8, 4f);
                    case 7: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(8, 5);
                    case 8: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(4f, 5);
                    case 9: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 8, 4f);
                    case 10: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 8, 5);
                    case 11: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 4f, 5);
                    case 12: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>(8, 4f, 5);
                    case 13: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", 8, 4f, 5);
                    case 14: return (ObjectPoolBase)QGF.ObjectPool.CreateMultiSpawnObjectPool<PolicyCubeItem>("Compare", .25f, 8, 4f, 5);
                }
            }
            if (creationKind.value == 3)
            {
                switch (creationArguments.value)
                {
                    case 0: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem));
                    case 1: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare");
                    case 2: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 8);
                    case 3: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 4f);
                    case 4: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8);
                    case 5: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 4f);
                    case 6: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 8, 4f);
                    case 7: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 8, 5);
                    case 8: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 4f, 5);
                    case 9: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 4f);
                    case 10: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 5);
                    case 11: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 4f, 5);
                    case 12: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), 8, 4f, 5);
                    case 13: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", 8, 4f, 5);
                    case 14: return QGF.ObjectPool.CreateMultiSpawnObjectPool(typeof(PolicyCubeItem), "Compare", .25f, 8, 4f, 5);
                }
            }
            throw new InvalidOperationException("Unknown pool creation option.");
        }
    }
}
