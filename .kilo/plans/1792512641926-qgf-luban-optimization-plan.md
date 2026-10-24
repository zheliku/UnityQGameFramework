# QGF Luban 接入与框架优化计划

## 目标与范围

修复审查中发现的高风险问题，完善 Luban 工具链、运行时 API 与编辑器体验，补齐测试与自动化，最后更新数据表相关示例与文档。

- 主要改动目录：`Packages/com.zheliku.unityqgameframework`（简称 PKG）。
- 示例只改以下范围：
  - `Assets/Examples/01_Modules/11_DataTable`
  - `Assets/Examples/02_StarForce` 中调用数据表的代码
- 文档只改数据表相关章节。
- QFramework Architecture 层、Resource 换 YooAsset、全站文档重组不在本计划内，见末尾"后续"。

## 前置条件

1. 当前工作区暂存了约 3192 个文件的大改动。开始前由用户提交，或建分支保存。实现方只有在用户要求时才提交。
2. GPT 正在修改示例。改动 `Assets/Examples` 和 `Documentation~` 前，先用 `git status` 和 `git diff` 确认对方没有在改同一文件。冲突时先问用户。
3. Unity CLI 必须可用：`unity status` 能连上本工程的 Editor（6000.3.24f1，已装 com.unity.pipeline 0.7.0-exp.1）。找不到 `unity` 或连不上 Editor 时，停下来告知用户，不要改用 Unity.exe。

## 决策

用户关闭了确认问题，以下暂按推荐方案执行。如需调整，请在对应 Phase 开始前修改。

| 编号 | 决策 | 默认方案 |
| --- | --- | --- |
| D1 | 业务查询以哪一层为主 | 以 Luban 原生 Tables 为主；QGF `IDataTable` 视图保留，作为兼容 UGF 的通用注册表，供 Inspector、调试和按 Id 查询使用。视图不扩展到非 int 主键 |
| D2 | 生成代码放在哪个程序集 | 新建工作区时，在根目录生成 `<topModule>.asmdef`（`autoReferenced: true`），同时覆盖 Generated 和 Extensions。已有工作区不自动迁移，由窗口提供"添加程序集定义"按钮 |
| D3 | 异步 API 放在哪里 | Runtime 程序集按名称引用 `UniTask`，不新建扩展程序集 |
| D4 | 卸载行为 | 从 Package Manager 移除包时，示例（包内容的副本，留下会导致编译错误）仍自动备份后移除；配置工作区（用户的 Excel 与生成物）不再自动删除，只在日志中列出。完整卸载用菜单 `QGF > Uninstall > Framework` |
| D5 | Debugger 默认窗口 | 组件默认值和 QGF 预制体都改为 `OnlyOpenWhenDevelopment` |
| D6 | 编辑器 UI 技术 | 继续用 IMGUI，与现有 Inspector 保持一致，兼容 2022.3 |

## Phase 0 基线与核实（不改代码）

1. 建立 Unity CLI 连接：
   ```
   unity status
   unity command --project-path P:\Unity-Project\Test\UnityQGameFramework set_autotick --enable true
   ```
2. 记录基线：
   - 运行 `unity recompile`，记录现有警告。
   - 运行 `unity command console --level warn --tail 200`。
   - 先查看 `ProjectSettings/UnityQGameFrameworkSettings.asset` 指向哪个 Profile。如果不是默认工作区 `Assets/UnityQGameFramework/Config`，先问用户再生成。
   - 执行 `unity command menu --path "QGF/Generate Tables"`，等 `LubanGenerator.IsBusy` 变为 false，再等重编译结束。
   - Console 应没有 error，`git diff` 应无变化，即生成是幂等的。
3. 用 `dotnet <Library/UnityQGameFramework/Tools/Luban/5.1.0/Luban/Luban.dll> --help` 核实 Luban 5.1.0 是否支持以下能力，结果用于 Phase 2：
   - `--errorFormat json`
   - `--locale zh`
   - `-c schema-json`
   - 多个 code target 一起运行时，怎样分别指定输出目录
   - `pathValidator.rootDir`
   - `dotnet --roll-forward Major <Luban.dll> --version` 能否运行

