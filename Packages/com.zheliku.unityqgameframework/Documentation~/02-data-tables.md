# Luban 数据表

先运行模块 Sample 的 [01_LoadTables](../Samples~/01_Modules/11_DataTable/01_LoadTables/LoadLubanTablesExample.cs)。它带有完整生成结果，展示“启动时加载一次，业务按需查询”。

## 编辑与生成

默认工作区：

```text
Assets/UnityQGameFramework/Config/
  LubanProfile.asset
  luban.conf
  Source/
    __tables__.xlsx
    __beans__.xlsx
    __enums__.xlsx
    Item.xlsx
  Generated/              生成的 C#
  Data/                   bytes 与 Catalog.asset
```

`__tables__` 登记表，`__beans__` 定义复用结构，`__enums__` 定义枚举；Item.xlsx 同时保存字段定义与数据。后两份 schema 可只保留标题，未使用时不必填写。

`__tables__.xlsx` 的最小登记：

| full_name | value_type | read_schema_from_file | input | index | mode |
| --- | --- | --- | --- | --- | --- |
| TbItem | DRItem | 1 | Item@Item.xlsx | Id | map |

`工作表名@文件名.xlsx` 的工作表名必须与页签一致。Item 工作表示例：

| A | B | C | D |
| --- | --- | --- | --- |
| ##var | Id | Name | Price |
| ##type | int | string | int |
| ##group | c | c | c |
| ## | 编号 | 名称 | 价格 |
| | 1 | Potion | 20 |
| | 2 | Energy | 50 |

保存 Excel，在 `QGF > Settings` 选择 Profile，执行 `QGF > Generate Tables`。首次生成自动下载固定的官方 Luban 编译器到 Library，需要 .NET 8 或兼容运行时。之后改表仍用同一菜单。

生成得到 `DRItem`（记录）、`TbItem`（整张表）、`Tables`（整组加载器）、bytes（数据）和 Catalog（文件清单与结构版本）。不要手改生成代码。

## 加载与查询

将 `Data/Catalog.asset` 赋给 DataTableComponent，在组件启动后的业务 Start 或入口流程调用：

```csharp
// Tables 使用本工作区生成代码的命名空间。
if (QGF.DataTable.LoadDataTables<Tables>())
{
    var items = QGF.DataTable.GetDataTable<TbItem>();
    var required = items[1];                  // 确定存在；缺键抛异常。
    var optional = items.GetDataRow(999);     // 允许缺键；class 返回 null。
}
```

不需要手动 Initialize。`LoadDataTables` 负责构造整组表、解析跨表引用、校验版本并登记；`GetDataTable` 只查询登记，不会隐式加载。数据值来自 bytes，生成 C# 负责解析，两者缺一不可。

```csharp
var items = QGF.DataTable.GetDataTable<TbItem>();
var first = items.GetDataRow(row => row.Price <= 20);
var matches = items.GetDataRows(row => row.Price <= 20);
var sorted = items.GetDataRows((a, b) => a.Price.CompareTo(b.Price));

// results 由业务预先创建并复用；方法清空后填入原记录引用。
items.GetDataRowsNonAlloc(row => row.Price <= 20, results);
```

数组查询会创建结果容器。NonAlloc 使用调用方列表，容量不足仍会扩容；复用列表与谓词后适合高频查询。表也支持 foreach 与 LINQ，频繁按键查大表时优先在 schema 定义索引。

| 表模式 | 查询方式 |
| --- | --- |
| map | 按实际主键生成索引器、GetDataRow(key)、HasDataRow(key)，支持 int、string、long、enum |
| list + 复合索引 | 复合键索引器与 GetDataRow(key1, key2, ...)，多个独立索引保留生成的 GetByXxx |
| list 无索引 | DataList、枚举与条件查询 |
| one | Data 取得唯一记录，Count 始终表示行数 |

键来自 schema，不是 Excel 物理行号。`GetDataRow(key)` 缺键时返回默认值，class 为 null，struct 为默认结构；索引器与原生 Get 保留 Luban 的缺键行为。统一从 `GetDataTable<TbXxx>()` 取得生成表，不再提供另一套表集访问入口。

## 通过资源加载 Catalog

将 Catalog 收集进 YooAsset；bytes 作为它的依赖一起构建，不必逐张登记。配置好 ResourceComponent 后：

```csharp
await QGF.DataTable.LoadDataTablesAsync<Tables>(
    "Tables_Catalog", cancellationToken: cancellationToken);
var items = QGF.DataTable.GetDataTable<TbItem>();
```

失败抛异常，取消抛 OperationCanceledException。事件方式使用 `LoadDataTables<Tables>(address, userData)`，先订阅 Runtime 的 LoadDataTableSuccessEventArgs / LoadDataTableFailureEventArgs，再发起加载；按 UserData 区分请求。

同一组件持有一组表，切换前调用 `UnloadDataTables()`，同时丢弃业务保存的旧表与旧记录引用。它取消在途请求并移除登记，晚到的资源回调仍会释放资产。`RemoveDataTable<TbXxx>()` 只移除对应登记；`Count` 是登记数，`IsLoading` 是资源请求状态，`IsLoaded` 是整组表已发布状态。

## Profile 与 Catalog

| 对象 | 用途 | 运行时读取 |
| --- | --- | --- |
| LubanProfile | 配置文件、target、代码与数据输出目录 | 否 |
| 全局 Profile | 本次菜单生成使用哪份 Profile | 否 |
| Catalog | bytes 引用清单、结构版本 | 是 |

全局 Profile 在 `QGF > Settings` 选择，也可在 DataTable Inspector 选择后点击 **Set Global**。Catalog 由场景组件指定，可点击 **Use Global Catalog**；改变全局生成目标不会自动改变场景数据。

命名空间与生成入口名在 `luban.conf`：

```json
"targets": [{
  "name": "client",
  "manager": "Tables",
  "groups": ["c"],
  "topModule": "MyGame.Config"
}]
```

Profile.Target 对应 name。topModule 决定命名空间，manager 决定加载器名称，groups 决定导出分组；多个工作区用不同命名空间。改后重新生成，并更新业务 using。

生成覆盖本 Profile 登记的文件、删除不再生成的旧文件并保留 `.meta`；陌生的同名输出会拒绝覆盖。提交 Excel、luban.conf、Profile、Generated、Data 和对应 `.meta`，结构变化时 C#、bytes、Catalog 一起提交。

移动工作区时在 Unity Project 窗口移动整个目录。单独移动 Source 时更新 luban.conf 的 dataDir 和 schemaFiles；单独移动输出时更新 Profile。源表、代码、数据目录相互独立，手写 partial 放在 Generated 外。源表不要放在 Resources、StreamingAssets 或收集规则中。

Config 负责少量全局参数，Localization 负责翻译，二者使用 TOML，均不依赖 DataTable。具体见 [Config](Modules/config.md) 和 [Localization](Modules/localization.md)。
