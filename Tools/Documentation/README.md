# 示例维护

`Assets/Examples` 是编辑源目录。`Packages/com.zheliku.unityqgameframework/Samples~` 由同步脚本生成，不直接编辑。

## 场景与界面

- `BuildPointGameScene.cs` 保存点点点。
- `BuildExampleScenes.cs` 保存模块场景。
- `StyleExampleUi.cs` 和 `NormalizeUguiNames.cs` 更新 uGUI 样式与物体名。
- `ConfigureExampleLayouts.cs` 为已有场景添加横竖屏布局；`ValidateExampleLayouts.cs` 检查三种分辨率下的物体可见范围和窗口避让。

修改场景后运行 `ValidateUguiExamples.cs`、相应模块检查和 `ValidatePointGame.cs`。检查报告写入隔离工程的 `Artifacts~`。

## Sample 与包

模块 Sample 各自包含一份 Common 资源，GUID 由 `sync_samples.py` 按模块生成。Sample 的锚点和目录映射在 `examples-layout.json`。

使用自适应布局的模块还会复制公共显示脚本，并生成模块专属命名空间。业务脚本直接展示模块调用。

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
