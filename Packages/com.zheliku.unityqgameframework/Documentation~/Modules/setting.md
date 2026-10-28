# Setting

保存玩家可修改的本地偏好与进度。

```csharp
int best = QGF.Setting.GetInt("BestScore", 0);
QGF.Setting.SetInt("BestScore", best + 1);
QGF.Setting.Save();
```

Set 方法先修改内存，Save 才持久化。默认 Helper 将设置保存到 persistentDataPath；自己的游戏可替换 ISettingHelper，示例使用独立位置。初始只读参数使用 Config，表格数据使用 DataTable。

[运行示例与完整脚本](../../Samples~/01_Modules/09_Setting/)。

源码：[组件](../../Runtime/Unity/Setting/SettingComponent.cs)。
