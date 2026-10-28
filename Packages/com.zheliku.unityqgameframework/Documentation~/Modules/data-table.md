# DataTable 与 Luban

QGF 直接登记 Luban 生成的表对象。场景中的 DataTableComponent、Profile、Catalog 和资源加载流程继续使用，旧 TXT 解析器和 int Id 行接口已移除。

## 加载和查询

不需要手动 `Initialize`。DataTableComponent 在 `Awake` 取得管理器，在 `Start` 连接 Event 和 Resource；框架组件的执行顺序早于普通业务脚本，所以通常在业务 `Start` 或后续启动流程中加载。不要在 `Awake`/`OnEnable` 发起加载；运行时新建框架时也要先让组件完成 `Start`。

将生成的 Catalog 指定给 DataTableComponent，在启动流程中加载一次：

```csharp
using UnityQGameFramework.Examples.Modules.TableData;

if (!QGF.DataTable.LoadDataTables<Tables>())
{
    return;
}

// 泛型参数是表类型，返回的就是生成的 TbItem。
var items = QGF.DataTable.GetDataTable<TbItem>();
var item = items.GetDataRow(1);
```

表构造、跨表引用解析和结构版本校验全部完成后，组件才登记整组表。普通查询不触发文件读取；运行时不读取 Excel，也不运行 Luban 编译器。

`Tables` 是生成的加载器，实现 `IDataTableLoader`，仅在加载时构造。它不作为业务数据入口返回或保存；加载后统一用 `QGF.DataTable.GetDataTable<TbXxx>()`。生成的 `TbXxx` 直接继承 QGF 的 `DataTable<T>`，没有额外的表视图或 Luban 查询管理器。

Catalog 是 QGF 生成的 ScriptableObject，不是 Luban 自带的功能。它保存文件名、bytes 资产引用和结构版本，方便 Unity 跟随目录移动、收集资源依赖并校验代码与数据。生成 C# 只包含类型和解析逻辑，具体记录仍保存在 bytes 中。

Catalog 是运行时数据清单，不是缓存。`LubanProfile` 只供编辑器生成，`QGF > Settings` 的全局 Profile 指定生成菜单的目标；DataTableComponent 的 Catalog 指定同步加载的数据。切换全局 Profile 不会更换组件的 Catalog。两者应配合同一套生成代码使用，结构版本不一致时加载会失败。

