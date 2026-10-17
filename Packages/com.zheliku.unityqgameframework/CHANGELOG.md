# Changelog

UnityQGameFramework 的版本变更记录，最新版本在前。

## [0.1.0-exp.4] - 2026-10-18

### Added

- 新增 `QGF > Extensions`，按需安装 Alchemy 与 Rainbow Folders 2，显示已安装状态与 Unity 版本要求。QGF 核心不增加这两个工具的依赖，已有安装与固定提交保持原样。

### Fixed

- 修复 StarForce 的 TIFF 贴图遗漏 Git LFS 排除规则，导致新工程通过 Git URL 安装时可能只收到指针文件、背景显示为白色的问题。示例贴图现在作为普通 Git 二进制内容发布。

### Changed

- 发布检查覆盖包内所有文件的 LFS 指针和属性，发布已提交版本时额外检查 Git 中实际存储的内容。
- 示例验证增加贴图引用与真实 Game 画面的检查，覆盖菜单文字、按钮、背景和战斗显示。

### Upgrade Notes

- 此修复必须连同 `.gitattributes` 和 TIFF 贴图文件一起提交、推送，不能只提交属性或版本号。
- 已导入的旧示例需要备份、移除后重新导入，才能替换之前复制到 Assets 中的无效贴图。

## [0.1.0-exp.3] - 2026-10-18

### Fixed

- 修复 StarForce 血条依赖其他脚本的 `Start()` 初始化顺序，导致对象池未就绪、`Update()` 持续抛出空引用的问题。
- 血条模板、Canvas 或对象池缺失时给出明确错误，并停止血条组件更新。

### Changed

- StarForce 的 21 个材质统一使用 URP 内置 Lit、Unlit、Particles/Unlit Shader，移除示例自定义 Shader。
- StarForce 以已启用 URP 的工程为运行前提，导入示例不修改宿主的渲染管线、输入设置、Layer 或构建场景列表。
- 同步包内 `Samples~/StarForce` 发布副本，更新安装和运行说明。

### Added

- 添加包根目录的 `CHANGELOG.md`，供 Package Manager 查看版本记录。
- 发布检查验证包版本、框架显示版本和最新 Changelog 版本一致。

### Upgrade Notes

- 更新 Git 包后，已导入 `Assets/Samples` 的示例不会自动覆盖。先通过 `QGF > Uninstall > StarForce` 备份并移除旧示例，再导入新版本，避免两个版本的示例脚本同时参与编译。
- Git 依赖通过 Package Manager 主动更新；单纯重启 Unity 或修改包版本号不会解除已锁定的 Git 提交。

## [0.1.0-exp.2]

### Added

- 接入 Luban 配置表，提供全局 Profile、Catalog、DataTable Inspector 和一键生成菜单。
- 使用官方 Excel schema，提供 `__tables__`、`__beans__`、`__enums__` 初始表格与工作区初始化、修复功能。
- 提供 Git URL 安装、包内 StarForce Sample、配置目录管理和卸载备份。
- 添加安装、配置表制作及运行时读取教程。

### Changed

- 框架命名空间与程序集统一为 `UnityQGameFramework`。
- DataTable 保留组件入口，底层改用 Luban，移除原有 TXT 表生成与解析流程。
- 游戏参数、语言字典、BuildInfo 和编辑器资源配置改用 TOML，并同步相关资产引用。
- 适配现代 Unity API，保留 StarForce 原版鼠标操作。

### Fixed

- 修复 Git 安装后默认配置工作区及初始 Excel schema 缺失的问题。
- 修复示例对宿主 Layer、输入后端和编辑器构建场景列表的依赖。

## [0.1.0-exp.1] - 2026-10-16

### Added

- 导入 UnityGameFramework 源码，建立 UnityQGameFramework 的 UPM 包结构。
- 添加 StarForce 示例与 Unity 6 开发工程。