## Phase 1 测试骨架与高风险修复

0. **测试骨架**
   - 新增两个测试程序集，都设置 `defineConstraints: UNITY_INCLUDE_TESTS` 并引用 nunit：
     - `PKG/Tests/Editor/UnityQGameFramework.Tests.Editor.asmdef`
     - `PKG/Tests/Runtime/UnityQGameFramework.Tests.Runtime.asmdef`
   - 在 Editor 程序集中加入 `InternalsVisibleTo("UnityQGameFramework.Tests.Editor")`，并把下面要测的 `private static` 方法改为 `internal static`。
   - 先给现有行为写单测，锁定当前语义后再重构：
     - `LubanGenerator.PrepareCode` 的哈希是否确定
     - `CheckSync` 和 `OwnedPath` 能否拒绝越权写入与路径穿越
     - `Overlaps`、`Quote`
     - DataTableManager 的注册、命名和销毁
1. **卸载安全**（`Editor/Luban/QgfProjectAssets.cs:27-30`，D4）
   - `registeringPackages` 回调只备份并移除示例根目录，不再处理 `m_Workspaces`。
   - 日志列出保留的工作区路径，说明其中的生成代码在重装 QGF 前无法编译，可以手动删除。
   - `RemoveFramework`（`:177-183`）的确认对话框列出将要备份并删除的工作区和示例路径。
   - 验收：从 Package Manager 移除包后，配置工作区仍在，示例已移入 `QgfBackup~`。
2. **UniTask 同名冲突**
   - 修改 `PKG/Runtime/Plugins/UniTask/**` 下的全部 asmdef：
     - versionDefines：`com.cysharp.unitask` → `QGF_EXTERNAL_UNITASK`
     - defineConstraints：`!QGF_EXTERNAL_UNITASK`
   - 文档说明：UniTask 以源码形式放在 Assets 中时，需要手动定义 `QGF_EXTERNAL_UNITASK`。
   - 在 `Tools/Packaging/unitask-source.json` 中记录这项改动与上游的差异。
   - 验收：
     - 本工程编译结果不变。
     - 用 `Tools/Validation/prepare_project.py` 建临时工程，加入 `com.cysharp.unitask` Git 依赖后，不出现同名程序集错误。
3. **Luban 工具链**：从 `LubanGenerator.cs` 中拆出 `Editor/Luban/LubanToolchain.cs`。
   - **dotnet**
     - 按以下顺序查找：EditorPrefs 中按机器配置的路径 → PATH → 常见安装位置（Windows Program Files、`/usr/local/share/dotnet`、`~/.dotnet`）。
     - 启动 Luban 时加 `--roll-forward Major`。这样在装有 .NET 8 时优先用 8，没有 8 时用更高的主版本。
   - **解压**（替换 `:80` 的 `tar`）
     - 优先使用 `EditorApplication.applicationContentsPath + "/Tools/7z.exe"`。2022.3.62f3 与 6000.3.24f1 都已确认存在。
     - macOS 和 Linux 上的 `7za` 路径需实测。
     - 找不到 7z 时再回退到 tar。
   - **离线安装与镜像**
     - 支持选择本地 `Luban.7z` 安装，同样校验 `luban-lock.json` 中的 sha256。
     - 下载地址可配置，默认仍为 GitHub（`:67`）。
   - **IsBusy**：覆盖"安装 + 生成"整个过程，修复两步之间短暂变回 false 的问题。
   - **Quote**（`:272`）：按 Windows 命令行规则转义，处理路径以反斜杠结尾的情况。
   - 验收：
     - 只装有 .NET 9 或 10 时可以生成。可以把 `DOTNET_ROOT` 指向只含 9 的安装来模拟。
     - 断网时能从本地压缩包安装。
