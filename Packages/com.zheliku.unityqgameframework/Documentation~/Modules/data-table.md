# DataTable 与 Luban

QGF 直接登记 Luban 生成的表对象。场景中的 DataTableComponent、Profile、Catalog 和资源加载流程继续使用，旧 TXT 解析器和 int Id 行接口已移除。

## 加载和查询

将生成的 Catalog 指定给 DataTableComponent，在启动流程中加载一次：

```csharp
using UnityQGameFramework.Examples.TableData;

var tables = QGF.DataTable.LoadDataTables(loader => new Tables(loader));
if (tables == null) return;

// 泛型参数是表类型，返回的就是生成的 TbItem。
var items = QGF.DataTable.GetDataTable<TbItem>();
var item = items.GetDataRow(1);
```

表构造、跨表引用解析和结构版本校验全部完成后，组件才登记整组表。普通查询不触发文件读取；运行时不读取 Excel，也不运行 Luban 编译器。

## 键与表模式

| schema 定义 | 查询方式 |
| --- | --- |
| map，整数、字符串、长整数或枚举键 | `table.GetDataRow(key)`、`table.HasDataRow(key)` |
| list，复合索引 | 生成 `table.GetDataRow(key1, key2, ...)` |
| list，多个独立索引 | 生成 `table.GetByXxx(key)` |
| 无主键 list | `DataList`、枚举、条件查询 |
| one | `Data`；`Count` 为 1 |
| 跨表引用 | 直接读取生成的引用属性 |

所有表统一使用 `GetDataTable<TbXxx>()`。`DataMap`、`DataList`、生成索引与 QGF 公共查询都在同一个表对象上，记录不复制。one 表字段与元数据重名时用 `Data` 访问，例如 `rules.Data.Count`；`rules.Count` 始终表示行数。

`GetDataRow(key)` 缺键时返回 default：class 为 null，struct 为默认值。Luban 原生 `Get`、`GetOrDefault` 和索引器保留各自的行为。

## 筛选与排序

```csharp
var affordable = items.GetDataRows(row => row.Price <= 100);
var sorted = items.GetDataRows(row => row.Price <= 100,
    (a, b) => a.Price.CompareTo(b.Price));
```

条件查询遍历记录。高频查找应在 schema 中定义索引。排序只改变结果数组或调用方提供的 `List<T>`，不改变 `DataList` 的源表顺序；列表重载会先清空结果列表。

同一行结构可用于多张表，每张表用自己的 Tb 类型登记。手动构造同一种表的额外实例时可传入 name，查询使用 `GetDataTable<TbXxx>(name)`。

## 异步加载和卸载

资源加载仍由 Resource 负责。先订阅 `LoadDataTableSuccessEventArgs` 和 `LoadDataTableFailureEventArgs`，再调用：

```csharp
int requestId = QGF.DataTable.LoadDataTables(
    catalogAssetName, loader => new Tables(loader), this);
```

这个重载返回请求序号，结果通过 Event 通知。`CancelLoadDataTables()` 使在途请求失效，迟到回调会释放资产，不发布表和结果事件。

`IsLoading` 表示正在加载，`IsLoaded` 表示整组表已发布，`Count` 包括所有已登记的 map、list、one 表。`UnloadDataTables()` 取消在途请求并移除该组表的登记；业务保存的旧表和记录引用应在切换配置时更新。

`DestroyDataTable<TbXxx>()` 只移除登记，不改写表对象及其跨表引用。`RegisterDataTables` 支持手动构造的表，整批冲突时不登记。

## 与 IDataProvider 的边界

`IDataProvider<T>` 保留给 Config 和 Localization。它适合“读取一份资源、交给一个 Helper 解析、发布读取事件”的流程，因此可以同时支持文本、字节片段、进度和依赖资源。

Luban 表不是这个模型：一次加载先读取 Catalog，再按 Catalog 读取多份 bytes，由生成的 `Tables` 构造所有 `TbXxx`，解析跨表引用，校验 schema 版本，最后原子登记整组表。它没有独立有意义的 `ParseData(string)`，也不能把一份 Catalog 的完成事件等同于单个文本资源的完成事件。

因此当前不新增 `LubanTableProvider : IDataProvider<T>`。`DataTableComponent` 就是 Luban 的加载与发布边界；`TbXxx` 是运行时表对象。Config、Localization 和 DataTable 仍然通过各自清晰的组件接口协作，不共享不匹配的解析抽象。

将来接入 Addressables、远程热更或自定义文件系统时，再抽取窄接口 `ILubanTableSource`，只提供 Catalog 和 bytes 的读取；表构造、引用解析、版本校验、登记和卸载仍由 DataTableComponent 统一完成。这样不会改变现有调用，也不会复制 `IDataProvider` 的整套文本解析 API。

## 生成与迁移

Profile 决定源目录、target、namespace 和输出位置，组件上的 Catalog 决定运行时数据。schema 或生成模板改变后，应重新生成并一起提交 Excel、生成的 C#、bytes、Catalog 和 Profile。

旧调用的泛型参数需要从行类型改为表类型；例如旧的 `GetDataTable<DRItem>()` 应改为 `GetDataTable<TbItem>()`。独立的 GetLubanTables 查询入口已移除。

Config 和 Localization 使用 TOML，依赖共享的数据提供器与 Resource、Event，不依赖 DataTable。结构化配置使用 Luban，少量参数和翻译字典仍由各自模块管理。

源码：[DataTableComponent](../../Runtime/Unity/DataTable/DataTableComponent.cs)、[DataTable](../../Runtime/Core/DataTable/DataTableManager.DataTable.cs)、[Catalog](../../Runtime/Unity/DataTable/LubanDataTableCatalog.cs)。
