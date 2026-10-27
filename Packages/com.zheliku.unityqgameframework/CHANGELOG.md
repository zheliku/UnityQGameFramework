# Changelog

## [Unreleased]

### Added

- ResourceSettings 使用英文 Inspector 和 YooAsset 原生 EPlayMode；每包只保留一个 Play Mode，编辑器与 Player 统一读取。旧双字段配置保留原 Player 模式，示例默认模拟，构建应用前明确选择离线或联机等模式。
- 指定 Collector 后同步原生窗口、包列表和默认包；原生 Save 后同步包增删并保留同名包的运行参数。
- YooAsset 原生 Builder 增加 Batch Build，多选或全选包后使用每个包的原生设置依次构建，首包复制目录与原生单包构建一致；保留刷新后的构建结果。
- 资源入门示例直接 await LoadAssetAsync，文档以 UniTask 和资产归还为主要流程；旧 UGF 兼容成员保留签名并从常用代码提示隐藏。
- 资源模式补充 Custom 初始化扩展，直接返回 YooAsset 官方初始化选项；缓存和有效的 UGF 清理间隔统一放到资源配置的高级选项。
- 移除运行面板中的 Windows 专用 Player 构建区和不生效的旧加载代理配置；首包与应用构建使用 YooAsset、Unity 原生面板。
- 自有框架和示例补齐中文 XML 文档、参数与返回值，表达式主体和紧凑代码块统一为 UGF 写法；增加基于 Roslyn 的代码规范检查，覆盖内部类型中的公开成员、内部方法、运算符和条件编译分支。
- 统一 Luban 表加载器与 QGF DataTable 入口；生成的 `Tables` 实现 `IDataTableLoader`，业务统一通过 `GetDataTable<TbXxx>()` 查询。
- 模块 DataTable 示例增加官方 Excel schema 的字符串键、复合索引、单例表、无主键列表、跨表引用和具名表登记示例。
- 增加 `QGF > Resource > Packages` 入口，复用 YooAsset 原生收集与构建窗口，并支持在 Project 中选择 Collector 资产切换 Sample 配置。
- 资源构建改为读取 YooAsset 原生 Builder 的管线、压缩、首包复制、缓存和加密选项；离线 Player 仅对当前构建临时覆盖首包复制方式。
- 移除重复的 ResourceBuildProfile 配置；每个 Sample 只保留运行配置和官方 Collector，收集与构建统一使用 YooAsset 窗口，组件 Inspector 保留运行设置及快捷入口。
- Luban 的 Profile 和 luban.conf 放在 Config 根目录；Profile 直接引用配置文件，源表目录由 dataDir 指定，保留 Source、Generated、Data，不再预建空目录。
- 点点点和 StarForce Sample 移除专用 Editor 打包脚本；应用构建使用 Unity 原生面板，DataTable Inspector 可直接展开编辑 Profile。
- 资源示例分别展示路径和 YooAsset 地址定位；关闭扩展名别名，避免同目录资源产生重复位置警告。
- QGF 编辑器日志显示调用方法及可点击的文件行号；四个等级使用更明亮的深浅主题色，正文沿用 Console 原生颜色，不再重复输出时间。Player 保留纯文本时间，异常直接保留原类型和堆栈。

### Fixed

- 移除 QGF 对 YooAsset Collector 的自定义 Inspector，恢复原生配置字段显示；保留双击打开官方收集窗口的入口。
- YooAsset 原生收集与构建窗口的背景按 Unity 深浅主题适配，修复浅色主题下深底深字和黑色边框。
- DataTable Inspector 区分生成 Profile 与运行时 Catalog，补充示例和 README 的初始化时序、加载方式及数据清单说明。
- 修复资源后端在退出和销毁包时把 YooAsset 正常终止任务记录为运行时警告的问题。
- 资源更新示例的构建入口移到 `UpdateRemoteResources` Inspector，并使用 `UNITY_EDITOR` 条件编译。
- 修复模块示例编号、表目录序号和发布 Sample 同步后的场景绑定。
- 补齐模块示例的实际行为检查，覆盖数据节点、事件、流程、所有权、复用、下载、网络、声音和调试器生命周期。
- 多个 Sample 各自拥有 Luban Profile 时，编辑器启动等待 QGF Settings 的全局选择，不再输出误导性的初始化警告。
- 修复资源示例在框架退出后清理资产借用时重复访问已关闭资源服务的问题。

