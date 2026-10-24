# 示例维护

`Assets/Examples` 是编辑源目录。`Packages/com.zheliku.unityqgameframework/Samples~` 由同步脚本生成，不直接编辑。

## 场景与界面

- `BuildPointGameScene.cs` 保存点点点。
- `BuildExampleScenes.cs` 保存模块场景。
- `StyleExampleUi.cs` 和 `NormalizeUguiNames.cs` 更新 uGUI 样式与物体名。
- `ConfigureExampleLayouts.cs` 为已有场景添加横竖屏布局；`ValidateExampleLayouts.cs` 检查三种分辨率下的物体可见范围和窗口避让。

修改场景后运行 `ValidateUguiExamples.cs`、相应模块检查和 `ValidatePointGame.cs`。检查报告写入隔离工程的 `Artifacts~`。

`ValidateSampleStructure.Run` 检查三个 Sample 的场景、预制体、脚本引用和资源归属；`ValidateModuleExamples.Run` 运行当前登记的模块场景。`ValidateExampleCancellation.Run` 专门检查实体和窗口跨帧等待的销毁取消。

当前精简版先运行模块行为检查，再运行 Sample 导入和 StarForce 检查；未包含专门断言的场景只算启动检查。

## Sample 与包

`Assets/Examples` 与 `Samples~` 都只有 `00_PointPointPoint`、`01_Modules`、`02_StarForce` 三个目录。`sample-groups.json` 登记同名源目录；`ugui-layouts.json` 记录保留场景的界面，`examples-layout.json` 记录模块设置。

模块示例共用一份 Common 资源和一个运行时程序集。`sync_samples.py` 直接复制三个目录，保留内容、GUID 和命名空间；`--check` 逐文件比较源与发布副本。修改场景和代码只在 Assets 中进行。

```text
python Tools/Packaging/sync_samples.py
python Tools/Packaging/sync_samples.py --check
python Tools/Packaging/check_package.py
python Tools/Documentation/check_docs.py
```

Git Consumer 按单份 Sample 验证 `ImportOne`、`MoveOne`、`PlayOne` 和 `RemoveOne`。消费工程必须位于 `Artifacts~` 并带有 `QgfExampleConsumer~.json`。

导入、移动和卸载脚本后，等待 Unity 编译及程序集重载完成再运行下一步。阶段记录保存在消费工程的 `Artifacts~/ExamplesMigration/current.json`，可据此恢复被重载中断的验证。

## API 覆盖

`AuditExampleApi.Run` 导出公开成员清单，`check_example_api_coverage.py` 对照示例映射。组件转发、初始化入口和 Helper 回调单独标记；实际行为由各模块检查验证。
