# UI

管理 uGUI 窗口的加载、分组与复用。

```csharp
QGF.UI.AddUIGroup("Main", 0);
int serialId = QGF.UI.OpenUIForm("Inventory", "Main", inventoryData);
QGF.UI.CloseUIForm(serialId);
```

Open 返回请求编号，不代表实例已打开。窗口继承 UIFormLogic，在 OnOpen 接收 userData，在 OnClose 取消订阅并清理本次状态；OnInit 只在首次创建调用。Close 交回缓存，清理实例由 UI 模块负责。覆盖暂停和置顶见后两个示例。

[运行示例与完整脚本](../../Samples~/01_Modules/14_UI/)。

源码：[组件](../../Runtime/Unity/UI/UIComponent.cs)。
