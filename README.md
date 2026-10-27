# UnityQGameFramework

QGF 基于 UnityGameFramework 的模块和 Unity 组件开发，使用 Luban 制作数据表、Tomlyn 读取 TOML，内置 UniTask。兼容目标为 Unity 2022.3、6.0、6.3；示例的 3D 资源使用 URP。

[入门](Packages/com.zheliku.unityqgameframework/Documentation~/01-getting-started.md) · [模块](Packages/com.zheliku.unityqgameframework/Documentation~/Modules/README.md) · [Luban](Packages/com.zheliku.unityqgameframework/Documentation~/02-data-tables.md) · [变更记录](Packages/com.zheliku.unityqgameframework/CHANGELOG.md)

| StarForce 菜单 | 战斗 |
| --- | --- |
| ![菜单](Packages/com.zheliku.unityqgameframework/Documentation~/Images/starforce-menu.png) | ![战斗](Packages/com.zheliku.unityqgameframework/Documentation~/Images/starforce-battle.png) |

## 安装

在 Package Manager 选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

编译完成后，Samples 中有三个导入项：点点点、模块示例和 StarForce。模块示例内部按模块编号，并包含一份公共资源。

游戏代码和资源放在 Assets，框架与工具放在 Packages。Luban 默认工作区为 `Assets/UnityQGameFramework/Config`，全局 Profile 在 `QGF > Settings` 选择。

资源示例使用 YooAsset 原生 `BundleCollectorSetting` 和 `BundleBuilder` 窗口；QGF 只提供 Sample 配置切换和 ResourceComponent 关联，不修改 YooAsset 源码。Luban 运行库只保留必要源码，编译器按需下载到 `Library`。

## 运行示例

导入 Sample 后等待 Unity 编译完成，再打开对应场景点击 Play。示例默认使用各自的 YooAsset Editor Simulation，普通运行无需手动构建 AssetBundle，也无需在 YooAsset 收集器窗口点击 `Fix` 或 `Save`；修改收集规则后才需要保存，路径失效时再使用 Fix。资源构建和 Player 构建请按各 Sample 的 README 操作。

## 开发

数据表不需要手动 Initialize：场景 QGF 的 DataTableComponent 指定生成的 `Catalog.asset` 后，在普通业务 `Start` 或启动流程中调用一次 `LoadDataTables<Tables>()`，成功后用 `GetDataTable<TbItem>()` 查询。全局 Luban Profile 只决定编辑器生成哪份表，Catalog 才决定运行时数据，二者不是两份缓存。配置关系和同步/异步完整步骤见[模块示例 README](Assets/Examples/01_Modules/README.md#datatable-从哪里开始)。

```text
Assets/Examples/00_PointPointPoint/       点点点
Assets/Examples/01_Modules/               模块示例
Assets/Examples/02_StarForce/             StarForce
Packages/com.zheliku.unityqgameframework/ UPM 包
Tools/Packaging/                        同步与打包
Artifacts~/                             本机产物与验证记录
```

示例只在 Assets 中编辑，目录与包内 Samples~ 相同。修改后原样复制发布并检查包：

```sh
python Tools/Packaging/sync_samples.py
python Tools/Packaging/check_package.py
python Tools/Documentation/check_docs.py
```

`python Tools/Packaging/prepare_upm.py` 生成 tgz。发布时同步 package.json、框架版本和 Changelog。

消费项目已安装官方 UniTask 或 YooAsset 时，可分别追加 `--external-dependency unitask`、`--external-dependency yooasset` 生成排除对应内置副本的包。输出的 `external-dependencies.json` 列出消费项目需要安装的官方 Git URL；默认 Git 包仍包含两个插件，不能与官方副本同时安装。

## 文档与扩展

- [运行机制](Packages/com.zheliku.unityqgameframework/Documentation~/00-runtime.md)：初始化、统一更新与关闭。
- [项目约定](Packages/com.zheliku.unityqgameframework/Documentation~/03-conventions.md)：生命周期、事件与资源归还。
- [StarForce 导读](Packages/com.zheliku.unityqgameframework/Documentation~/05-starforce.md)：飞机、武器、UI 和数据表的协作。
- [UGF 到 QGF](Packages/com.zheliku.unityqgameframework/Documentation~/04-changes-from-ugf.md)：当前改动与架构迁移进度。

`QGF > Extensions` 提供 Alchemy 和 Rainbow Folders 2 的可选安装入口。

## 来源与许可

基于 [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework) 和 [GameFramework](https://github.com/EllanJiang/GameFramework)。配置流程参考 GodotGameFramework，架构设计与示例参考 [QFramework](https://github.com/liangxiegame/QFramework)。

源码采用 [MIT](Packages/com.zheliku.unityqgameframework/LICENSE.md)，第三方内容见 [许可声明](Packages/com.zheliku.unityqgameframework/Third%20Party%20Notices.md)。
资源配置只有一个保存来源：`ResourceSettings.asset`。ResourceComponent 仅引用它；Collector 资产由 YooAsset 官方窗口管理。运行模式、服务器和缓存设置在 ResourceSettings Inspector 修改，避免在组件、窗口和多个 SO 中重复保存。
