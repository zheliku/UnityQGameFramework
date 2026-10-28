# 示例

| 开发目录 | 启动与用途 |
| --- | --- |
| [00_PointPointPoint](00_PointPointPoint/README.md) | 运行 GameMain/Scenes/PointPointPoint.unity，查看小游戏从启动到结算 |
| [01_Modules](01_Modules/README.md) | 每个场景展示一项核心调用，按模块选择 |
| [02_StarForce](02_StarForce/README.md) | 运行 StarForce Launcher.unity，查看多个模块协作 |

开发只修改 Assets/Examples，发布副本由 Tools/Packaging/sync_samples.py 同步到包内 Samples~。三个 Sample 各有一个根程序集，可独立导入、移动或删除。

宿主启用 Input System，3D 资源使用 URP；示例不会修改项目设置。普通 Play 自动生成 YooAsset Editor Simulation，无需先打包、Fix 或 Save。资源更新示例的前置构建见模块 README，Player 通过 YooAsset 构建首包后再由 Unity 构建。

框架的加载、查询和清理约定见包内 Documentation~/Modules。