4. **快捷键**：去掉 `LubanMenu.cs:10` 的 `%#g`，它与 Unity 内置 Create Empty Parent 冲突。文档改为说明可在 Shortcuts 中自行绑定。
5. **Debugger 旧版输入窗口**：给 6 个窗口文件及其注册代码（`Runtime/Unity/Debugger/DebuggerComponent.cs:164-169`）加 `#if ENABLE_LEGACY_INPUT_MANAGER`。验收：本工程（`activeInputHandler: 1`）打开 Debugger 各页都不抛异常。
6. **Excel 锁文件**
   - 生成前，如果 Source 下存在 `~$*` 文件，提示"有工作簿正在 Excel 中打开，请确认已保存"。
   - 文档建议在 `.gitignore` 中加入 `~$*` 和 `~$*.meta`。

## Phase 2 生成管线与诊断

1. **统一入口**
   - 入口改为 `LubanGenerator.RunAsync(profile, mode)`，mode 为 Generate 或 Validate。
   - 菜单不再写成 `async void` 业务，只调用这个入口并记录异常。
   - 新增菜单 `QGF/Validate Tables`，复用已有的 `validateOnly` 路径（`LubanGenerator.cs:87,127`）。
2. **结果模型**
   - 新增 `LubanGenerationResult`，包含以下字段：
     - Profile GUID、开始时间、耗时、是否成功
     - Luban 版本、dotnet 版本、SchemaHash
     - errors 和 warnings，每条含 category、code、message、file、location、fieldPath
     - 原始日志
   - 保存到 `Library/UnityQGameFramework/Luban/results/<profileGuid>.json`。
   - `LastLog` 属性保留以兼容旧代码。
3. **结构化诊断**（具体参数以 Phase 0.3 核实为准）
   - 加上 `--errorFormat json --locale zh`，解析 stderr 中的报告。
   - 每条错误调用 `Debug.LogError(msg, <对应 xlsx 资产>)`，最后再输出一条汇总。
   - 解析失败时回退为输出原始日志。
4. **进度与取消**：用 `UnityEditor.Progress` 显示进度。取消时结束 Luban 进程，并删除暂存目录。
5. **模板 v2**
   - 修改 `Editor/Luban/Templates~/cs-bin`，同步更新：
     - `luban-lock.json`：`templates.version` 改为 2
     - `Tools/Validation/luban_templates.py`
   - `tables.sbn` 生成 `public const string QgfSchemaHash`，`SchemaHash` 返回该常量。
   - 生成 `TableInfos`，覆盖所有表（含 list/one 和非 int 主键），每项包含：
     - 表名
     - 模式（map/list/one）
     - 主键类型、行类型
     - 计数委托、行枚举委托
   - 在 `Runtime/Unity/DataTable` 新增 `LubanTableInfo`。`LubanTableSet` 新增的成员一律为 `virtual` 并带默认值，保证旧生成代码仍能编译。
   - `table.sbn`：map 表增加 `bool TryGet(key, out value)`。
   - 视图命名（`tables.sbn:31-33`）：
     - 一个行类型只被一张表使用时，注册未命名视图，与现在相同。
     - 被多张表共用时，全部用表全名注册，不再让第一张表占用空名称。
     - 这种情况在生成结果中给出警告。
6. **源表指纹**
   - 生成时计算 Source 下文件的 SHA256（相对路径 + 内容，排除 `~$*` 和 `.meta`）。
   - 写入 `LubanDataTableCatalog` 新增的 `SourceHash` 字段。
   - 后续用它判断"源表已修改但未重新生成"。
7. **构建前检查**（`IPreprocessBuildWithReport`）
   - 对每个 Profile 的 `Data/Catalog.asset`，用 `TypeCache.GetTypesDerivedFrom<LubanTableSet>()` 找出 `QgfSchemaHash` 与之相同的编译类型：
     - 找不到 → 构建失败，并报出 Catalog 路径。
     - SourceHash 不一致 → 输出警告。Settings 中提供开关，可把这种情况视为错误。
   - 旧模板生成的代码没有该常量 → 跳过检查，并提示重新生成。
