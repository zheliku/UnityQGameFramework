# Unity Q Game Framework

Unity Q Game Framework 是面向 **Unity 2022.3 LTS 及 Unity 6** 的模块化游戏框架。当前基础维护版为 `0.1.0-exp.2`：提供纯 C# 核心、Unity 运行时组件、编辑器资源工具和 StarForce 示例。

本阶段统一框架名称、公共命名空间和程序集，更新 Unity API，清理低于最低支持版本的代码分支，并修复程序集发现与生命周期问题。资源、实体、UI、流程、事件、对象池和网络继续保留。DataTable 已接入 Luban，采用 Excel schema，保留 QGF 查询接口并提供一键生成菜单；游戏 Config 使用 Tomlyn 读取 TOML。QFramework 架构融合尚未开始。详见 [Luban 使用说明](Packages/com.zheliku.unityqgameframework/Documentation~/luban.md) 与 [配置文件说明](Packages/com.zheliku.unityqgameframework/Documentation~/configuration.md)。

## 安装

在 Package Manager 中选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

上述地址在本次修改推送后包含基础维护版；发行时使用版本标签固定依赖。本地开发直接打开仓库根工程，框架为 embedded package，无需重复添加自身的 Git 依赖。

## 结构

```text
Assets/Examples/StarForce/                    可直接运行、调试的示例
Packages/com.zheliku.unityqgameframework/
  Runtime/Core/                             引擎无关的模块、接口与通用工具
  Runtime/Unity/                            Unity 组件、默认辅助器、Debugger
  Runtime/Plugins/                          压缩与 TOML 依赖
  Runtime/Prefabs/UnityQGameFramework.prefab  框架入口预制体
  Editor/                                   Inspector、资源管理和打包工具
  Documentation~/                           架构、API、兼容与迁移说明
Tools/Packaging/                            发行包生成
Tools/Validation/                           独立消费者工程与回归验证
Artifacts~/                                 被忽略的验证工程与发行产物
```

程序集为 `UnityQGameFramework.Core`、`UnityQGameFramework.Runtime` 和 `UnityQGameFramework.Editor`。核心不引用 UnityEngine；运行时不依赖 URP、Input System、Unity CLI 或本仓库的编辑器插件。业务代码可使用自定义 asmdef，编辑器会扫描可用于当前项目的流程与辅助器。

## 开始使用

将框架预制体放入首个启动场景，在 Inspector 中配置流程与所需辅助器。菜单统一位于 **Unity Q Game Framework**。见 [基础 API](Packages/com.zheliku.unityqgameframework/Documentation~/api.md) 与 [架构说明](Packages/com.zheliku.unityqgameframework/Documentation~/architecture.md)。

StarForce 入口为 `Assets/Examples/StarForce/StarForce Launcher.unity`。示例使用 URP；在 Game 视图按住鼠标左键移动并开火。场景列表、Layer 和输入系统配置见 [示例说明](Assets/Examples/StarForce/README.md)。

## 版本与验证

最低安装版本为 Unity 2022.3。仓库开发宿主使用 Unity 6000.3.24f1；其 URP、Input System 和工具包版本属于宿主，不会通过框架包强制安装到消费者工程。

Unity 2022.3、6.0 和 6.3 的实际验证范围及验证命令见 [兼容性说明](Packages/com.zheliku.unityqgameframework/Documentation~/compatibility.md)。

Luban 的首次 Git 安装、编辑器配置、生成、运行及发布状态见 [生产前检查记录](Packages/com.zheliku.unityqgameframework/Documentation~/production-readiness.md)。发布暂存前会自动检查包完整性，可加 `--require-committed` 拒绝尚未提交的 Package 变更。

更名涉及公共 API 和程序集名称，已有代码迁移请阅读 [迁移说明](Packages/com.zheliku.unityqgameframework/Documentation~/migration.md)。

源码整理遵循 [C# 代码约定](Packages/com.zheliku.unityqgameframework/Documentation~/coding-style.md)，根目录 `.editorconfig` 提供 IDE 建议。自动属性仅用于适合的非序列化存储；已有 Unity 资产依赖的字段名称和特性继续保留。

## 发布

StarForce 的唯一开发源码位于 Assets。运行 `python Tools/Packaging/prepare_upm.py`，会在 `Artifacts~/UPM/<版本>/` 生成包含 `Samples~/StarForce` 的发行树和 `.tgz`，校验示例内容及 GUID。消费者从 Package Manager 导入示例后再编辑。

开发分支 Git URL 提供框架；含 Samples 的发行标签应使用生成的发布树。unitypackage 仍从同一份包内容暂存导出，避免维护多份源码。详见 [目录与发布约定](Packages/com.zheliku.unityqgameframework/Documentation~/repository-layout.md)。

框架沿用 MIT 许可。原始版权、第三方组件许可及必要的来源记录保留于 [第三方声明](Packages/com.zheliku.unityqgameframework/Third%20Party%20Notices.md)，不作为产品帮助入口或示例服务地址。
