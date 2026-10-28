# Event

用事件通知多个接收者，发送方无需引用接收者。

```csharp
QGF.Event.Subscribe(ScoreChanged.EventId, OnScoreChanged);
QGF.Event.Fire(this, ScoreChanged.Create(10));
// 离开当前业务生命周期时取消订阅。
QGF.Event.Unsubscribe(ScoreChanged.EventId, OnScoreChanged);
```

自定义参数继承 GameEventArgs，实现 Id 与 Clear，Create 从 ReferencePool.Acquire 取得。Fire 排队后在模块更新时派发，FireNow 在调用点派发；参数在派发结束后由事件池归还，不长期保存 args。运行中的 Subscribe 与 Unsubscribe 应成对。组件通知使用 Runtime 的事件参数，部分进度通知需在组件 Inspector 启用。

例如实体模块先发出 `Core.Events.ShowEntitySuccessEventArgs`，EntityComponent 将其转换为 Runtime 的同名游戏事件，再交给 QGF.Event。业务引入 `UnityQGameFramework.Core` 和 `UnityQGameFramework.Runtime` 即可；只有直接处理管理器通知才需引用 Core.Events。命名空间分层仍在同一个 Core 程序集中。

[运行示例与完整脚本](../../Samples~/01_Modules/04_Event/)。

源码：[组件](../../Runtime/Unity/Event/EventComponent.cs)。
