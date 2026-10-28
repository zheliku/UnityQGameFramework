# DataNode

用路径组织内存中的临时共享值。

```csharp
QGF.DataNode.SetData<VarInt32>("Battle/Score", 10);
int score = QGF.DataNode.GetData<VarInt32>("Battle/Score").Value;
QGF.DataNode.RemoveNode("Battle");
```

SetData 自动创建中间节点，VarInt32 等 Variable 对象由节点持有和归还，业务不要重复 Release。GetNode 查询节点，GetOrAddNode 创建缺失节点；IDataNode 的 GetAllChildNonAlloc 清空并填充业务列表。DataNode 不负责存档。

[运行示例与完整脚本](../../Samples~/01_Modules/03_DataNode/)。

源码：[组件](../../Runtime/Unity/DataNode/DataNodeComponent.cs)。
