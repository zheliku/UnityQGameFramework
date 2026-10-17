# Luban 安装、运行与发布检查

检查日期：2026-10-18。Luban 的生成、编辑器配置及 Windows Mono 运行链路已完成本轮验证；发布到 GitHub 是独立步骤。当前包版本仍为 `0.1.0-exp.2`，本轮变更记录在 Unreleased 中。

## 新项目使用步骤

1. 在 Unity 2022.3 或 Unity 6 的 Package Manager 中使用 Git URL 安装框架包。Git 客户端需可从 PATH 找到；生成配置还需要 .NET 8 运行时或兼容环境。运行已有生成数据不需要 .NET 或 Luban 编译器。
2. 等待编译完成，默认在 `Assets/UnityQGameFramework/Config` 创建表源、输出目录及 `Editor/LubanProfile.asset`。安装时不下载编译器、不生成业务代码，也不修改已有场景。
3. 在 `Project Settings > Unity Q Game Framework > Luban` 确认全局 Profile。已有项目可指定其他 Profile；新工作区目录可改，现有目录通过 Project 窗口移动并保留 `.meta`。
4. 编辑 Source/Datas 下的 Excel，执行 `Unity Q Game Framework > Generate Data Tables`。首次生成下载并校验固定版本编译器；离线环境选择本地 Luban 5.1.0 的 `Luban.dll`。
5. 把框架预制体放入启动场景，在 DataTable 组件上指定生成的 Catalog。组件 Inspector 也提供“绑定全局 Profile 的 Catalog”；切换全局 Profile 不会隐式修改场景引用。
6. 业务流程在组件 Start 完成后调用 `LoadDataTables(loader => new Tables(loader))`，处理成功／失败事件，再通过 `GetDataTable<DRItem>()` 或 `GetLubanTables<Tables>()` 查询。默认生成 namespace 为 `UnityQGameFramework.Config`；自定义 asmdef 需引用 Core、Runtime、Luban 三个程序集。

namespace／manager／groups 以 Source/luban.conf 为准，Profile Inspector 可查看并打开它们。模板与工具属于 Packages；源表、生成代码、数据、Profile、Catalog 和业务扩展属于消费者的 Assets。运行时依赖只有已编译代码、Catalog 与二进制记录，源 Excel、schema、Profile 不加入资源集合。

## 本轮证据

| 检查 | 结果 |
| --- | --- |
| GitHub main 的真实 HTTPS Git URL | 提交 `5ddfec1f65438d490ebcf639e680c41e36fa6c5e` 可安装；基础编译、AssetBundle 和 Windows Player 通过，但它尚无本地新增的 Luban/TOML |
| 本地新版的真实 UPM Git 安装 | 使用隔离 Git 快照 `df20b023d8a90fe6c6d709c45f86b6a8ec198076`，以 file Git URL 安装到只读 PackageCache；Unity 2022.3.62f3、6000.3.24f1 均通过首次自动初始化、从官方下载编译器、实际菜单生成和 strict 校验 |
| 新版运行 | 上述两个版本均通过 11 张 StarForce 表与 TOML 的 AssetBundle 构建、Windows Mono Player 读取、整组卸载；运行错误 0 |
| 默认工作区直接运行 | Unity 6000.3.24f1：首次生成的 Item 代码与 Catalog 编译进入 Player，读取到“示例道具”，原生与 QGF 查询返回同一记录，运行错误 0 |
| 编辑器故障路径 | 无效 schema 不改输出、移动目录保留 GUID、用户文件保留、废弃生成文件清理、拒绝覆盖未归属目录；模拟文件锁触发中途写入失败后恢复文件／Profile／Catalog；超时子进程终止 |
| 查询及模板 | StarForce 155 项原数据、筛选排序、具名表、取消加载、失败原子发布及跨表引用、字符串键、容器、值类型、list／one 表通过 |
| 包完整性 | Assets 与框架包 1070 个条目、12 份 XLSX ZIP 校验通过；无缺失／孤立 meta、无重复 GUID；依赖 DLL 是实际二进制，不是 Git LFS 指针 |

前两项分别验证远端实际内容和待发布版本。尚未把本地新版提交到远端，因此不能把离线 Git 快照结果表述为“GitHub 已发布新版”。本地报告在 `Artifacts~/ProductionAudit/20261018`；验证工具在仓库 Tools 中，不是消费者的安装依赖。

## Git 与发布产物

框架目录 `Packages/com.zheliku.unityqgameframework` 已受 Git 管理并上传。Git URL 安装直接读取这个目录的 package.json、源码与资产，**不要求把 .tgz 或 unitypackage 提交到 Git**。本轮检查时 main 仍为上述旧提交；Luban/TOML 的本地修改、这次补强的代码和新工具需要一起审查、提交、推送，才能让其他人安装到新版。本轮未提交或推送。

开发分支的 package.json 不登记 Samples：StarForce 唯一开发源码位于 Assets/Examples。发行工具将其复制到输出包的 Samples~ 并登记导入入口。需要通过 Git URL 提供 Samples 时，应将同样包路径的发行暂存树用于发行分支／标签；只推送开发树不会自动出现 Samples 面板。消费者安装框架并生成自己的默认工作区不依赖 StarForce。

发布前运行：

```text
python Tools/Packaging/check_package.py --require-committed
python Tools/Packaging/prepare_upm.py --require-committed --output Artifacts~/UPM/<新的发行目录>
```

默认 check_package 可在开发期间检查包，`--require-committed` 会拒绝尚未提交的 Package 变更。这个选项检查本地 HEAD，不代替 `git ls-remote` 的远端确认。prepare_upm 自动执行完整性检查，输出 `.tgz` 和含 Samples~ 的发布树，逐文件核对示例内容／GUID；Artifacts~ 不提交。正式项目应在安装 URL 后固定已验证的标签或提交，提交宿主 packages-lock.json。

## 已知验证边界

本轮覆盖 Windows 编辑器及 Windows Mono；Android、iOS、WebGL、IL2CPP、macOS／Linux 生成环境、完整 StarForce 资源发行和远程热更新尚未完成平台验证。当前安装自动解压使用系统 tar；没有可用 7z 解压能力的环境需提供已解压的本地编译器。大规模业务表的性能与内存预算应使用实际数据评估。QFramework 的 Architecture／Model／System／Utility 迁移尚未开始。

异常恢复、目录归属和版本检查降低误覆盖及错配风险；配置源与生成产物仍按正常项目资产进行版本控制。详细 API 与生命周期见 [Luban](luban.md)，Config／资源工具配置分工见 [配置说明](configuration.md)。
