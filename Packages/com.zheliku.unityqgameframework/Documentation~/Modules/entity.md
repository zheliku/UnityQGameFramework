# Entity

加载、显示和隐藏带 EntityLogic 的游戏对象。

```csharp
QGF.Entity.AddEntityGroup("Actors", 30f, 8, 60f, 0);
QGF.Entity.ShowEntity<PlayerLogic>(1, "Player", "Actors", playerData);
QGF.Entity.HideEntity(1);
```

1 是业务实体编号，Player 是收集器地址。ShowEntity 发起异步显示；资源就绪后在 EntityLogic.OnShow 取得 userData。OnHide 清理本次显示的数据和订阅，实例由实体池缓存，不自行 Destroy。相同资产再次显示可复用已有实例。

[运行示例与完整脚本](../../Samples~/01_Modules/13_Entity/)。

源码：[组件](../../Runtime/Unity/Entity/EntityComponent.cs)。
