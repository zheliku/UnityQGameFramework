# UnityQGameFramework

QGF 基于 UnityGameFramework 的模块和 Unity 组件开发，使用 Luban 制作数据表、Tomlyn 读取 TOML，内置 UniTask。兼容目标为 Unity 2022.3、6.0、6.3；示例的 3D 资源使用 URP。

[入门](Documentation~/01-getting-started.md) · [模块](Documentation~/Modules/README.md) · [Luban](Documentation~/02-data-tables.md) · [变更记录](CHANGELOG.md)

## 安装

在 Package Manager 选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

编译完成后，可从 Samples 导入点点点、模块示例或 StarForce。模块示例包含一份公共资源；完整清单见[示例目录](Documentation~/06-module-examples.md)。

游戏代码和资源放在 Assets，框架与工具放在 Packages。Luban 默认工作区为 `Assets/UnityQGameFramework/Config`，全局 Profile 在 `QGF > Settings` 选择。

## 文档

DataTableComponent 自动初始化；在业务 `Start` 或启动流程中用 `LoadDataTables<Tables>()` 加载一次，再用 `GetDataTable<TbXxx>()` 查询。全局 Luban Profile 决定生成目标，组件的 Catalog 决定运行时数据；Catalog 是数据清单，不是缓存。配置与完整步骤见[模块 Sample README](Samples~/01_Modules/README.md#datatable-从哪里开始)。

- [框架运行](Documentation~/00-runtime.md)：初始化、更新、关闭和异步调用。
- [模块示例](Documentation~/06-module-examples.md)：运行场景、编辑代码和资源。
- [项目约定](Documentation~/03-conventions.md)：生命周期与资源归还。
- [StarForce](Documentation~/05-starforce.md)：从启动流程读到飞机、武器和血条。
- [UGF 到 QGF](Documentation~/04-changes-from-ugf.md)：当前改动与架构迁移进度。

`QGF > Extensions` 提供 Alchemy 和 Rainbow Folders 2 的用途与 Git URL，复制后通过 Package Manager 安装。

源码采用 [MIT](LICENSE.md)，第三方内容见 [许可声明](<Third%20Party%20Notices.md>)。