同步加载只需要 DataTable、Event 和组件上的 Catalog；不需要先初始化 YooAsset 包。`GetDataTable` 不会触发加载，只有 `LoadDataTables` 成功后才能查询。实际项目由启动流程加载完毕后进入业务流程，避免依赖多个普通脚本 `Start` 的先后顺序。可直接运行的完整说明见 [模块 Sample README](../../Samples~/01_Modules/README.md#datatable-从哪里开始)。

本地 GGF 的 `ConfigSystem.Tables` 看起来能直接读取，是因为属性首次访问时会调用 `Load()`，内部仍然构造 `Tables` 并读取 bytes。QGF 在启动流程显式加载，便于处理资源包异步下载、失败和取消；后续查询不需要重复加载。

## 键与表模式

| schema 定义 | 查询方式 |
| --- | --- |
| map，整数、字符串、长整数或枚举键 | `table[key]`、`table.GetDataRow(key)`、`table.HasDataRow(key)` |
| list，复合索引 | 生成 `table[(key1, key2, ...)]`、`table.GetDataRow(key1, key2, ...)` |
| list，多个独立索引 | 生成 `table.GetByXxx(key)` |
| 无主键 list | `DataList`、枚举、条件查询 |
| one | `Data`；`Count` 为 1 |
| 跨表引用 | 直接读取生成的引用属性 |

所有表统一使用 `GetDataTable<TbXxx>()`。`DataMap`、`DataList`、生成索引与 QGF 公共查询都在同一个表对象上，记录不复制。one 表字段与元数据重名时用 `Data` 访问，例如 `rules.Data.Count`；`rules.Count` 始终表示行数。

[表模式示例](../../Samples~/01_Modules/11_DataTable/06_TableModes/TableModesExample.cs) 使用官方 Excel schema 展示上述用法。`__tables__.xlsx` 显式登记每张表，`DataTableModes.xlsx` 保存别名、关卡、规则和提示。`TbItemIndex` 复用 `Item.xlsx` 的行结构，并增加 Name、Price 两个独立索引；它是在 schema 中声明的另一张表，不是额外的查询管理器。

`table[key]` 缺键时抛出 `KeyNotFoundException`，适合确认键一定存在的路径；`GetDataRow(key)` 缺键时返回 default：class 为 null，struct 为默认值。Luban 原生 `Get`、`GetOrDefault` 和索引器保留各自的行为。没有主键的 list 表和 one 表不生成索引器，分别使用 `DataList` 和 `Data`。

索引器直接定义在生成的具体表中，键类型由 schema 决定。`DataTable<T>` 和 `IDataTable<T>` 只保留所有表通用的遍历、筛选和排序，不再增加带键类型的接口或基类。

```csharp
// 已知键存在时，直接按键取得原表记录；跨表引用也指向同一行对象。
var item = QGF.DataTable.GetDataTable<TbItem>()[1];
var alias = QGF.DataTable.GetDataTable<TbItemAlias>()["Potion"];
var stage = QGF.DataTable.GetDataTable<TbStage>()[(1, 2)];
var relatedItem = alias.ItemId_Ref;

// 用户输入可能没有对应记录，使用返回 null 的查询。
var optionalItem = QGF.DataTable.GetDataTable<TbItem>().GetDataRow(inputId);
```

## 筛选与排序

```csharp
var affordable = items.GetDataRows(row => row.Price <= 100);
var sorted = items.GetDataRows(row => row.Price <= 100,
    (a, b) => a.Price.CompareTo(b.Price));
var reusable = new System.Collections.Generic.List<DRItem>();
items.GetDataRowsNonAlloc(row => row.Price <= 100,
    (a, b) => a.Price.CompareTo(b.Price), reusable);
```

条件查询遍历记录。高频查找应在 schema 中定义索引。排序只改变结果数组或调用方提供的 `List<T>`，不改变 `DataList` 的源表顺序；`NonAlloc` 方法会先清空结果列表，复用容量和委托时可避免这条查询路径的 GC 分配，列表容量不足时仍会扩容。

同一行结构可用于多张表，每张表用自己的 Tb 类型登记。手动构造同一种表的额外实例时可传入 name，查询使用 `GetDataTable<TbXxx>(name)`。

表类型在运行时才能确定时，可使用 `HasDataTable(Type, name)`、`GetDataTable(Type, name)` 和 `RemoveDataTable(Type, name)`。`GetAllDataTables()` 返回数组；`GetAllDataTablesNonAlloc(List<DataTableBase>)` 先清空再填充传入的列表，适合重复刷新工具面板。

## 异步加载和卸载

资源加载仍由 Resource 负责。先订阅 `LoadDataTableSuccessEventArgs` 和 `LoadDataTableFailureEventArgs`，再调用：

```csharp
int requestId = QGF.DataTable.LoadDataTables<Tables>(catalogAssetName, this);
```

这个重载返回请求序号，结果通过 Event 通知。`CancelLoadDataTables()` 使在途请求失效，迟到回调会释放资产，不发布表和结果事件。

带 `catalogAssetName` 的重载按资源地址读取 Catalog，不使用组件上的 Catalog；调用返回时不能立即假定表已就绪。它需要 ResourceComponent 的资源配置和 YooAsset 收集规则，资源模块会在加载时初始化对应包。

使用 UniTask 时可等待 `QGF.DataTable.LoadDataTablesAsync<Tables>(catalogAssetName, cancellationToken: token)`；完成后仍通过 `GetDataTable<TbXxx>()` 查询。失败抛出异常，取消抛出 `OperationCanceledException`。

`IsLoading` 表示正在加载，`IsLoaded` 表示整组表已完成加载，`Count` 包括所有已加入管理器的 map、list、one 表。`UnloadDataTables()` 取消在途请求并移除该组表；业务保存的旧表和记录引用应在切换配置时更新。

`RemoveDataTable<TbXxx>()` 只移除登记，不改写表对象及其跨表引用。`AddDataTables` 支持手动构造的表，整批冲突时不登记。

## 与 IDataProvider 的边界

`IDataProvider<T>` 保留给 Config 和 Localization。它适合“读取一份资源、交给一个 Helper 解析、发送读取事件”的流程，因此可以同时支持文本、字节片段、进度和依赖资源。

Luban 表不是这个模型：一次加载先读取 Catalog，再按 Catalog 读取多份 bytes，由生成的 `Tables` 构造所有 `TbXxx`，解析跨表引用，校验 schema 版本，最后原子登记整组表。它没有独立有意义的 `ParseData(string)`，也不能把一份 Catalog 的完成事件等同于单个文本资源的完成事件。

因此当前不新增 `LubanTableProvider : IDataProvider<T>`。`DataTableComponent` 就是 Luban 的加载与加入管理器边界；`TbXxx` 是运行时表对象。Config、Localization 和 DataTable 仍然通过各自清晰的组件接口协作，不共享不匹配的解析抽象。

Catalog 的资源加载使用 QGF.Resource 的 YooAsset 后端；编辑器模拟、离线和远程模式共用同一套表解析与登记流程。

## 生成与迁移

Profile 决定源目录、target、namespace 和输出位置，组件上的 Catalog 决定运行时数据。schema 或生成模板改变后，应重新生成并一起提交 Excel、生成的 C#、bytes、Catalog 和 Profile。

旧调用的泛型参数需要从行类型改为表类型；例如旧的 `GetDataTable<DRItem>()` 应改为 `GetDataTable<TbItem>()`。独立的 GetLubanTables 查询入口已移除。

Config 和 Localization 使用 TOML，依赖共享的数据提供器与 Resource、Event，不依赖 DataTable。结构化配置使用 Luban，少量参数和翻译字典仍由各自模块管理。

源码：[DataTableComponent](../../Runtime/Unity/DataTable/DataTableComponent.cs)、[DataTable](../../Runtime/Core/DataTable/DataTable.cs)、[Catalog](../../Runtime/Unity/DataTable/LubanDataTableCatalog.cs)。
