# Base / Utility

控制游戏时间，使用日志、文本、转换与随机工具。

```csharp
QGF.Base.GameSpeed = 0.5f;
Log.Info("Game speed: {0}", QGF.Base.GameSpeed);
string path = Utility.Path.GetRegularPath("Assets\\Game\\Cube.prefab");
```

BaseComponent 驱动全部 Core 模块。Log 只格式化 QGF 调用，不接管 Unity 全局日志；Utility 为纯 C# 工具，平台 Helper 由 Base 配置。

[运行示例与完整脚本](../../Samples~/01_Modules/01_Base/)。

源码：[组件](../../Runtime/Unity/Base/BaseComponent.cs)。
