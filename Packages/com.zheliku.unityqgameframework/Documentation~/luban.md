# Luban 配置表

业务通过 QGF `DataTableComponent` 查询数据。Excel、schema、校验和二进制生成采用官方 Luban 5.1.0；框架中不再保留 UGF 的 TXT 读表器、行解析接口和旧表生成器。独立的 Config 模块使用 TOML，Localization 保留原格式；三者分工见 [配置文件说明](configuration.md)。

## 配表与生成

在 `Project Settings > Unity Q Game Framework > Luban` 中拖入“全局 Profile”；也可通过 `Unity Q Game Framework > Project Settings` 直达。DataTable 节点的 Inspector 同样提供 Profile 选择框，选好后点击“设为全局”，两处设置即时同步。项目级选择保存在 `ProjectSettings/UnityQGameFrameworkSettings.asset`，应随项目提交；通过 GUID 跟随 Profile 在 Assets 内移动。

执行 `Unity Q Game Framework > Generate Data Tables`（Ctrl/Cmd + Shift + G）一键生成，无需单独窗口。菜单和组件中的“生成全局配置”始终使用全局 Profile；在 Project 窗口选中其他文件或 Profile 不会改变目标。组件的运行时 Catalog 独立配置，Inspector 会提示与全局 Profile 的输出是否一致，可点击“绑定全局 Profile 的 Catalog”显式更新引用。

通过 Git URL 或其他 UPM 方式安装包并完成编译后，会自动初始化工作区：沿用已指定的全局 Profile，否则优先使用默认目录内已有的 Profile，或项目中唯一的 Profile；没有任何 Profile 时自动创建下列默认目录。存在多个非默认方案时要求在项目设置中明确选择；已指定的 Profile 丢失时不会自动改用其他方案。此过程不下载编译器、不生成代码，也不更改场景。旧版 EditorPrefs 中的有效选择会自动迁移一次。

项目设置中的“新工作区目录”默认值为 `Assets/UnityQGameFramework/Config`，用户可以改到 Assets 中的其他位置，再点击“创建或使用该目录的工作区”。该设置不移动已有文件。Profile 的 Inspector 则可分别设置源表、生成代码、二进制目录与 target。默认目录：

```text
Assets/UnityQGameFramework/Config/
  Source/luban.conf
  Source/Defines/                # 可选 XML schema
  Source/Datas/__tables__.xlsx   # 表名、输入、索引、模式
  Source/Datas/__beans__.xlsx    # 复用结构类型（初始仅标题）
  Source/Datas/__enums__.xlsx    # 枚举类型（初始仅标题）
  Source/Datas/Item.xlsx
  Generated/                    # 工具生成的 C#，勿手改
  Data/                         # 工具生成的 .bytes 和 Catalog.asset
  Extensions/                   # 手写 partial 扩展
  Editor/LubanProfile.asset      # 生成设置，Player 不引用
```

