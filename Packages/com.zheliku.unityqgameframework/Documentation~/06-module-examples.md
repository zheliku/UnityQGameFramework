# 运行示例

Package Manager 提供三个独立 Sample。完整仓库的开发目录是 `Assets/Examples`，不要在仓库里再次导入同一 Sample。

| Sample | 启动方式 | 阅读入口 |
| --- | --- | --- |
| 00 · PointPointPoint | 打开 PointPointPoint.unity | [说明与流程脚本](../Samples~/00_PointPointPoint/README.md) |
| 01 · Modules | 选择模块中的任一主场景 | [完整场景清单](../Samples~/01_Modules/README.md) |
| 02 · StarForce | 打开 StarForce Launcher.unity | [运行说明](../Samples~/02_StarForce/README.md)、[代码导读](05-starforce.md) |

宿主启用 Input System；3D 资源使用 URP。导入后等待编译，默认 Editor Simulation 无需预构建、Fix 或 Save。只有资源更新示例需要先在脚本 Inspector 点击 **Build Remote Resources Example**，见模块 Sample README。

模块示例共用 `Common/Prefabs/QGF.prefab` 和公共 UI 资源，每个场景只展示一项核心功能。运行场景、操作按钮、观察结果，再阅读同名脚本即可；API 按[模块用法](Modules/README.md)查阅。

修改 Excel 后生成 Luban 表；修改收集规则后在 YooAsset 原生窗口 Save。资源包构建与 Player 构建分开执行，见[资源工作流](Modules/resource-build.md)。
