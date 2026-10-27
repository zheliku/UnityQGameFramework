# 示例

开发源文件与 Package Sample 使用同一目录结构：00_PointPointPoint、01_Modules、02_StarForce。

模块项保留高价值的常用调用；重复的重载和相近功能不再单独复制场景。导入时保留整个 Sample 目录。

| 开发目录 | 内容 |
| --- | --- |
| [00_PointPointPoint](00_PointPointPoint/README.md) | 从启动到结算的一局小游戏 |
| [01_Modules](01_Modules/) | 按模块查看常用调用 |
| [02_StarForce](02_StarForce/README.md) | 多模块协作的射击游戏 |

每个场景单独运行。完整接口说明见包内 `Documentation~/Modules`。

## 运行前准备

导入 Sample 后等待 Unity 编译完成，打开对应场景并点击 Play。三个 Sample 都带有自己的 `ResourceSettings` 和官方 `YooAssetCollector`，默认使用 YooAsset Editor Simulation；首次运行或删除 `Library` 后，QGF 会按 Collector 自动生成模拟包。

- **点点点**：打开 `00_PointPointPoint/GameMain/Scenes/PointPointPoint.unity` 后直接运行。
- **模块示例**：打开 `01_Modules/README.md` 中对应的场景后直接运行；数据表和生成代码已随示例提供。
- **StarForce**：在 URP 工程中打开 `02_StarForce/StarForce Launcher.unity` 后运行，Game 视图建议使用 3:4。

示例运行不需要打开 YooAsset 的收集器窗口点击 `Fix` 或 `Save`。这两个按钮只用于维护 YooAsset 官方全局收集器；QGF 使用 Sample 自己的 Profile，构建时临时替换收集规则，结束后恢复原配置。

需要验证实际 Player 资源时，先停止 Play，在 YooAsset Builder 构建并复制内置资源，再使用 Unity 原生 Build Profiles 构建应用；只运行编辑器示例不需要先构建 AssetBundle。