默认采用官方 Excel schema：`luban.conf.schemaFiles` 分别登记三份 XLSX 的 `table` / `bean` / `enum` 类型。普通表在 `__tables__` 中设置 `read_schema_from_file=true`，直接从数据工作表的 `##var`、`##type`、`##group`、`##` 标题行读取行结构；无需再在 `__beans__` 重复声明同一行。复杂的复用结构、枚举分别填入后两份工作簿。`Defines` 仅保留为可选 XML 扩展目录，使用时需在 `schemaFiles` 登记。指定输入工作表的写法是 `SheetName@File.xlsx`。参考 [官方 Excel schema](https://www.datable.cn/docs/schema/excel-schema)。

生成的 `Tb*` 来自 `__tables__.xlsx` 的声明。例如 `TbEntity / DREntity / Entity@StarForce.xlsx / Id / map`：`DREntity` 表示一条记录，`TbEntity` 保存整表及索引，`Tables` 汇总所有表。11 个数据工作表对应 11 个 DR 类型、11 个 Tb 容器和一个 Tables 入口；三份 schema 工作簿不会额外生成游戏数据表。

Profile 管理源表／代码／数据目录及 target 选择。命名空间写在 `Source/luban.conf` 对应 target 的 `topModule`，总入口类名写在 `manager`，导出分组写在 `groups`。Profile Inspector 展示这些值，并提供“打开 luban.conf”和“定位 Excel 表定义”按钮；不会在 Profile 中再存一份易失步的参数。修改命名空间后需同步业务 using、手写 partial 扩展及引用类型，再重新生成。代码与数据格式当前固定为 `cs-bin` / `bin`。

生成需要 .NET 8 或兼容运行时；首次生成时若缺少编译器，会从官方发布地址安装固定版本并校验 SHA-256。工具仅缓存于 `Library/UnityQGameFramework/Tools/Luban/5.1.0`。离线环境可通过 `Unity Q Game Framework > Luban > Select Local Compiler` 选择已解压的 `Luban.dll`；`Use Downloaded Compiler` 恢复使用下载缓存。本机路径保存在 EditorPrefs，不会写进项目资产。生成器和运行时的版本分别追踪，见 [第三方声明](../Third%20Party%20Notices.md)。

项目设置同时显示当前编译器路径、固定版本和进程超时。外部进程默认 300 秒，允许按大型配置工程的需要调整为 10–3600 秒；该超时与本地编译器路径仅保存在本机。下载单独使用 120 秒超时。生成期间锁定程序集重载；成功或失败都会解除生成状态。

Excel 和 `luban.conf` 直接在 Project 窗口定位并打开，生成进度和错误见 Console。DataTable Inspector 可定位源表、查看 Catalog 的版本与表文件；进入 Play Mode 后可按表浏览、筛选和分页查看记录。生成使用临时输出目录，Luban 校验成功后才同步；只覆盖和删除 Profile 记录的生成文件，保留 `.meta`。手写文件放在 Extensions，工具不会清空整个目录。

同步前备份此次可能变更的生成文件、`.meta`、Profile 和 Catalog。文件写入或导入阶段抛出异常时恢复原状态；恢复也失败则保留 `Library/UnityQGameFramework/Luban/Staging/<操作 ID>/Backup` 及 `manifest.json`，错误消息会给出位置。这是可处理异常的恢复机制，不承诺在操作系统崩溃／断电时完成自动回滚；源表与生成物仍应提交版本控制。

将表源、生成代码、二进制、Catalog、Profile 和 `.meta` 一起提交版本控制。缓存、临时日志和工具不提交。预生成的示例可直接运行，无需安装生成器。

生成文件的归属绑定输出目录 GUID。通过 Project 移动目录会继续识别原有生成物；在 Profile 中重新指定另一个目录时，只生成到空位置，不继承覆盖该目录既有文件或 Catalog 的权限。

## 加载与查询

生成代码所在程序集引用 `UnityQGameFramework.Core`、`UnityQGameFramework.Runtime`、`UnityQGameFramework.Luban`。默认 Assembly-CSharp 自动引用这些程序集。框架包不引用业务生成代码，传入生成的 Tables 构造函数建立连接：

```csharp
// 把 Data/Catalog.asset 拖入已有 DataTable 节点的 Luban Catalog 字段。
// 在各组件 Start 之后（如预加载流程）调用。
GameEntry.DataTable.LoadDataTables(loader => new Tables(loader), this);

// 完整表组发布后触发 LoadDataTableSuccessEventArgs；UserData 为上面的 this。
IDataTable<DRItem> items = GameEntry.DataTable.GetDataTable<DRItem>();
DRItem item = items.GetDataRow(1); // 不存在时返回 null
```

`HasDataTable`、`GetDataTable`（泛型 / Type / name）、`GetAllDataTables`、Count、Id 索引、最小／最大 Id、枚举、谓词筛选、排序及 List 输出重载继续可用。视图直接持有 Luban 的 DataMap，返回同一条生成记录，不复制行对象。

这里的“行”是业务记录，以配置的 Id 查询，不是 Excel 的物理行号。表可枚举，因此也能使用 LINQ 做 Where / Select / OrderBy / GroupBy / Sum。复杂索引、跨表引用、list / one 表及字段中的数组／结构体，使用下方原生入口与 Luban schema；不需要把所有数据强行改成 int Id 行表。

兼容查询要求 map 表的行是 class，主键是 `int Id`（字段可用 Luban 的大小写转换）。同一种行类型用于多个表时，第一张使用默认空名称，其余使用完整表名作为 name。字符串键、值类型行、复合索引、单例、列表等其他 Luban 表仍可生成，通过同一组件的原生入口访问：

```csharp
var tables = GameEntry.DataTable.GetLubanTables<Tables>();
// tables.TbXxx.Get(...) / DataList / 生成的引用属性
```

数据表是配置快照：`IDataRow` 只保留 Id；旧 `ParseDataRow`、`ReadData`、TXT / 旧二进制行解析、Add/RemoveDataRow、缓存辅助器及其进度／依赖事件已移除。不存在伪装成功的旧解析路径。`CreateDataTable` 保留创建空视图的调用形式；自定义完整视图可使用 `DataTable<T>(name, readOnlyDictionary)`，经 `IDataTableManager.RegisterDataTables` 整批注册。一般业务直接加载生成的 Tables，无需手动建表。

## 资源、错误与生命周期

Inspector 中直接引用 Catalog 的方式同步解析已随场景加载的 bytes，适合小项目和示例。Catalog 只引用生成二进制，绝不引用 Excel、schema 或 Profile；源表在 Assets 不代表会进入 Player。

使用资源包或可更新配置时，资源集合只需登记 Catalog；其 bytes 作为 Unity 序列化依赖随包构建，避免增删表时维护逐文件清单。无需将 Excel、schema 或 Profile 加入资源集合。按资源路径异步加载：

```csharp
int request = GameEntry.DataTable.LoadDataTables<Tables>(
    catalogAssetPath, loader => new Tables(loader), this);
// 可以取消；晚到的资源仍经过 ResourceComponent.UnloadAsset 释放。
GameEntry.DataTable.CancelLoadDataTables();
```

一个组件同时只加载一个表组。先构造所有表、解析引用并检查生成版本，全部成功后整批注册；失败触发 `LoadDataTableFailureEventArgs`，不暴露部分表。重复加载前调用 `UnloadDataTables()`，它取消在途请求并释放原生表组引用。单张 DestroyDataTable 仅移除查询视图；整组卸载才释放 Tables 持有的跨表引用。业务持有的数据行引用仍由业务自行释放。

不要修改只读包缓存。框架实现、模板和最小 Luban runtime 在 Packages；可编辑的表源、生成物、Profile 与业务扩展在 Assets。使用 Project 窗口移动这些目录并保留 `.meta`，Profile 的目录引用、Catalog 对 bytes 的引用、场景对 Catalog 的引用随之更新。源码和 bytes 的输出目录也可分别重新指定，旧位置不会被自动清空。生成时避免通过文件管理器移动目录；嵌套在 Source 内的自定义路径仍按 Luban 相对路径规则调整。

StarForce 的完整工作区位于示例自身的 `UnityQGameFramework/Config`，随 Samples 导入一起复制，可直接检查 11 张真实表。调试 StarForce 时使用这里的 Profile；Launcher 的 DataTable 节点引用这里的 Catalog。`Assets/UnityQGameFramework/Config` 是业务项目的默认工作区，两者各自独立，没有目录优先级；生成目标由 Profile 决定，运行时数据由 DataTable 节点的 Catalog 引用或加载路径决定。其 Entity、UI、Sound 等调用继续通过 DataTableComponent。

## XLSX Diff 兼容

已将仓库工作簿的 SpreadsheetML 元素统一为默认命名空间形式。原先带 `x:` 前缀的写法也是有效 OOXML，但本机 XLSX Diff 1.6.4 的 fastxlsx 读取器使用字面标签匹配，读不到工作表。`Tools/Validation/normalize_xlsx_namespaces.py` 可规范化后续生成的工作簿：逐 XML 验证元素、属性和单元格内容的语义一致，并校验 ZIP CRC，不修改业务数据或 GUID。其他开发者读取规范化后的文件无需安装额外补丁。

本机扩展另外添加了读取兼容补丁，使 Git 历史中的旧格式也可比较。原扩展备份在 `Artifacts~/XlsxDiff/extension-1.6.4.original.cjs`；通过 VS Code 的 **Developer: Reload Window** 加载。扩展升级可能覆盖此本机补丁。

## 文件夹菜单

`Unity Q Game Framework > Open Folder` 区分 Unity 路径和当前全局 Profile 的路径：

| 菜单 | 实际目录 |
| --- | --- |
| Assets (Application.dataPath) | 当前工程的 Assets；这是 Unity 的 dataPath 含义 |
| Persistent Data (Saves) | Application.persistentDataPath，存档目录 |
| Streaming Assets | Application.streamingAssetsPath |
| Temporary Cache | Application.temporaryCachePath |
| Editor Logs | Application.consoleLogPath 所在目录 |
| Luban Sources (Active Profile) | 全局 Profile 的 SourceFolder |
| Luban Generated Code (Active Profile) | 全局 Profile 的 CodeFolder |
| Luban Data (Active Profile) | 全局 Profile 的 DataFolder，包含 bytes 和 Catalog |

目录不存在时只提示，不创建空文件夹或误开其他位置。Luban 目录未配置时对应入口禁用。