8. **schema 快照**：如果 Phase 0.3 确认支持，生成时同时输出 schema-json 到 `Library/UnityQGameFramework/Luban/schema/<profileGuid>.json`，供窗口展示。
9. **生成代码的程序集（D2）**
   - `LubanWorkspace.Create` 在工作区根目录写入 asmdef，引用：
     - `UnityQGameFramework.Core`
     - `UnityQGameFramework.Runtime`
     - `UnityQGameFramework.Luban`
   - 窗口对没有 asmdef 的工作区提供"添加程序集定义"按钮。
   - 用 `CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath` 判断，已位于某个 asmdef 内的工作区（如两个示例）不提示，也不嵌套创建。
   - 文档说明：Extensions 与生成代码在同一程序集，不能引用 Assembly-CSharp。

## Phase 3 运行时 API

1. **执行顺序**
   - 每个框架组件的具体类都显式加 `[DefaultExecutionOrder]`：Base 为 -1000，其余为 -900。
   - 测试用反射检查：每个 `FrameworkComponent` 的具体子类都带有 `DefaultExecutionOrderAttribute`，且值符合约定。
2. **`DataTableComponent`**（`Runtime/Unity/DataTable/DataTableComponent.cs`）
   - Event 和 Resource 组件改为调用时再获取，不在 Start 中缓存（`:38-42`）：
     - 业务脚本在 Awake 中即可同步加载。
     - 异步加载时如果 Resource 尚未就绪，抛出说明原因的异常。
   - 同步 `LoadDataTables<T>`（`:45-62`）：
     - 返回 `T`；失败时返回 null，并照常发送失败事件。
     - `Fail` 改用 `Log.Error` 输出完整异常（`:155-160`）。
   - 新增 `UniTask<T> LoadDataTablesAsync<T>(string catalogAssetName, Func<Func<string, ByteBuf>, T> factory, int priority = 0, CancellationToken ct = default)`：
     - 直接挂在资源回调上，不经过事件。
     - 取消时抛 `OperationCanceledException`，失败时抛异常。
     - 照常发送原有事件。
     - 组件销毁、Cancel 或 Unload 时，尚未完成的 Task 以取消结束。
   - 事件参数新增 `SerialId`。同步加载也分配序号并写入事件。XML 注释写明 `DataTableAssetName` 在同步和异步两种方式下的含义（`:61` 与 `:109`）。
   - 新增 `RegisterDataTables(IEnumerable<DataTableBase>)`，转发给 Core 管理器。
3. **查询便利方法**
   - Core 新增 `DataTableExtensions.TryGetDataRow` 和 `GetRequiredDataRow`，异常信息包含行类型、表名和 Id。
   - 组件新增 `TryGetDataRow<T>(int id, out T row)` 和 `GetRequiredDataRow<T>(int id)`；表不存在时同样给出明确信息。
4. **缺键语义**：写进 XML 注释和文档表格。
   - QGF 视图：`GetDataRow` 和索引器返回 null。
   - 原生表：`Get` 和索引器抛异常；`GetOrDefault` 和 `TryGet` 不抛。
5. **测试数据**
   - 用 Starter~ 生成一组测试数据（topModule 为 `UnityQGameFramework.Tests.Config`），代码、bytes 和 Catalog 放在 `PKG/Tests/Runtime/Fixtures`。
   - 重新生成步骤写在测试代码的注释里。
   - PlayMode 测试覆盖以下情况：
     - 同步加载、异步加载、取消
     - hash 不一致、尾随字节
     - 整批发布的原子性
     - 共享行类型的视图命名
     - TryGet 和 GetRequired

## Phase 4 编辑器体验

