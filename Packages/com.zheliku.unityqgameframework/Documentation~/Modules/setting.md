# Setting

保存玩家可修改的本地偏好与进度。

```csharp
int best = QGF.Setting.GetInt("BestScore", 0);
QGF.Setting.SetInt("BestScore", best + 1);
bool saved = QGF.Setting.Save();
```

Set 方法先修改内存，Save 才持久化并返回结果；失败时内存修改仍在，可反馈给玩家后重试。默认 Helper 同步写入 persistentDataPath 的 GameFrameworkSetting.dat，沿用 GFS 版本 0 格式。少量偏好设置适用本模块；多槽位、版本迁移和大型存档应使用独立方案。初始只读参数使用 Config，表格数据使用 DataTable。

写入先在同目录完成 `.tmp` 文件并刷新到磁盘，再原子替换正式文件；`.bak` 保存上一份有效提交。读取先完整解析正式文件，失败时尝试备份，全部失败则保留当前内存数据。从备份恢复后的下一次保存不会把已损坏的正式文件覆盖到有效备份。加载失败后，退出时跳过自动保存；业务显式调用 Save 可提交恢复后的数据。

默认文件 Helper 需要文件系统支持原子替换；不支持时返回 false，不退化为先删除再写入。WebGL 等平台可在 SettingComponent 选择 PlayerPrefsSettingHelper 或项目自己的 ISettingHelper；浏览器持久化的时序由平台决定。默认 Helper 以单个实例持有文件，不提供多进程并发写入或断电事务数据库保证。

项目只需隔离文件路径时，继承 DefaultSettingHelper 并覆写 GetFilePath，无需重复实现 Load / Save；路径在初始化时确定。Serialize / Deserialize 不关闭调用方提供的流，Deserialize 的完整结果不会自动提交到 Helper。示例的 SettingExampleHelper 仅指定场景独立路径，读写流程与正式项目一致。

[运行示例与完整脚本](../../Samples~/01_Modules/09_Setting/)。

源码：[组件](../../Runtime/Unity/Setting/SettingComponent.cs)。