### Validation

- Unity 2022.3、6.0、6.3 的 YooAsset、Luban、TOML、取消和重试集成检查通过；Unity 6.3 的 62 个模块场景、点点点和 StarForce 启动检查通过。

- 内置 YooAsset 3.0.6 源码、许可证和版本锁定；增加资源配置、构建 Profile、模拟构建与 Player 资源包构建入口。
- 增加 UniTask 资源等待与取消处理；旧 Resource 模块的整体迁移尚在进行。
- ResourceComponent 可指定 ResourceSettings，接入 YooAsset 资产借用、原生文件与场景加载；增加异步加载、独立句柄、包初始化和安全销毁入口。
- 增加 `LoadDataTablesAsync<T>`，与原有表集事件入口共用请求记录，支持取消、失败异常及重试。
- 增加 `DownloadResourcesAsync`，统一 YooAsset 下载的 UniTask 等待、失败异常和取消；StarForce 增加下载确认、进度、重试及更新后缓存清理流程。
- StarForce 预载 Luban 表集与主字体改用 UniTask，启动失败提示不依赖资源包；修复内置对话框同帧连续打开时丢失重试提示。
- 示例改用资产 GUID 作为 Bundle 名，缩短按 Profile 隔离的构建缓存路径，修复 Windows 深层目录下的资源构建路径过长错误。
- 联机包可关闭内置文件系统，支持没有 StreamingAssets 首包的纯远端下载；修正 HTTP 更新示例强制读取缺失的 BuiltinCatalog 的问题。
- 重载启动场景时等待 QGF 上一实例完成资源包关闭，修复 StarForce 切换语言后同名包仍被占用、停在初始化的问题。
- Player 资源构建现在同步生成首包目录并部署 `BuiltinCatalog.bytes`，修复离线 Player 启动时缺少内置资源目录的问题。
- 同一 Profile 和版本可重复构建，只覆盖该版本产物并保留增量缓存。
- 打包器支持排除内置 UniTask 或 YooAsset，供已安装官方依赖的项目使用；默认分发继续内置源码。

## [0.1.0-exp.8] - 2026-10-24

### Changed

- 模块示例合并为一个 `01_Modules` Sample，保留 62 个核心场景，移除重复的 API 演示；点点点和 StarForce 仍为独立 Sample。
- `Assets/Examples` 和 `Samples~` 统一为 `00_PointPointPoint`、`01_Modules`、`02_StarForce`。从 Assets 原样复制发布，不再筛选场景或转换 GUID、程序集和命名空间。
- 模块示例共用一个运行时程序集和一份 QGF prefab；更新场景引用、Luban Profile、资源构建路径与维护工具。
- 示例 JSON 输入框恢复可编辑，序列化与反序列化按钮分别写入和读取当前输入内容。
- 公开 API 的 XML 文档按 UGF 风格补齐泛型参数、方法参数和返回值说明。

## [0.1.0-exp.7] - 2026-10-24

### Changed

- DataTable 直接登记 Luban 生成表，统一使用 `GetDataTable<TbXxx>()`；原调用的泛型参数需从 DR 行类型迁移为 Tb 表类型。
- 移除独立的 GetLubanTables 查询入口、额外表视图和 IDataRow 的 int Id 约束。字符串、长整数、枚举、复合键、list、one、结构体记录及跨表引用均通过同一入口访问。
- 保留 QGF 的条件查询和排序，结果直接引用生成表中的记录；整组登记在完成构造、引用解析及结构校验后发布。
- 更新 StarForce、模块示例、Inspector 和文档；公开接口及生成模板采用 UGF 的普通方法体与完整 XML 文档注释。
- 明确 IDataProvider 继续服务 Config 与 Localization；Luban 使用独立的 Catalog、多 bytes、跨表解析和原子登记流程，不引入不匹配的 LubanTableProvider。

