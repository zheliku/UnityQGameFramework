# DataTable

使用 Luban 生成的强类型表，统一通过 QGF.DataTable 加载与查询。

```csharp
if (QGF.DataTable.LoadDataTables<Tables>())
{
    var items = QGF.DataTable.GetDataTable<TbItem>();
    var item = items[1];
}
```

DataTableComponent 自动初始化，LoadDataTables 从 Inspector 的 Catalog 同步加载；按资源地址加载使用 LoadDataTablesAsync。Tables、TbItem 来自当前工作区的生成命名空间。索引器保留实际键类型，允许缺键时用 GetDataRow。完整生成、目录和查询说明见 [Luban 数据表](../02-data-tables.md)。

[运行示例与完整脚本](../../Samples~/01_Modules/11_DataTable/)。

源码：[组件](../../Runtime/Unity/DataTable/DataTableComponent.cs)。