1. **`QGF > Luban` 窗口**（`Editor/Luban/LubanWindow.cs`）
   - 状态：
     - dotnet 的路径和版本
     - Luban 的版本、哈希和安装位置
     - Profile 的三个目录与 target 是否有效
     - 是否有 asmdef
     - 源表是否已修改未生成：在窗口获得焦点或点"刷新"时计算，不在每帧重绘时计算
     - Catalog 与编译代码的 hash 是否一致
   - 操作：
     - 生成、校验
     - 打开 Source、Generated、Data 目录
     - 编辑 luban.conf
     - 打开 `__tables__.xlsx`
   - 上次结果：错误和警告列表，点击后定位到对应 xlsx。
   - 表列表（来自 schema 快照）：表名、模式、主键、输入文件。
   - Settings 页（`LubanSettingsProvider.cs`）保留现有内容，并加入打开窗口的链接。
   - "存在多个 Profile"的警告（`LubanWorkspace.cs:44`）每个会话只提示一次。
2. **DataTable 运行时 Inspector**（`Editor/Inspector/DataTableComponentInspector.cs:42`）
   - 按 `TableInfos` 列出所有表及其 QGF 视图名。
   - 支持按 Id 搜索，用反射展示公共字段，保留分页。
   - 显示加载状态、最近一次错误，以及已加载 Tables 与 Catalog 的 hash。
3. **Profile Inspector**：显示表列表摘要、asmdef 状态和上次生成结果。
4. **文案约定**：编辑器 UI 用中文；运行时异常保持英文，并带上下文。写入 `Documentation~/03-conventions.md`。

## Phase 5 UGF 源码加固

1. **IL2CPP 裁剪**
   - 新增 `Editor/Build/QgfLinkerProcessor.cs`（实现 `IUnityLinkerProcessor`），为 Core、Runtime、Luban、Tomlyn 生成 preserve。
   - 按 Unity 手册，包内的 link.xml 不被处理。把 `Runtime/link.xml` 和 `Runtime/Unity/UI/link.xml` 的条目并入处理器生成的内容，再删除这两个文件。
   - 验收：在 `prepare_project.py` 生成的临时工程中，用 IL2CPP、Managed Stripping 为 High 构建 Windows Player，StarForce 能正常启动。
   - 未安装 IL2CPP 构建模块时，告知用户，不要在本工程改设置。
2. **静态状态重置**
   - Core 公开 `FrameworkEntry.ResetStatics()`。
   - Runtime 用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 调用它，并清理 `GameEntry`、ReferencePool 和 Utility 中的 helper。
   - 验收：在临时工程中关闭 Domain Reload，连续两次进入 Play，不出现重复注册错误。
3. **反射创建前先校验类型**：以下位置改为先用 `IsAssignableFrom` 检查，错误信息带上类型名。
   - `Runtime/Unity/Base/BaseComponent.cs:251,274,296,319,343`
   - `Runtime/Unity/Procedure/ProcedureComponent.cs:69-70`
   - `Runtime/Core/Base/FrameworkEntry.cs:103-104`
4. **Debugger 默认窗口（D5）**：同时修改 `DebuggerComponent.cs:45` 和预制体中的序列化值。在 PKG 和 `Assets/Examples` 的 `*.prefab` 中搜索 `m_ActiveWindow` 找出要改的位置，改示例预制体前先与 GPT 协调。
5. **其他**
   - 补全 `Runtime/Plugins/Tomlyn.dll.meta` 的 PluginImporter 设置。
   - package.json 移除未使用的 `com.unity.modules.uielements` 和 `com.unity.modules.unitywebrequestassetbundle`，移除前先 grep 确认没有引用。

## Phase 6 自动化

