# Fsm

创建由框架统一更新的业务状态机。

```csharp
var fsm = QGF.Fsm.CreateFsm(this, new Waiting(), new Working());
fsm.Start<Waiting>();
// 状态内通过 ChangeState<Working>(owner) 切换。
QGF.Fsm.DestroyFsm(fsm);
```

状态继承 FsmState<T>，在 OnEnter、OnUpdate、OnLeave 处理阶段逻辑。T 是持有者类型，owner.Owner 是业务对象；同一类型创建多台状态机时使用名称区分。状态数据中的 Variable 由状态机回收，不重复归还。

[运行示例与完整脚本](../../Samples~/01_Modules/05_Fsm/)。

源码：[组件](../../Runtime/Unity/Fsm/FsmComponent.cs)。
