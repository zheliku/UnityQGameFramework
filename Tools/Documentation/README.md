# 示例维护

`Assets/Examples` 是编辑源目录。`Packages/com.zheliku.unityqgameframework/Samples~` 由同步脚本生成，不直接编辑。

## 场景与界面

- `BuildPointGameScene.cs` 保存点点点。
- `BuildExampleScenes.cs` 保存模块场景。
- `StyleExampleUi.cs` 和 `NormalizeUguiNames.cs` 更新 uGUI 样式与物体名。
- `ConfigureExampleLayouts.cs` 为已有场景添加横竖屏布局；`ValidateExampleLayouts.cs` 检查三种分辨率下的物体可见范围和窗口避让。
- 重建本地化场景后执行 `ConfigureLocalizationExample.Run`，保存语言配置、LocalizeText / LocalizeImage 绑定及预览启停操作；`Tools/Validation/LocalizationSampleValidation.cs` 检查真实场景交互，`LocalizationIntegrationValidation.cs` 检查失败、取消和资源所有权。

修改场景后运行 `ValidateUguiExamples.cs`、相应模块检查和 `ValidatePointGame.cs`。检查报告写入隔离工程的 `Artifacts~`。

`ValidateSampleStructure.Run` 检查三个 Sample 的场景、预制体、脚本引用和资源归属；`ValidateModuleExamples.Run` 运行当前登记的模块场景。`ValidateExampleCancellation.Run` 专门检查实体和窗口跨帧等待的销毁取消。

模块回归会按登记顺序运行全部示例；每个场景都有启动绑定检查和行为分支，未登记行为时检查器直接失败，不会静默按启动成功通过。

跨版本复核使用 `Tools/Validation/CurrentSampleCompatibility.cs`。在隔离工程复制当前三个示例和所需验证脚本后，通过 Unity CLI 的 `open --batchmode --args "-executeMethod CurrentSampleCompatibility.Run"` 启动。它检查场景与预制体引用、数据表操作、StarForce 启动、点点点流程和 UniTask；报告写入隔离工程的 `Artifacts~`，完成后自动退出。2022.3、6.0、6.3 分别使用匹配的 URP 版本；原生鼠标输入和界面截图由单独的游戏检查覆盖。

隔离工程的 Editor 目录需要上述脚本，以及 `ExampleCompatibilityChecks`、`CompatibilitySmoke`、`ValidateSampleStructure`、`ValidateModuleExamples`、`ModuleExampleActions`；同时复制当前 `ugui-layouts.json` 和 `examples-layout.json` 到相同的 Tools/Documentation 路径，并安装 Newtonsoft JSON。真实 Git 安装检查使用 `QgfGitInstallValidation~.json` 标记，要求 PackageSource 为 Git。

## Sample 与包

`Assets/Examples` 与 `Samples~` 都只有 `00_PointPointPoint`、`01_Modules`、`02_StarForce` 三个目录。`sample-groups.json` 登记同名源目录；`ugui-layouts.json` 记录保留场景的界面，`examples-layout.json` 记录模块设置。

模块示例共用一份 Common 资源和一个运行时程序集。`sync_samples.py` 直接复制三个目录，保留内容、GUID 和命名空间；`--check` 逐文件比较源与发布副本。修改场景和代码只在 Assets 中进行。

Sample 不包含点点点和 StarForce 的专用打包脚本。自动验收使用 `Tools/Validation/BuildPointGame.cs` 和 `BuildStarForce.cs`，需要打包验收时将它们复制到隔离工程的 Editor 目录；开发者从 Unity 原生 Build Profiles / Build Settings 构建 Player。

```text
python Tools/Packaging/sync_samples.py
python Tools/Packaging/sync_samples.py --check
python Tools/Packaging/check_package.py
python Tools/Documentation/check_docs.py
dotnet run --project Tools/Style/CodeStyleAudit.csproj -- .
```

Git Consumer 按单份 Sample 验证 `ImportOne`、`MoveOne`、`PlayOne` 和 `RemoveOne`。消费工程必须位于 `Artifacts~` 并带有 `QgfExampleConsumer~.json`。

导入、移动和卸载脚本后，等待 Unity 编译及程序集重载完成再运行下一步。阶段记录保存在消费工程的 `Artifacts~/ExamplesMigration/current.json`，可据此恢复被重载中断的验证。

## API 覆盖

`AuditExampleApi.Run` 导出公开成员清单，`check_example_api_coverage.py` 对照示例映射。组件转发、初始化入口和 Helper 回调单独标记；实际行为由各模块检查验证。

`check_docs.py` 检查本地链接，并将完整 C# 示例提取到 `Artifacts~/Documentation/CompleteExamples.cs`。通过 Unity CLI 的 `script run --dry_run true` 编译此文件；模块行为直接验证实际 Sample，不再拼接旧文档中的临时类型或单独维护一份示例实现。

## 现代化复核

`RunModernizationBatch` 复用 LocalizationCoreValidation、LocalizationBuildValidation、SettingPersistenceValidation、LocalizationIntegrationValidation 和 LocalizationSampleValidation。将这些脚本放入隔离工程的 Assets/Editor，使用 `unity open <project> --batchmode --args "-executeMethod RunModernizationBatch.Run"`；脚本在异步检查完成后自行退出并写入 Artifacts~/modernization-final.json。不要使用附带 `-quit` 的 `unity run`，它会在异步方法结束前开始退出。

独立 Player 使用 BuildLocalizationPlayer 与 LocalizationPlayerProbe。将前者放入隔离工程 Editor，后者、SettingPersistenceValidation 以及改名为 LocalizationPlayerChecks 的 Core 检查放在 Assets；需要 TMP Essential Resources。构建脚本仅修改隔离工程的资产和设置，输出高裁剪 Windows IL2CPP Player；运行结果写入可执行文件旁的 localization-player.json。