1. **可选的 CLI 命令程序集**：`Editor/Pipeline/UnityQGameFramework.Editor.Pipeline.asmdef`
   - `includePlatforms: ["Editor"]`，`autoReferenced: false`。
   - versionDefines：`com.unity.pipeline`，表达式 `[0.7.0-exp.1,0.8.0)` → `QGF_UNITY_PIPELINE`；defineConstraints 用同一个符号。实验包升级到 0.8 后重新验证，再放宽上限。
   - 引用 `UnityQGameFramework.Editor` 和 `Unity.Pipeline`。
   - 提供三个命令：`qgf_luban_status`、`qgf_luban_validate`、`qgf_luban_generate`。
     - 都接受可选参数 `--profile <资产路径>`，省略时使用全局 Profile。按 Profile 生成时不修改全局设置。
     - generate 是 async，等生成完成后返回 `LubanGenerationResult` 的 JSON。
     - 生成后的重编译会触发域重载，命令可能在返回前断开。结果以 Library 中的结果文件为准，重载后用 `qgf_luban_status` 读取。
   - 验收：
     - `unity command qgf_luban_generate` 返回 success 和错误列表，或断开后 `qgf_luban_status` 能读到同一结果。
     - 2022.3 下跳过该程序集，没有编译错误。
2. **`Tools/Documentation/check_docs.py`** 增加三项检查：
   - 文档中的 `QGF > …` 菜单路径存在于 `[MenuItem]`
   - 粗体按钮名存在于 Inspector 或窗口源码
   - 示例路径存在

## Phase 7 示例与文档

开始前先与 GPT 协调。

1. **用模板 v2 重新生成**以下三个工作区，确认 Catalog 与代码的 hash 一致，然后运行 `python Tools/Packaging/sync_samples.py`。用 `qgf_luban_generate --profile` 逐个生成，不切换全局 Profile：
   - `Assets/UnityQGameFramework/Config`
   - `Assets/Examples/01_Modules/11_DataTable/Common/Config`
   - `Assets/Examples/02_StarForce/UnityQGameFramework/Config`
2. **`11_DataTable`**
   - 01（`01_LoadCatalog/LoadCatalogLesson.cs:1,51`）：
     - 去掉 `#if UNITY_EDITOR` 和 AssetDatabase，改为序列化 Catalog 路径。
     - 事件用 SerialId 匹配。
     - 增加 `await LoadDataTablesAsync` 的写法。
     - 局部变量 `tables` 改名为 `dataTable`。
   - 02：改正 `QueryRowsLesson.cs:46` 关于缺键的注释；下拉选项旁显示实际调用的代码。
   - 03：加入 `TryGet`。
   - 04（`TableRegistry.cs:64-65`）：改用组件的 `RegisterDataTables`，类名改为 `TableRegistryLesson`。
   - 全部课程：
     - 改在 Awake 中加载，去掉 `UniTask.NextFrame` 等待。
     - `Common/Config/Extensions` 增加 DRItem 的 partial 示例。
     - asmdef、rootNamespace、topModule 统一命名。
3. **StarForce**
   - `ProcedurePreload.cs`：加载失败时显示错误，不再停在 Preload。
   - 修复 `AircraftData` 构造函数中途 return 导致的半初始化。
   - 修复三个武器槽全部填满时的越界。
   - 约 14 处"取表、取行、判空"改用 `TryGetDataRow`。
4. **文档**：每节按"最小可运行代码 → 运行结果 → 原因"组织。
   - `02-data-tables.md`
     - 核对并修正 `:107`、`:204`。
     - 加入缺键语义表、程序集说明。
     - 用异步示例替换 `:292-331`。
     - Profile 与 Catalog 的区别只在本页讲一次，其他页改为链接。
     - §11 的 TOML 和本地化内容移到 `Modules/config.md` 与 `Modules/localization.md`。
   - `01-getting-started.md`：更新快捷键说明；说明 .NET 版本要求与离线安装；入口表加入 `QGF > Luban` 和 `QGF > Logging`。
   - `Modules/data-table.md`：只保留一个加载流程，并链接到 11_DataTable。
   - `05-starforce.md:29,73`：修正。
   - 新增排错清单：dotnet、7z、下载失败、Excel 锁文件、hash 不一致、程序集引用。
