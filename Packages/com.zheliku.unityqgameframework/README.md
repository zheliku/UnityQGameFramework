# UnityQGameFramework

QGF 基于 UnityGameFramework，使用 Luban 制作数据表、Tomlyn 读取 TOML、YooAsset 管理资源、UniTask 处理异步。支持目标为 Unity 2022.3、6.0、6.3，示例使用 uGUI、Input System 和 URP。

## 安装

在 Package Manager 选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

默认内置 UniTask 和 YooAsset，请勿重复安装。游戏脚本、Excel 和资源放在 `Assets`；框架放在 `Packages`。

## 示例与文档

从 Package Manager 导入一个 Sample，打开启动场景并运行。宿主需启用 Input System；示例不会替你修改项目设置。编辑器模拟模式无需预先打包资源或点击 YooAsset 的 Fix。

| Sample | 内容 |
| --- | --- |
| 00 · PointPointPoint | 用流程、UI、事件和设置完成一个小游戏 |
| 01 · Modules | 按模块查看核心调用和运行结果 |
| 02 · StarForce | 飞机、武器、数据表、资源和界面的协作 |

[开始使用](Documentation~/01-getting-started.md) · [模块用法](Documentation~/Modules/README.md) · [Luban](Documentation~/02-data-tables.md) · [资源构建](Documentation~/Modules/resource-build.md) · [更新记录](CHANGELOG.md)

基于 [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework) 和 [GameFramework](https://github.com/EllanJiang/GameFramework)，示例与架构设计参考 [QFramework](https://github.com/liangxiegame/QFramework)。目前尚未移植 QFramework 的 Architecture、Model、System。

源码采用 [MIT](LICENSE.md)，第三方内容见 [许可声明](<Third%20Party%20Notices.md>)。
