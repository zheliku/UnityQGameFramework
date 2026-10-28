# Debugger

查看运行状态并登记业务调试页。

```csharp
QGF.Debugger.ActiveWindow = true;
QGF.Debugger.ShowFullWindow = true;
QGF.Debugger.RegisterDebuggerWindow("Game/Counter", window, 0);
QGF.Debugger.SelectDebuggerWindow("Game/Counter");
QGF.Debugger.UnregisterDebuggerWindow("Game/Counter");
```

window 实现 IDebuggerWindow。注册调用 Initialize，页签切换触发进入与离开，注销调用 Shutdown；最后的参数传给 Initialize。路径中的 / 创建页组。Debugger 读取模块状态，不代替业务生命周期管理。

[运行示例与完整脚本](../../Samples~/01_Modules/21_Debugger/)。

源码：[组件](../../Runtime/Unity/Debugger/DebuggerComponent.cs)。
