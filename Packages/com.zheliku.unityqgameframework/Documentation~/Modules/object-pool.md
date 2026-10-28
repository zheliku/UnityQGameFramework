# ObjectPool

管理可借用、归还和淘汰的实际对象。

```csharp
var pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<CubeItem>("Cubes");
pool.Register(CubeItem.Create(cube), false);
var item = pool.Spawn();
GameObject target = (GameObject)item.Target;
pool.Unspawn(item);
```

Target 是真正被管理的对象，可以是 GameObject、Image、AudioClip 或普通实例；ObjectBase 是登记条目。OnSpawn / OnUnspawn 控制显示与重置，Release 释放 Target。Unspawn 归还借用，通常隐藏以便复用，不等于 Destroy；销毁池或淘汰时才执行 Release。

## 池空时由工厂创建

```csharp
var item = pool.Spawn();
if (item == null)
{
    item = CubeItem.Create(Instantiate(cubePrefab));
    pool.Register(item, true); // 已借出，执行一次 OnSpawn，不再调用 Spawn。
}
```

工厂在业务侧，池本身不自动 Instantiate。单次池同一条目不能同时借出多次；多次池维护借用计数。Capacity、ExpireTime、Locked 与 Priority 控制闲置淘汰，见 03_Policy。

[运行示例与完整脚本](../../Samples~/01_Modules/08_ObjectPool/)。

源码：[组件](../../Runtime/Unity/ObjectPool/ObjectPoolComponent.cs)。
