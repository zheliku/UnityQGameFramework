# Procedure

用启动、菜单、加载与战斗流程组织整个游戏。

```csharp
// 当前流程是 Inspector 中登记的 ProcedureBase 实例。
var current = QGF.Procedure.CurrentProcedure;
float elapsed = QGF.Procedure.CurrentProcedureTime;
// 在 ProcedureBase 内调用 ChangeState<Battle>(owner)。
```

流程继承 ProcedureBase，使用 IFsm<IProcedureManager>。在 Procedure Inspector 勾选可用流程并选择入口，组件完成启动后自动进入；不需要业务再次 Initialize。OnEnter 订阅、OnLeave 取消；在 OnUpdate 判断异步准备完成后切换，避免回调与离开时序交叉。

[运行示例与完整脚本](../../Samples~/01_Modules/06_Procedure/)。

源码：[组件](../../Runtime/Unity/Procedure/ProcedureComponent.cs)。