## [0.1.0-exp.6] - 2026-10-24

### Fixed

- 3D 与 UI 模块适配横竖屏，镜头完整显示演示物体，窗口避开操作面板，长面板显示原生滚动条。
- StarForce 从实际小行星表记录中随机选取类型，支持不连续的 Id；每局重置出怪计时，游戏结束后停止出怪。
- DataNode 示例预览前四个物品节点并显示剩余数量，清空后同步按钮状态，添加时重建缺少的路径。
- 模块示例的下拉框标题按可用空间缩放文字，避免长选项溢出；修正随机数标记的命名，保留其强调色。

### Changed

- 模块示例继续按真实任务整理：移除 API 重载选择器和重复的启动等待；对象池保留具名工厂和回收策略，Luban 保留条件筛选与排序示例。
- 资源包加载、Luban 异步加载、二进制片段、请求队列、下载优先级和声音组均收敛为清晰操作路径；模块程序集自动引用 UniTask。
- 配置与语言字典的异步读取示例固定使用带 UserData 的入口，移除只用于切换重载的下拉框，保留语言切换与失败恢复。
- 明确框架组件的启动顺序，使默认顺序的业务 Start 可以直接调用模块。同步 LoadDataTables 返回是否成功，生成表统一从 DataTable 查询，组件保留 RegisterDataTables 入口。
- 更新 Sample 分组、卸载根目录识别与打包检查，补充 Luban 生成流程及两种查询入口的适用范围。
- 精简模块文档的重复说明，补充 DataNode 和 StarForce 主流程的调用注释，修正 StarForce 的菜单与战斗场景加载说明。

## [0.1.0-exp.5] - 2026-10-23

### Added

- 提供点点点、21 个独立模块 Sample 和 StarForce。模块包含 102 个主场景，配套脚本、资源和模块文档。
- 点点点展示 Procedure 启动、TOML 规则、UIForm、事件计分、结算与最高分保存；点点点和 StarForce 提供独立的 Windows64 Player 构建入口。
- Base、Fsm、Procedure、ReferencePool、ObjectPool 增加实时计时、进度、生命周期、引用所有权、Cube 借还、业务工厂、共享借用和回收策略示例。
- Config、Setting、Localization、DataTable 增加参数解析、保存与重读、字典切换、Luban 加载取消、表查询及注册管理示例；相近重载在同一界面对照。
- Resource 增加 Package、二进制、HTTP 更新、资源包、缓存校验、变体、缓存回收和按需下载示例，附资源构建菜单。
- Entity、UI、Sound、Scene 通过可见实体、窗口、声源和附加场景展示复用、分组、挂接、空间声音、切关和生命周期。
- FileSystem、WebRequest、Download、Network、Debugger 增加内容、队列、进度、失败恢复和调试窗口示例。网络示例自带本机服务。
- 内置官方 UniTask 2.5.11 源码与许可证；新增 `QGF` 静态组件入口和框架运行、模块协作、安装与资源构建文档。

### Changed

- 源场景仍按模块放在 `Assets/Examples`；UPM 发布收拢为三个 Sample，StarForce 的源码目录仍编号为 22。
- 示例改用 uGUI、StarForce 风格控件和默认天空盒，控件通过序列化引用绑定，物体采用类型前缀与 PascalCase 用途名；StarForce 保留星空背景。
- 示例直接展示 QGF 调用，移除 Print、仅转发的辅助方法和无用生命周期钩子；跨帧等待使用 UniTask，Socket 与文件 I/O 保留 .NET Task。
- 示例注释说明参数、生命周期、异步时序和所有权；关闭、取消和退出时只清理自身创建的对象与请求。直接依赖 Editor API 的示例保留条件编译。
- 示例日志统一使用框架 Log，由 `QGF > Logging` 控制。StarForce 使用默认 JSON 辅助器和 Unity 可序列化版本字段，移除 LitJson 辅助器及 DLL。
- 新增选中示例的备份卸载，支持整体移动后的模块 Sample 和旧版导入目录；备份写入 `QgfBackup~`。
- 资源构建器支持显式 Collection 路径；ResourceComponent 保留业务在 Awake 中设置的资源目录。Player 构建产物生成在 Library 和 Player 输出目录。
- 可选扩展窗口与安装文档显示上游许可，分别说明 Rainbow Folders 2 的代码与美术资源许可，以及 Git 版本锁定方式。