5. **CHANGELOG 与版本**
   - 列出破坏性变化：
     - 同步加载有返回值
     - 共享行类型的视图命名
     - 卸载行为
     - 快捷键
     - UniTask 约束
     - 模板 v2 需要重新生成
   - 版本升到 `0.1.0-exp.5`，`check_package.py` 检查的三处保持一致。

## 验证（每个 Phase 结束时执行）

```
unity status
unity recompile                                   # 警告数不应多于基线
unity command console --level warn --tail 200
unity command run_tests --mode editor --filter UnityQGameFramework.Tests --filter_type assembly
unity command run_tests --mode playmode --async_tests true --filter UnityQGameFramework.Tests --filter_type assembly
unity command test_status
unity command menu --path "QGF/Generate Tables"   # Phase 6 后改用 unity command qgf_luban_generate
unity command wait_for --condition '{"member":"UnityQGameFramework.Editor.LubanGenerator.IsBusy","op":"equals","value":false}' --timeout_s 300
unity command recompile_status                    # 轮询到 completed 或 up_to_date；域重载期间连接会短暂失败
unity command console --level error
python Tools/Packaging/sync_samples.py
python Tools/Packaging/check_package.py
python Tools/Documentation/check_docs.py
```

- **场景冒烟**：
  1. 用 `unity command eval --code "UnityEditor.SceneManagement.EditorSceneManager.OpenScene(\"<场景路径>\"); return true;"` 打开场景。
  2. 依次执行 `editor_play`、`wait_for`、`console --level error`、`editor_stop`。
  3. 覆盖 11_DataTable 的 4 个场景和 StarForce。
- **PowerShell 引号**：如果 JSON 中的引号在 PowerShell 中被吞掉，改用 `eval` 轮询 `LubanGenerator.IsBusy`。
- **不改本工程设置**：验证结束后，`ProjectSettings/`（含 `UnityQGameFrameworkSettings.asset`）在 `git diff` 中应无变化。需要改设置的验证（IL2CPP 裁剪、关闭 Domain Reload、外部 UniTask）在 `Tools/Validation/prepare_project.py` 生成的临时工程中做。
- **2022.3**：pipeline 需要 Unity 6000.0 以上，无法连接 2022.3。
  - 用 `prepare_project.py` 建临时工程，尝试用 `unity run` 指定 `S:\Unity Editor\2022.3.62f3` 进行批处理编译。
  - 如果 CLI 不支持选择编辑器版本，停下来询问用户。

## 风险与应对

- **模板 v2 会改变所有工作区的 SchemaHash。** 仓库内三个工作区必须在同一次改动中重新生成。外部用户只要不重新生成，旧代码与旧 Catalog 依然一致；`LubanTableSet` 新成员是 virtual，旧代码能编译。
- **执行顺序和同步加载的语义变化可能影响示例时序。** Phase 7 的冒烟测试覆盖全部数据表示例和 StarForce。
- **com.unity.pipeline 是实验包。** CLI 命令层保持精简，放在独立的可选程序集中。
- **UniTask asmdef 偏离上游，升级 UniTask 时需要重新应用。** 已记录在 `unitask-source.json` 中。

## 后续（另行规划）

- Play Mode 数据热重载：只有数据变化时，同步 bytes 并重新发布表；结构变化时拒绝。需要新增 `ReloadDataTables()`。
- 资源路径校验：使用 Luban 的 path 校验器，并自动传入项目根目录。
- Unity 值类型映射：vector、color。
- 全站文档按 QFramework 的方式重组：5 分钟上手 → 跟着点点点逐步推进的教程 → 模块手册 → 原理 → 排错。
- QFramework Architecture 层与配置表的衔接，采用 ConfigModel 模式。
- 评估 SharpZipLib 的替换；收敛大型重载族与 `FrameworkAction`/`FrameworkFunc`。
- 评估把"安装时自动在 Assets 创建工作区"（`LubanWorkspace.cs:10-23`）改为在 Luban 窗口中引导创建。