### Fixed

- 修复独立模块 Sample 导入后没有登记卸载根目录的问题；同时保留旧版 Framework prefab 锚点的识别。
- 修正 Config 示例 TOML 的功能开关键名，使初始界面与 `Game.FeatureEnabled` 查询一致。
- ResourceComponent 支持在 Awake 或启用前设置解密回调；变体示例在创建加载代理前注册，修复初始化被拒绝的问题。
- StarForce 更新前使用场景携带的对话框，预加载后交给 UI 模块；移除自动确认，保留取消操作，防止重复点击触发多次回调，并保留外部暂停状态。
- 修正 StarForce 弹窗宽度和正文间距；启动翻译集中在内置字典，避免预加载时出现重复键。
- 修复示例下拉列表行高、文字对比度和选中状态；StarForce 启动场景改用 UIInputAdapter，支持当前启用的原生输入模块。
- StarForce 窗口、按钮和血条动画使用非缩放时间，关闭或回池时取消旧动画；暂停和切换视图后仍可完成淡出。
- 修复 Base 时间、暂停和应用参数示例退出后，QGF 属性与 Unity 状态不一致的问题。
- 修复 ObjectPool 示例被其他模块内部池干扰的问题；说明 `Register(item, true)` 会执行一次 OnSpawn 并计入一次借用。
- 修复异或区间结束位置、短文件快速处理和负长度入口；加密二进制片段按源偏移还原，并保留目标缓冲区范围。自定义回调先还原完整资源再取片段。
- Resource 的最后一个下载耗尽重试后触发失败完成回调，使更新状态能结束并再次重试。
- 同步 TCP 通道识别远端 FIN；合并发送时按消息数累计发送包数。
- 默认 Download Helper 在写入前校验 HTTP 错误与 Range 响应，拒绝忽略续传或起点不符的数据。
- WebRequest 取消和添加失败时归还表单上下文；byte[] POST 发送原始请求体，表单字段使用 WWWForm。
- 默认文件流以 Write 模式重开时允许读取容器头和索引；业务接口继续禁止读取只写容器的文件内容。
- Sound 加载取消或失败时清理等待请求并归还上下文；暂停保留资源与位置，停止时释放资源并取消残留渐变，音量渐变逐帧推进。
- EventHandlerCount 改为统计所有已订阅处理函数；默认 Unity 日志辅助器保留异常类型、堆栈和 Console 定位。
- Debugger 注销最后一个窗口后正确处理空分组，移除较早窗口时保留当前选择，重复注销返回 false。

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
- 同步包内 StarForce Sample 发布副本，更新安装和运行说明。

### Added

- 添加包根目录的 `CHANGELOG.md`，供 Package Manager 查看版本记录。
- 发布检查验证包版本、框架显示版本和最新 Changelog 版本一致。

### Upgrade Notes

- 更新 Git 包后，已导入 `Assets/Samples` 的示例不会自动覆盖。先通过 `QGF > Uninstall > Space Shooter` 备份并移除旧示例，再导入新版本，避免两个版本的示例脚本同时参与编译。
- Git 依赖通过 Package Manager 主动更新；单纯重启 Unity 或修改包版本号不会解除已锁定的 Git 提交。

## [0.1.0-exp.2]

### Added

- 接入 Luban 配置表，提供全局 Profile、Catalog、DataTable Inspector 和一键生成菜单。
- 使用官方 Excel schema，提供 `__tables__`、`__beans__`、`__enums__` 初始表格与工作区初始化、修复功能。
- 提供 Git URL 安装、包内 StarForce Sample、配置目录管理和卸载备份。
- 添加安装、配置表制作及运行时读取示例。

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
